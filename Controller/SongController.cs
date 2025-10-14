using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Utils;

namespace TuneMates_Backend.Controller
{
    public static class SongController
    {
        /// <summary>
        /// Get all songs from a specific room
        /// </summary>
        /// <param name="http">The HTTP context</param>
        /// <param name="db">The database context</param>
        /// <param name="roomId">The ID of the room</param>
        /// <returns>A list of songs in the room or an error result</returns>
        public static async Task<IResult> GetAllSongsFromRoom(HttpContext http, AppDbContext db, int roomId)
        {
            var userId = HelpMethods.GetUserIdFromJwtClaims(http);
            if (userId == null)
                return TypedResults.Unauthorized();

            var room = await db.Rooms.FindAsync(roomId);
            if (room == null)
                return TypedResults.NotFound("Room not found");

            if (room.UserId != userId)
                return TypedResults.Forbid();

            var songs = await db.Songs.Where(s => s.RoomId == roomId).ToListAsync();
            return TypedResults.Ok(songs);
        }

        /// <summary>
        /// Get songs from a specific room filtered by their status
        /// </summary>
        /// <param name="http">The HTTP context</param>
        /// <param name="db">The database context</param>
        /// <param name="roomId">The ID of the room</param>
        /// <param name="statusCode">The status to filter songs by (<see cref="SongStatus"/>: "Pending": 0, "Approved": 1, "Refused": 2)</param>
        /// <returns>A list of songs with the specified status in the room or an error result</returns>
        public static async Task<IResult> GetSongsFromRoomWithStatus(HttpContext http, AppDbContext db, int roomId, int statusCode)
        {
            var userId = HelpMethods.GetUserIdFromJwtClaims(http);
            if (userId == null)
                return TypedResults.Unauthorized();

            var room = await db.Rooms.FindAsync(roomId);
            if (room == null)
                return TypedResults.NotFound("Room not found");

            if (room.UserId != userId)
                return TypedResults.Forbid();

            var songs = await db.Songs.Where(s => s.RoomId == roomId && (int)s.Status == statusCode).ToListAsync();
            return TypedResults.Ok(songs);
        }

        public static async Task<IResult> AddSongToPlaylist(IConfiguration cfg, AppDbContext db, int roomId, int songId)
        {
            var room = await db.Rooms.FindAsync(roomId);
            if (room is null)
                return TypedResults.NotFound("Room not found");

            if (string.IsNullOrWhiteSpace(room.SpotifyPlaylistId))
                return TypedResults.BadRequest("Room does not have a linked Spotify playlist");

            var song = await db.Songs.FindAsync(songId); 
            if (song is null || song.RoomId != roomId)
                return TypedResults.NotFound("Song not found in the specified room");

            if (song.Status != SongStatus.Pending)
                return TypedResults.Conflict("Song is not in a pending state");

            song.Status = SongStatus.Approved;
            await db.SaveChangesAsync();

            HttpClient http = new();
            SpotifyApi spotifyApi = new(http, db, cfg);
            bool res = await spotifyApi.AddSongToPlaylistAsync(room.SpotifyPlaylistId, song.SongId);

            return res ? TypedResults.Ok(song) : TypedResults.StatusCode(500);
        }

        /// <summary>
        /// Add a new song to a specific room by its Spotify ID.
        /// Used by every user in the room to make requests for songs to be added to the playlist.
        /// </summary>
        /// <param name="cfg">The configuration containing Spotify settings</param>
        /// <param name="db">The database context</param>
        /// <param name="roomId">The ID of the room</param>
        /// <param name="songId">The song object containing the Spotify ID</param>
        /// <returns>The added song details or an error result</returns>
        public static async Task<IResult> AddSongToRoom(IConfiguration cfg, AppDbContext db, int roomId, string songId)
        {
            var room = await db.Rooms.FindAsync(roomId);
            if (room == null)
                return TypedResults.NotFound("Room not found");

            // Getting the song details from Spotify API
            HttpClient httpClient = new();
            SpotifyApi spotifyApi = new(httpClient, db, cfg);
            var spotifySong = await spotifyApi.GetSongDetailsAsync(songId);
            if (spotifySong == null)
                return TypedResults.NotFound("Song not found on Spotify");

            // Check if the song already exists in the room
            var existingSong = await db.Songs.Where(s => s.RoomId == roomId && s.SongId == songId).FirstOrDefaultAsync();
            if (existingSong != null)
                return TypedResults.Conflict("Song already exists in the room");

            spotifySong.RoomId = roomId;
            db.Songs.Add(spotifySong);
            await db.SaveChangesAsync();
            return TypedResults.Ok(spotifySong);
        }

        public static async Task<IResult> SearchSongs(IConfiguration cfg, AppDbContext db, string q, int offset, string market)
        {
            if (string.IsNullOrWhiteSpace(q) || offset < 0)
                return TypedResults.BadRequest("Invalid query or page number");

            HttpClient httpClient = new();
            SpotifyApi spotifyApi = new(httpClient, db, cfg);
            var results = await spotifyApi.SearchTracksAsync(q,
                offset: offset <= 0 ? 0 : offset,
                market: market);

            return TypedResults.Ok(results);
        }
    }
}