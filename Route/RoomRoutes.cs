using TuneMates_Backend.Controller;

namespace TuneMates_Backend.Route
{
    public static class RoomRoutes
    {
        public static RouteGroupBuilder MapRoomRoutes(this RouteGroupBuilder group)
        {
            var roomGroup = group.MapGroup("/room");

            roomGroup.MapGet("/{id:int}", RoomController.GetRoomById);
            roomGroup.MapGet("/slug/{slug:string}", RoomController.GetRoomBySlug);
            roomGroup.MapGet("/user/{id:int}", RoomController.GetAllRoomsFromUser);
            roomGroup.MapPost("/", RoomController.CreateRoom);
            roomGroup.MapPut("/{id:int}", RoomController.EditRoom);
            roomGroup.MapPut("/{id:int}/password", RoomController.EditRoomPassword);
            roomGroup.MapDelete("/{id:int}", RoomController.DeleteRoom);

            return roomGroup;
        }
    }
}