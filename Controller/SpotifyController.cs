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
            await db.SaveChangesAsync();

            return TypedResults.Ok(new { song, snapshotId });
        }

        public static async Task<IResult> SearchSongs(IConfiguration cfg, IMemoryCache cache, AppDbContext db, string q, int offset, string market)
        {
            if (string.IsNullOrWhiteSpace(q) || offset < 0)
                return TypedResults.BadRequest("Invalid query or page number");

            SpotifyApi spotifyApi = new(db, cfg, cache);
            var results = await spotifyApi.SearchTracksAsync(q,
                offset: offset <= 0 ? 0 : offset,
                market: market);

            return TypedResults.Ok(results);
        }
    }
}