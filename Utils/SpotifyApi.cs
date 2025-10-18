using System.Text;
using TuneMates_Backend.DataBase;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Caching.Memory;

namespace TuneMates_Backend.Utils
{
    /// <summary>
    /// A service for interacting with the Spotify API, including obtaining access tokens, searching for tracks, and managing playlists.
    /// </summary>
    public class SpotifyApi
    {
        private readonly HttpClient _http;
        private readonly AppDbContext _db;
        private readonly IConfiguration _cfg;
        private readonly IMemoryCache _cache;

        private const string TokenCacheKey = "Spotify:AccessToken";
        private static readonly SemaphoreSlim _tokenLock = new(1, 1);

        /// <summary>
        /// Constructor for SpotifyApi
        /// </summary>
        /// <param name="db">The database context</param>
        /// <param name="cfg">The configuration to use for settings</param>
        /// <param name="cache">The memory cache for caching tokens</param>
        public SpotifyApi(AppDbContext db, IConfiguration cfg, IMemoryCache cache)
        {
            _http = new HttpClient();
            _db = db;
            _cfg = cfg;
            _cache = cache;
        }

        /// <summary>
        /// Get a valid access token from Spotify, either from the database or by requesting a new one
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains the access token as a string.</returns>
        /// <exception cref="Exception">Thrown when the Spotify client ID or secret is not configured, or when the access token cannot be retrieved from Spotify.</exception>
        public async Task<string> GetAccessTokenAsync()
        {
            // First, check if we have a valid token in the cache
            if (_cache.TryGetValue<string>(TokenCacheKey, out var cachedToken) && !string.IsNullOrWhiteSpace(cachedToken))
                return cachedToken;

            await _tokenLock.WaitAsync();

            try
            {
                // Double-check the cache after acquiring the lock
                if (_cache.TryGetValue<string>(TokenCacheKey, out cachedToken) && !string.IsNullOrWhiteSpace(cachedToken))
                    return cachedToken;

                // Cache miss, proceed to check the database
                // Check if we have a valid token in the database
                var token = await _db.Tokens.OrderByDescending(t => t.CreatedAt).FirstOrDefaultAsync();
                EncryptionService encryptionService = new(_cfg);
                if (token != null && token.ExpiresAt > DateTime.UtcNow.AddMinutes(1))
                {
                    string decryptedToken = encryptionService.Decrypt(token.RefreshToken);
                    CacheToken(decryptedToken, token.ExpiresAt);
                    return decryptedToken;
                }

                // If not, request a new one from Spotify

                var clientId = _cfg["Spotify:ClientId"];
                var clientSecret = _cfg["Spotify:ClientSecret"];

                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                    throw new Exception("Spotify client ID or secret is not configured.");

                // Requesting a new token from Spotify API using Client Credentials Flow
                HttpRequestMessage request = new(HttpMethod.Post, "https://accounts.spotify.com/api/token");
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", clientId },
                    { "client_secret", clientSecret }
                });

                // Sending the request and processing the response to extract the access token
                using HttpResponseMessage response = await _http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                var contentToken = content?["access_token"]?.ToString() ?? throw new Exception("Failed to retrieve access token from Spotify.");
                int expiresInSeconds = int.Parse(content?["expires_in"]?.ToString() ?? "3600");
                DateTime expiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);

                Token newAccessToken = new()
                {
                    RefreshToken = encryptionService.Encrypt(contentToken),
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                };

                _db.Tokens.Add(newAccessToken);
                await _db.SaveChangesAsync();

                // Cache the new token
                CacheToken(contentToken, expiresAt);

                return contentToken;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        /// <summary>
        /// Get song details from Spotify by song ID
        /// </summary>
        /// <param name="songId">The Spotify ID of the song</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains the song details as a <see cref="Song"/> object, or null if not found.</returns>
        public async Task<Song?> GetSongDetailsAsync(string songId)
        {
            string accessToken = await GetAccessTokenAsync();

            // Requesting song details from Spotify API using the access token
            HttpRequestMessage request = new(HttpMethod.Get, $"https://api.spotify.com/v1/tracks/{songId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Sending the request and checking the response status
            HttpResponseMessage response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            // Parsing the response content to extract song details
            var content = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
            if (content == null)
                return null;

            var album = content["album"];
            var artists = content["artists"];
            var images = album.GetProperty("images");

            Song song = new()
            {
                Album = album.GetProperty("name").GetString() ?? string.Empty,
                Artist = string.Join(", ", artists.EnumerateArray().Select(a => a.GetProperty("name").GetString())),
                DurationMs = content["duration_ms"].GetInt32(),
                SongId = content["id"].GetString() ?? string.Empty,
                Title = content["name"].GetString() ?? string.Empty,
                AlbumArtUrl = images.GetArrayLength() > 0 ? images[0].GetProperty("url").GetString() ?? string.Empty : string.Empty
            };
            return song;
        }

        /// <summary>
        /// Search for tracks on Spotify matching the given query
        /// </summary>
        /// <param name="query">The search query</param>
        /// <param name="limit">The maximum number of results to return (1-50)</param>
        /// <param name="offset">The index of the first result to return (for pagination)</param>
        /// <param name="market">The market (country code) to search in (optional)</param>
        /// <param name="ct">The cancellation token</param>
        /// <returns>
        /// A <see cref="Task"/> that represents the asynchronous operation.
        /// The task result contains a paginated list of tracks (<see cref="SpotifyDTO.PageResult{T}"/>.<see cref="SpotifyDTO.TrackDTO"/>) matching the search query.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown if the query is null or empty.</exception>
        /// <exception cref="Exception">Thrown if the Spotify response cannot be parsed.</exception>
        public async Task<SpotifyDTO.PageResult<SpotifyDTO.TrackDTO>> SearchTracksAsync(string query, int limit = 10, int offset = 0, string? market = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be null or empty.", nameof(query));

            limit = Math.Clamp(limit, 1, 50);
            offset = Math.Max(0, offset);

            string accessToken = await GetAccessTokenAsync();

            // Building the search URL with query parameters
            StringBuilder url = new("https://api.spotify.com/v1/search?type=track");
            url.Append("&q=").Append(Uri.EscapeDataString(query));
            url.Append("&limit=").Append(limit);
            url.Append("&offset=").Append(offset);
            if (!string.IsNullOrWhiteSpace(market))
                url.Append("&market=").Append(Uri.EscapeDataString(market));

            // Creating the HTTP request with the access token in the Authorization header (Bearer token)
            using HttpRequestMessage req = new(HttpMethod.Get, url.ToString());
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Sending the request and processing the response
            using HttpResponseMessage res = await _http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();

            var root = await res.Content.ReadFromJsonAsync<SpotifyDTO.SearchResponse>(cancellationToken: ct);
            if (root?.Tracks is null)
                throw new Exception("Failed to parse Spotify response.");

            var tracks = root.Tracks.Items?.Select(MapTrack).ToList() ?? new List<SpotifyDTO.TrackDTO>();

            bool hasNext = !string.IsNullOrWhiteSpace(root.Tracks.Next);
            int? nextOffset = CalculateNextOffset(root.Tracks.Next);

            return new SpotifyDTO.PageResult<SpotifyDTO.TrackDTO>(
                Items: tracks,
                Limit: root.Tracks.Limit,
                Offset: root.Tracks.Offset,
                Total: root.Tracks.Total,
                HasNext: hasNext,
                NextOffset: nextOffset
            );
        }

        /// <summary>
        /// Add a song to a Spotify playlist by song ID
        /// </summary>
        /// <param name="playlistId">The Spotify ID of the playlist</param>
        /// <param name="songId">The Spotify ID of the song to add</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains <c>true</c> if the song was added successfully, otherwise <c>false</c>.</returns>
        public async Task<bool> AddSongToPlaylistAsync(string playlistId, string songId)
        {
            if (string.IsNullOrWhiteSpace(playlistId) || string.IsNullOrWhiteSpace(songId))
                return false;

            songId = songId.StartsWith("spotify:track:") ? songId : $"spotify:track:{songId}";

            string accessToken = await GetAccessTokenAsync();
            HttpRequestMessage req = new(HttpMethod.Post, $"https://api.spotify.com/v1/playlists/{playlistId}/tracks");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Request body to add the song at position 0
            req.Content = JsonContent.Create(new {
                uris = new[] { songId },
                position = 0
            });

            HttpResponseMessage res = await _http.SendAsync(req);

            return res.IsSuccessStatusCode;
        }

        /// <summary>
        /// Calculate the next offset from the Spotify "next" URL
        /// </summary>
        /// <param name="nextUrl">The "next" URL from Spotify API response</param>
        /// <returns>An integer representing the next offset, or null if not available</returns>
        private static int? CalculateNextOffset(string? nextUrl)
        {
            if (string.IsNullOrWhiteSpace(nextUrl))
                return null;

            Uri uri = new(nextUrl);
            var query = HttpUtility.ParseQueryString(uri.Query);
            if (int.TryParse(query.Get("offset"), out var next))
                return next;
            return null;
        }

        /// <summary>
        /// Map a SpotifyTrack to a TrackDTO
        /// </summary>
        /// <param name="t">The SpotifyTrack to map</param>
        /// <returns>A TrackDTO representing the track</returns>
        private static SpotifyDTO.TrackDTO MapTrack(SpotifyDTO.SpotifyTrack t)
        {
            var albumImg = t.Album?.Images?
                .OrderByDescending(i => i.Height)
                .FirstOrDefault()?.Url ?? string.Empty;

            return new SpotifyDTO.TrackDTO(
                Id: t.Id!,
                Name: t.Name!,
                Artist: string.Join(", ", t.Artists?.Select(a => a.Name) ?? Enumerable.Empty<string>()),
                Album: t.Album?.Name ?? string.Empty,
                AlbumImageUrl: albumImg,
                DurationMs: t.DurationMs,
                Uri: t.Uri ?? string.Empty,
                ExternalUri: t.ExternalUrls?["spotify"] ?? string.Empty
            );
        }

        /// <summary>
        /// Cache the access token with an expiration time
        /// </summary>
        /// <param name="token">The access token to cache</param>
        /// <param name="expiresAtUtc">The UTC expiration time of the token</param>
        private void CacheToken(string token, DateTime expiresAtUtc)
        {
            // Safe margin of 1 minute before actual expiry
            var ttl = expiresAtUtc - DateTime.UtcNow - TimeSpan.FromMinutes(1);
            if (ttl <= TimeSpan.Zero)
                return;

            _cache.Set(TokenCacheKey, token, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
                Priority = CacheItemPriority.High
            });
        }
    }
}