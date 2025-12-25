using TuneMates_Backend.Controller;
using TuneMates_Backend.Infrastructure.RateLimiting;

namespace TuneMates_Backend.Route
{
    public static class RoomRoutes
    {
        /// <summary>
        /// Map room-related routes to the given <see cref="RouteGroupBuilder"/>.
        /// </summary>
        /// <param name="group">The <see cref="RouteGroupBuilder"/> to map the routes to.</param>
        /// <returns>The updated <see cref="RouteGroupBuilder"/> with room routes mapped.</returns>
        public static RouteGroupBuilder MapRoomRoutes(this RouteGroupBuilder group)
        {
            var roomGroup = group.MapGroup("/rooms");

            roomGroup.MapGet("/", RoomController.GetAllRoomsFromUser).RequireRateLimiting(RateLimitPolicies.SearchTight).RequireAuthorization();
            roomGroup.MapGet("/{id:int}", RoomController.GetRoomById).RequireRateLimiting(RateLimitPolicies.SearchTight).RequireAuthorization();
            roomGroup.MapGet("/codes/{roomId:int}", RoomController.GetAllCodeOfARoom).RequireRateLimiting(RateLimitPolicies.SearchTight).RequireAuthorization();
            roomGroup.MapPost("/", RoomController.CreateRoom).RequireAuthorization();
            roomGroup.MapPost("/code", RoomController.GetRoomByCode).RequireRateLimiting(RateLimitPolicies.SearchTight);
            roomGroup.MapPost("/code/{roomId:int}", RoomController.GetCodeForARoom).RequireRateLimiting(RateLimitPolicies.Mutations).RequireAuthorization();
            roomGroup.MapPost("/slug/{slug}", RoomController.GetRoomBySlug).RequireRateLimiting(RateLimitPolicies.SearchTight);
            roomGroup.MapPut("/{roomId:int}", RoomController.EditRoom).RequireRateLimiting(RateLimitPolicies.Mutations).RequireAuthorization();
            roomGroup.MapPut("/password/{id:int}", RoomController.EditRoomPassword).RequireRateLimiting(RateLimitPolicies.Mutations).RequireAuthorization();
            roomGroup.MapDelete("/{id:int}", RoomController.DeleteRoom).RequireAuthorization();
            roomGroup.MapDelete("/code/{code}", RoomController.DeleteCode).RequireAuthorization();

            return roomGroup;
        }
    }
}