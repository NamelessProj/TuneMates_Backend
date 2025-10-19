using TuneMates_Backend.Utils;

namespace TuneMates_Backend.Controller
{
    public static class SpotifyController
    {
        /// <summary>
        /// Generate and return the Spotify OAuth link for user authorization
        /// </summary>
        /// <param name="cfg">The configuration containing Spotify settings</param>
        /// <returns>An <see cref="IResult"/> containing the OAuth URL or an error message</returns>
        public static async Task<IResult> SendUserOathLink(IConfiguration cfg)
        {
            var clientId = cfg["Spotify:ClientId"];
            var redirectUri = cfg["Spotify:RedirectUri"];
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUri))
                return TypedResults.Problem("Spotify configuration is missing.");

            string state = HelpMethods.GenerateRandomString(16);
            string scope = "user-read-private user-read-email playlist-read-private playlist-modify-private playlist-modify-public";

            // Build the query parameters
            Dictionary<string, string> queryParams = new()
            {
                { "response_type", "code" },
                { "client_id", clientId },
                { "scope", scope },
                { "redirect_uri", redirectUri },
                { "state", state }
            };
            string queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            string oauthUrl = $"https://accounts.spotify.com/authorize?{queryString}";

            return TypedResults.Ok(new { url = oauthUrl });
        }
    }
}