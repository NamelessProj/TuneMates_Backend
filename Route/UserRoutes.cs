using TuneMates_Backend.Controller;

namespace TuneMates_Backend.Route
{
    public static class UserRoutes
    {
        public static RouteGroupBuilder MapUserRoutes(this RouteGroupBuilder group)
        {
            var userGroup = group.MapGroup("/user");

            userGroup.MapGet("/", UserController.GetAllUser);
            userGroup.MapGet("/{id:int}", UserController.GetUserById);
            userGroup.MapPost("/", UserController.CreateUser);
            userGroup.MapPut("/{id:int}", UserController.EditUserById);
            userGroup.MapPut("/{id:int}/password", UserController.EditUserPasswordById);
            userGroup.MapDelete("/{id:int}", UserController.DeleteUserById);

            return userGroup;
        }
    }
}