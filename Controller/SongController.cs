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
        /// <param name="status">The status to filter songs by (<see cref="SongStatus"/>: "Pending", "Approved", "Refused")</param>
        /// <returns>A list of songs with the specified status in the room or an error result</returns>
        public static async Task<IResult> GetSongsFromRoomWithStatus(HttpContext http, AppDbContext db, int roomId, string status)
        {
            var userId = HelpMethods.GetUserIdFromJwtClaims(http);
            if (userId == null)
                return TypedResults.Unauthorized();

            var room = await db.Rooms.FindAsync(roomId);
            if (room == null)
                return TypedResults.NotFound("Room not found");

            if (room.UserId != userId)
                return TypedResults.Forbid();

            string normalizedStatus = status.Trim().ToLower();

            var songs = await db.Songs.Where(s => s.RoomId == roomId && s.Status.ToString().ToLower() == normalizedStatus).ToListAsync();
            return TypedResults.Ok(songs);
        }

        //public static async Task<IResult> AddSongToPlaylist(int roomId)
        //{ }

        /// <summary>
        /// Add a new song to a specific room by its Spotify ID.
        /// Used by every user in the room to make requests for songs to be added to the playlist.
        /// </summary>
        /// <param name="cfg">The configuration containing Spotify settings</param>
        /// <param name="db">The database context</param>
        /// <param name="roomId">The ID of the room</param>
        /// <param name="song">The song object containing the Spotify ID</param>
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
            var existingSong = await db.Songs.FirstOrDefaultAsync(s => s.RoomId == roomId && s.SongId == songId);
            if (existingSong != null)
                return TypedResults.Conflict("Song already exists in the room");

            spotifySong.RoomId = roomId;
            db.Songs.Add(spotifySong);
            await db.SaveChangesAsync();
            return TypedResults.Ok(spotifySong);
        }
    }
}