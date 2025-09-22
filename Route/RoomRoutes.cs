namespace TuneMates_Backend.Route
{
    public static class RoomRoutes
    {
        public static RouteGroupBuilder MapRoomRoutes(this RouteGroupBuilder group)
        {
            var roomGroup = group.MapGroup("/room");

            return roomGroup;
        }
    }
}