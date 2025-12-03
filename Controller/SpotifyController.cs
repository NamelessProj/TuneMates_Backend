using Microsoft.Extensions.Caching.Memory;
using TuneMates_Backend.DataBase;
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
        public static async Task<IResult> SendUserOathLink(IConfiguration cfg, AppDbContext db)
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

            // Store the state in the database to validate later
            SpotifyState spotifyState = new()
            {
                State = state,
                CreatedAt = DateTime.UtcNow
            };
            db.SpotifyStates.Add(spotifyState);
            await db.SaveChangesAsync();

            return TypedResults.Ok(new { url = oauthUrl });
        }

        /// <summary>
        /// Add a song from a room's pending list to the linked Spotify playlist.
        /// </summary>
        /// <param name="cfg">The configuration containing Spotify settings</param>
        /// <param name="db">The database context</param>
        /// <param name="roomId">The ID of the room</param>
        /// <param name="songId">The ID of the song to add</param>
        /// <returns>A result indicating success or failure</returns>
        public static async Task<IResult> AddSongToPlaylist(IConfiguration cfg, IMemoryCache cache, AppDbContext db, int roomId, int songId, CancellationToken ct)
        {
            var room = await db.Rooms.FindAsync(roomId, ct);
            if (room is null)
                return TypedResults.NotFound("Room not found");

            if (!room.IsActive)
                return TypedResults.Conflict("Room is not active");

            if (string.IsNullOrWhiteSpace(room.SpotifyPlaylistId))
                return TypedResults.BadRequest("Room does not have a linked Spotify playlist");

            var song = await db.Songs.FindAsync(songId, ct);
            if (song is null || song.RoomId != roomId)
                return TypedResults.NotFound("Song not found in the specified room");

            if (song.Status != SongStatus.Pending)
                return TypedResults.Conflict("Song is not in a pending state");

            // Getting the owner of the room to use their Spotify token
            var owner = await db.Users.FindAsync(room.UserId, ct);
            if (owner is null)
                return TypedResults.NotFound("Room owner not found");

            SpotifyApi spotifyApi = new(db, cfg, cache);
            string ownerToken = await spotifyApi.GetUserAccessTokenAsync(owner, ct);
            var snapshotId = await spotifyApi.AddSongToPlaylistAsync(
                ownerToken,
                room.SpotifyPlaylistId,
                song.SongId,
                ct
             );

            if (snapshotId is null)
                return TypedResults.Problem("Failed to add song to Spotify playlist");

            song.Status = SongStatus.Approved;

            room.LastUpdate = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return TypedResults.Ok(new {
                song,
                snapshotId
            });
        }

        /// <summary>
        /// Get the Spotify playlists of the currently authenticated user.
        /// </summary>
        /// <param name="cfg">The configuration containing Spotify settings</param>
        /// <param name="cache">The memory cache for caching Spotify tokens</param>
        /// <param name="http">The current HTTP context</param>
        /// <param name="db">The database context</param>
        /// <returns>A result containing the user's Spotify playlists or an error message</returns>
        public static async Task<IResult> GetUserSpotifyPlaylists(IConfiguration cfg, IMemoryCache cache, HttpContext http, AppDbContext db)
        {
            var userId = HelpMethods.GetUserIdFromJwtClaims(http);
            if (userId is null)
                return TypedResults.Unauthorized();

            var user = await db.Users.FindAsync(userId);
            if (user is null)
                return TypedResults.NotFound("User not found");

            SpotifyApi spotifyApi = new(db, cfg, cache);

            string userToken = await spotifyApi.GetUserAccessTokenAsync(user);
            SpotifyDTO.PlaylistResponse playlists = await spotifyApi.GetUserPlaylists(userToken, user.SpotifyId);

            return TypedResults.Ok(playlists);
        }

        /// <summary>
        /// Search for songs on Spotify based on a query string.
        /// </summary>
        /// <param name="cfg">The configuration containing Spotify settings</param>
        /// <param name="cache">The memory cache for caching Spotify tokens</param>
        /// <param name="db">The database context</param>
        /// <param name="q">The search query string</param>
        /// <param name="offset">The offset for pagination</param>
        /// <param name="market">The market code (e.g., "US")</param>
        /// <returns>A result containing the search results or an error message</returns>
        public static async Task<IResult> SearchSongs(IConfiguration cfg, IMemoryCache cache, AppDbContext db, string q, int offset, string market)
        {
            if (string.IsNullOrWhiteSpace(q) || offset < 0)
                return TypedResults.BadRequest("Invalid query or page number");

            SpotifyApi spotifyApi = new(db, cfg, cache);
            var results = await spotifyApi.SearchTracksAsync(
                q,
                offset: Math.Max(0, offset),
                market: market
             );

            return TypedResults.Ok(results);
        }

        /// <summary>
        /// Get the Spotify access token for the currently authenticated user (owner of the room).
        /// </summary>
        /// <param name="cfg">The configuration containing Spotify settings</param>
        /// <param name="cache">The memory cache for caching Spotify tokens</param>
        /// <param name="http">The current HTTP context</param>
        /// <param name="db">The database context</param>
        /// <param name="ct">The cancellation token</param>
        /// <returns>A result containing the access token or an error message</returns>
        public static async Task<IResult> GetOwnerToken(IConfiguration cfg, IMemoryCache cache, HttpContext http, AppDbContext db, CancellationToken ct)
        {
            var userId = HelpMethods.GetUserIdFromJwtClaims(http);
            if (userId is null)
                return TypedResults.Unauthorized();

            var owner = await db.Users.FindAsync(userId);
            if (owner is null)
                return TypedResults.NotFound("User not found");

            SpotifyApi spotifyApi = new(db, cfg, cache);
            string ownerToken = await spotifyApi.GetUserAccessTokenAsync(owner, ct);

            return TypedResults.Ok(new { token = ownerToken } );
        }
    }
}