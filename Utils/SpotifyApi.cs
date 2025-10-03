using System.Text;
using TuneMates_Backend.DataBase;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;

namespace TuneMates_Backend.Utils
{
    public class SpotifyApi
    {
        private readonly HttpClient _http;
        private readonly AppDbContext _db;
        private readonly IConfiguration _cfg;

        /// <summary>
        /// Constructor for SpotifyApi
        /// </summary>
        /// <param name="http">The HttpClient to use for requests</param>
        /// <param name="db">The database context</param>
        /// <param name="cfg">The configuration to use for settings</param>
        public SpotifyApi(HttpClient http, AppDbContext db, IConfiguration cfg)
        {
            _http = http;
            _db = db;
            _cfg = cfg;
        }

        /// <summary>
        /// Get a valid access token from Spotify, either from the database or by requesting a new one
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains the access token as a string.</returns>
        /// <exception cref="Exception">Thrown when the Spotify client ID or secret is not configured, or when the access token cannot be retrieved from Spotify.</exception>
        public async Task<string> GetAccessTokenAsync()
        {
            // Check if we have a valid token in the database
            var token = await _db.Tokens.OrderByDescending(t => t.CreatedAt).FirstOrDefaultAsync();
            if (token != null && token.ExpiresAt > DateTime.UtcNow.AddMinutes(1))
                return token.RefreshToken;

            // If not, request a new one from Spotify

            var clientId = _cfg["Spotify:ClientId"];
            var clientSecret = _cfg["Spotify:ClientSecret"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                throw new Exception("Spotify client ID or secret is not configured.");

            // Requesting a new token from Spotify API using Client Credentials Flow
            HttpRequestMessage request = new(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            request.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            });

            // Sending the request and processing the response to extract the access token
            HttpResponseMessage response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            var contentToken = content?["access_token"]?.ToString() ?? throw new Exception("Failed to retrieve access token from Spotify.");
            Token newAccessToken = new()
            {
                RefreshToken = contentToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddSeconds(int.Parse(content?["expires_in"]?.ToString() ?? "3600")),
            };
            _db.Tokens.Add(newAccessToken);
            await _db.SaveChangesAsync();
            return contentToken;
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
            var content = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            if (content == null)
                return null;
            Song song = new()
            {
                Album = ((Dictionary<string, object>)content["album"])["name"].ToString() ?? string.Empty,
                Artist = string.Join(", ", ((List<object>)content["artists"]).Select(a => ((Dictionary<string, object>)a)["name"].ToString())),
                DurationMs = int.Parse(content["duration_ms"].ToString() ?? "0"),
                SongId = content["id"].ToString() ?? string.Empty,
                Title = content["name"].ToString() ?? string.Empty,
                AlbumArtUrl = ((List<object>)((Dictionary<string, object>)content["album"])["images"]).FirstOrDefault() is Dictionary<string, object> img ? img["url"].ToString() ?? string.Empty : string.Empty
            };
            return song;
        }
    }
}