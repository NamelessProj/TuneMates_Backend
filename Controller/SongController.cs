using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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

        /// <summary>
        /// Add a new song to a specific room by its Spotify ID.
        /// Used by every user in the room to make requests for songs to be added to the playlist.
        /// </summary>
        /// <param name="cfg">The configuration containing Spotify settings</param>
        /// <param name="db">The database context</param>
        /// <param name="roomId">The ID of the room</param>
        /// <param name="songId">The song object containing the Spotify ID</param>
        /// <returns>The added song details or an error result</returns>
        public static async Task<IResult> AddSongToRoom(IConfiguration cfg, IMemoryCache cache, AppDbContext db, int roomId, string songId)
        {
            var room = await db.Rooms.FindAsync(roomId);
            if (room == null)
                return TypedResults.NotFound("Room not found");

            // Check if the room is active
            if (!room.IsActive)
                return TypedResults.BadRequest("Cannot add songs to an inactive room");

            // Getting the song details from Spotify API
            SpotifyApi spotifyApi = new(db, cfg, cache);
            var spotifySong = await spotifyApi.GetSongDetailsAsync(songId);
            if (spotifySong == null)
                return TypedResults.NotFound("Song not found on Spotify");

            // Check if the song already exists in the room
            var existingSong = await db.Songs.Where(s => s.RoomId == roomId && s.SongId == songId).FirstOrDefaultAsync();
            if (existingSong != null)
                return TypedResults.Conflict("Song already exists in the room");

            spotifySong.RoomId = roomId;
            db.Songs.Add(spotifySong);
            room.LastUpdate = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return TypedResults.Ok(spotifySong);
        }

        /// <summary>
        /// Add a new song to a specific room using a Spotify URI or URL.
        /// </summary>
        /// <param name="cfg">The configuration containing Spotify settings</param>
        /// <param name="cache">The memory cache for caching Spotify tokens</param>
        /// <param name="db">The database context</param>
        /// <param name="roomId">The ID of the room</param>
        /// <param name="input">The Spotify track URI or URL</param>
        /// <returns>A result indicating success or failure</returns>
        public static async Task<IResult> AddSongToRoomUsingUriOrUrl(IConfiguration cfg, IMemoryCache cache, AppDbContext db, int roomId, [FromBody] Song song)
        {
            var room = await db.Rooms.FindAsync(roomId);
            if (room == null)
                return TypedResults.NotFound("Room not found");

            // Check if the room is active
            if (!room.IsActive)
                return TypedResults.BadRequest("Cannot add songs to an inactive room");

            string input = song.Uri.Trim();

            if (string.IsNullOrWhiteSpace(input))
                return TypedResults.BadRequest("Input cannot be empty");

            // Getting the song details from Spotify API
            SpotifyApi spotifyApi = new(db, cfg, cache);

            var songId = spotifyApi.GetTrackIdFromUriOrUrl(input);
            if (songId == null)
                return TypedResults.BadRequest("Invalid Spotify track URI or URL");

            var spotifySong = await spotifyApi.GetSongDetailsAsync(songId);
            if (spotifySong == null)
                return TypedResults.NotFound("Song not found on Spotify");

            // Check if the song already exists in the room
            var existingSong = await db.Songs.Where(s => s.RoomId == roomId && s.SongId == songId).FirstOrDefaultAsync();
            if (existingSong != null)
                return TypedResults.Conflict("Song already exists in the room");

            spotifySong.RoomId = roomId;
            db.Songs.Add(spotifySong);
            room.LastUpdate = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return TypedResults.Ok(spotifySong);
        }
    }
}