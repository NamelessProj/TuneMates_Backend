using TuneMates_Backend.Controller;
using TuneMates_Backend.Infrastructure.RateLimiting;

namespace TuneMates_Backend.Route
{
    public static class SongRoutes
    {
        /// <summary>
        /// Map song-related routes to the given <see cref="RouteGroupBuilder"/>.
        /// </summary>
        /// <param name="group">The <see cref="RouteGroupBuilder"/> to map the routes to.</param>
        /// <returns>The updated <see cref="RouteGroupBuilder"/> with song routes mapped.</returns>
        public static RouteGroupBuilder MapSongRoutes(this RouteGroupBuilder group)
        {
            var songGroup = group.MapGroup("/songs");

            var songRoomGroup = songGroup.MapGroup("/room");
            songRoomGroup.MapGet("/{roomId:int}", SongController.GetAllSongsFromRoom).RequireAuthorization();
            songRoomGroup.MapGet("/{roomId:int}/status/{statusCode:int}", SongController.GetSongsFromRoomWithStatus).RequireAuthorization();
            songRoomGroup.MapPost("/{roomId:int}/{songId}", SongController.AddSongToRoom).RequireRateLimiting(RateLimitPolicies.Mutations);

            return songGroup;
        }
    }
}