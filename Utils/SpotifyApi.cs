using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using TuneMates_Backend.DataBase;

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
        /// Get a valid user access token, refreshing it if necessary
        /// </summary>
        /// <param name="owner">The user whose token to retrieve</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains the user access token as a string.</returns>
        /// <exception cref="Exception">Thrown when the Spotify client ID or secret is not configured, or when the access token cannot be retrieved from Spotify.</exception>
        public async Task<string> GetUserAccessTokenAsync(User owner, CancellationToken ct = default)
        {
            EncryptionService encryptionService = new(_cfg);

            // If the owner's token is still valid, return it
            if (owner.TokenExpiresAt > DateTime.UtcNow.AddMinutes(1) && !string.IsNullOrWhiteSpace(owner.Token))
                return encryptionService.Decrypt(owner.Token);

            // Otherwise, refresh the token using the refresh token

            var clientId = _cfg["Spotify:ClientId"];
            var clientSecret = _cfg["Spotify:ClientSecret"];
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                throw new Exception("Spotify client ID or secret is not configured.");

            if (string.IsNullOrWhiteSpace(owner.RefreshToken))
                throw new Exception("User does not have a refresh token.");

            using HttpRequestMessage req = new(HttpMethod.Post, "https://accounts.spotify.com/api/token");

            req.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"))
             );

            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", encryptionService.Decrypt(owner.RefreshToken) }
            });

            using HttpResponseMessage res = await _http.SendAsync(req, ct);
            
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new Exception($"Failed to refresh Spotify access token. Status: {(int)res.StatusCode}, Body: {body}");

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var newToken = root.GetProperty("access_token").GetString();
            int expiresInSeconds = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600;

            if (string.IsNullOrWhiteSpace(newToken))
                throw new Exception("Failed to retrieve new access token from Spotify.");

            owner.Token = encryptionService.Encrypt(newToken);
            owner.TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
            await _db.SaveChangesAsync();

            return newToken;
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
                Uri = content["uri"].GetString() ?? string.Empty,
                Explicit = content["explicit"].GetBoolean(),
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
        /// <param name="token">The user's Spotify access token</param>
        /// <param name="playlistInput">The Spotify ID of the playlist</param>
        /// <param name="trackInput">The Spotify ID of the song to add</param>
        /// <param name="ct">The cancellation token</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains the snapshot ID of the playlist after adding the song, or null if unsuccessful.</returns>
        public async Task<string?> AddSongToPlaylistAsync(string token, string playlistInput, string trackInput, CancellationToken ct = default)
        {
            var plaqylistId = NormalizePlaylistId(playlistInput);
            var trackUri = NormalizeTrackUri(trackInput);
            if (plaqylistId is null || trackUri is null)
                throw new ArgumentException("Invalid playlist ID or track ID.");

            async Task<HttpResponseMessage> SendAsync()
            {
                string url = $"https://api.spotify.com/v1/playlists/{plaqylistId}/tracks";
                using HttpRequestMessage req = new(HttpMethod.Post, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                req.Content = JsonContent.Create(new
                {
                    uris = new[] { trackUri },
                    position = 0
                });
                return await _http.SendAsync(req, ct);
            }

            using HttpResponseMessage res1 = await SendAsync();

            // Handle rate limiting (HTTP 429) by retrying after the specified wait time
            if (res1.StatusCode == (HttpStatusCode)429 &&
                res1.Headers.RetryAfter?.Delta is TimeSpan wait &&
                wait <= TimeSpan.FromSeconds(10))
            {
                await Task.Delay(wait, ct);
                using HttpResponseMessage resRetry429 = await SendAsync();
                return await ParseSnapshotOrThrow(resRetry429, ct);
            }

            if (res1.StatusCode == HttpStatusCode.Unauthorized)
                return await ParseSnapshotOrThrow(res1, ct);

            return await ParseSnapshotOrThrow(res1, ct);
        }

        /// <summary>
        /// Get a user access token from an authorization code
        /// </summary>
        /// <param name="code">The authorization code received from Spotify</param>
        /// <returns>The user access token as an <see cref="AccessToken"/> object</returns>
        /// <exception cref="Exception">Send when the Spotify client ID or secret is not configured, or when the access token cannot be retrieved from Spotify.</exception>
        public async Task<AccessToken> GetUserAccessTokenFromCode(string code)
        {
            var clientId = _cfg["Spotify:ClientId"];
            var clientSecret = _cfg["Spotify:ClientSecret"];
            var redirectUri = _cfg["Spotify:RedirectUri"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(redirectUri))
                throw new Exception("Spotify client ID or secret is not configured.");

            string authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            using HttpRequestMessage req = new(HttpMethod.Post, "https://accounts.spotify.com/api/token");

            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri }
            });

            using HttpResponseMessage res = await _http.SendAsync(req);

            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Failed to get Spotify user access token. Status: {(int)res.StatusCode}, Body: {body}");

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var accessToken = root.GetProperty("access_token").GetString();
            var refreshToken = root.GetProperty("refresh_token").GetString();
            int expiresInSeconds = root.GetProperty("expires_in").GetInt32();

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
                throw new Exception("Failed to retrieve user access token or refresh token from Spotify.");

            AccessToken token = new()
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = expiresInSeconds,
            };

            // Try fetching the user's profile to obtain the Spotify user ID
            try
            {
                using HttpRequestMessage profileReq = new(HttpMethod.Get, "https://api.spotify.com/v1/me");
                profileReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                using HttpResponseMessage profileRes = await _http.SendAsync(profileReq);

                if (profileRes.IsSuccessStatusCode)
                {
                    var profileBody = await profileRes.Content.ReadAsStringAsync();
                    using var profileDoc = JsonDocument.Parse(profileBody);
                    var profileRoot = profileDoc.RootElement;
                    var spotifyUserId = profileRoot.GetProperty("id").GetString();
                    if (!string.IsNullOrWhiteSpace(spotifyUserId))
                        token.SpotifyUserId = spotifyUserId;
                }
            }
            catch
            {
                // Ignore errors fetching the user profile
            }

            return token;
        }

        /// <summary>
        /// Get the playlists of a user by their Spotify user ID
        /// </summary>
        /// <param name="userAccessToken">The user's Spotify access token</param>
        /// <param name="userId">The Spotify user ID</param>
        /// <param name="limit">The maximum number of playlists to return (1-50)</param>
        /// <param name="offset">The index of the first playlist to return (for pagination)</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains the user's playlists as a <see cref="SpotifyDTO.PlaylistResponse"/> object.</returns>
        /// <exception cref="Exception">Thrown if the Spotify response cannot be parsed.</exception>
        public async Task<SpotifyDTO.PlaylistResponse> GetUserPlaylists(string userAccessToken, string userId, int limit = 50, int offset = 0)
        {
            limit = Math.Clamp(limit, 1, 50);
            offset = Math.Max(0, offset);

            string url = $"https://api.spotify.com/v1/users/{userId}/playlists?limit={limit}&offset={offset}";
            using HttpRequestMessage req = new(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAccessToken);

            using HttpResponseMessage res = await _http.SendAsync(req);

            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new Exception($"Failed to get user playlists from Spotify. Status: {(int)res.StatusCode}, Body: {body}");

            SpotifyDTO.PlaylistResponse? playlists = await res.Content.ReadFromJsonAsync<SpotifyDTO.PlaylistResponse>();

            return playlists ?? new SpotifyDTO.PlaylistResponse { Items = new List<SpotifyDTO.Playlist>() };
        }

        /// <summary>
        /// Extract the track ID from a Spotify URI or URL
        /// </summary>
        /// <param name="input">The Spotify URI or URL</param>
        /// <returns>A normalized track ID as a <c>string</c>, or <c>null</c> if the input is invalid.</returns>
        public static string? GetTrackIdFromUriOrUrl(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var mUrl = Regex.Match(input, Constants.Regex.SpotifyTrackUrl);
            if (mUrl.Success)
                return mUrl.Groups[1].Value;

            var mUri = Regex.Match(input, Constants.Regex.SpotifyTrackUri);
            if (mUri.Success)
                return mUri.Groups[1].Value;

            return Regex.IsMatch(input, @"^[A-Za-z0-9]+$") ? input : null;
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
                .OrderByDescending(i => i.Height ?? 0)
                .FirstOrDefault()?.Url ?? string.Empty;

            return new SpotifyDTO.TrackDTO(
                Id: t.Id!,
                Name: t.Name!,
                Artist: string.Join(", ", t.Artists?.Select(a => a.Name) ?? Enumerable.Empty<string>()),
                Album: t.Album?.Name ?? string.Empty,
                AlbumImageUrl: albumImg,
                DurationMs: t.DurationMs,
                Uri: t.Uri ?? string.Empty,
                Explicit: t.Explicit ?? false,
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

        /// <summary>
        /// Parse the snapshot ID from the HTTP response or throw an exception if the request failed
        /// </summary>
        /// <param name="res">The HTTP response message</param>
        /// <param name="ct">The cancellation token</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains the snapshot ID as a <c>string</c>, or <c>null</c> if not found.</returns>
        /// <exception cref="HttpRequestException">Thrown if the Spotify API request failed.</exception>
        private static async Task<string?> ParseSnapshotOrThrow(HttpResponseMessage res, CancellationToken ct)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"Spotify API request failed. Status: {(int)res.StatusCode}, Body: {body}");

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("snapshot_id", out var snap) ? snap.GetString() : null;
        }

        /// <summary>
        /// Normalize a Spotify playlist ID from various input formats
        /// </summary>
        /// <param name="input">The input string containing the playlist ID or URL</param>
        /// <returns>A normalized playlist ID as a <c>string</c>, or <c>null</c> if the input is invalid.</returns>
        private static string? NormalizePlaylistId(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;
            var mUrl = Regex.Match(input, Constants.Regex.SpotifyPlaylistUrl);
            if (mUrl.Success)
                return mUrl.Groups[1].Value;
            var mUri = Regex.Match(input, Constants.Regex.SpotifyPlaylistUri);
            if (mUri.Success)
                return mUri.Groups[1].Value;
            return Regex.IsMatch(input, @"^[A-Za-z0-9]+$") ? input : null;
        }

        /// <summary>
        /// Normalize a Spotify track URI from various input formats
        /// </summary>
        /// <param name="input">The input string containing the track ID, URI, or URL</param>
        /// <returns>A normalized track URI as a <c>string</c>, or <c>null</c> if the input is invalid.</returns>
        private static string? NormalizeTrackUri(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;
            var mUrl = Regex.Match(input, Constants.Regex.SpotifyTrackUrl);
            if (mUrl.Success)
                return $"spotify:track:{mUrl.Groups[1].Value}";
            var mUri = Regex.Match(input, Constants.Regex.SpotifyTrackUri);
            if (mUri.Success)
                return $"spotify:track:{mUri.Groups[1].Value}";
            return Regex.IsMatch(input, @"^[A-Za-z0-9]+$") ? $"spotify:track:{input}" : null;
        }
    }
}