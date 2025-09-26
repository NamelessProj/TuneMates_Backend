using TuneMates_Backend.Controller;

namespace TuneMates_Backend.Route
{
    public static class RoomRoutes
    {
        public static RouteGroupBuilder MapRoomRoutes(this RouteGroupBuilder group)
        {
            var roomGroup = group.MapGroup("/room");

            roomGroup.MapGet("/", RoomController.GetAllRoomsFromUser).RequireAuthorization();
            roomGroup.MapGet("/{id:int}", RoomController.GetRoomById).RequireAuthorization();
            roomGroup.MapGet("/slug/{slug}", RoomController.GetRoomBySlug);
            roomGroup.MapPost("/", RoomController.CreateRoom).RequireAuthorization();
            roomGroup.MapPut("/", RoomController.EditRoom).RequireAuthorization();
            roomGroup.MapPut("/password", RoomController.EditRoomPassword).RequireAuthorization();
            roomGroup.MapDelete("/{id:int}", RoomController.DeleteRoom).RequireAuthorization();

            return roomGroup;
        }
    }
}