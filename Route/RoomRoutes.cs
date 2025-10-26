using TuneMates_Backend.Controller;

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

            roomGroup.MapGet("/", RoomController.GetAllRoomsFromUser).RequireAuthorization();
            roomGroup.MapGet("/{id:int}", RoomController.GetRoomById).RequireAuthorization();
            roomGroup.MapPost("/slug/{slug}", RoomController.GetRoomBySlug);
            roomGroup.MapPost("/", RoomController.CreateRoom).RequireAuthorization();
            roomGroup.MapPut("/{roomId:int}", RoomController.EditRoom).RequireAuthorization();
            roomGroup.MapPut("/password/{id:int}", RoomController.EditRoomPassword).RequireAuthorization();
            roomGroup.MapDelete("/{id:int}", RoomController.DeleteRoom).RequireAuthorization();

            return roomGroup;
        }
    }
}