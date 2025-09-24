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
            userGroup.MapPost("/register", UserController.Register);
            userGroup.MapPost("/login", UserController.Login);
            userGroup.MapPost("/", UserController.CreateUser);
            userGroup.MapPut("/{id:int}", UserController.EditUser);
            userGroup.MapPut("/{id:int}/password", UserController.EditUserPassword);
            userGroup.MapDelete("/{id:int}", UserController.DeleteUser);

            return userGroup;
        }
    }
}