using TuneMates_Backend.Controller;

namespace TuneMates_Backend.Route
{
    public static class UserRoutes
    {
        /// <summary>
        /// Map user-related routes to the given <see cref="RouteGroupBuilder"/>.
        /// </summary>
        /// <param name="group">The <see cref="RouteGroupBuilder"/> to map the routes to.</param>
        /// <returns>The updated <see cref="RouteGroupBuilder"/> with user routes mapped.</returns>
        public static RouteGroupBuilder MapUserRoutes(this RouteGroupBuilder group)
        {
            var userGroup = group.MapGroup("/user");

            userGroup.MapGet("/", UserController.GetAllUser);
            userGroup.MapGet("/{id:int}", UserController.GetUserById);
            userGroup.MapPost("/register", UserController.Register);
            userGroup.MapPost("/login", UserController.Login);
            userGroup.MapPut("/", UserController.EditUser).RequireAuthorization();
            userGroup.MapPut("/password", UserController.EditUserPassword).RequireAuthorization();
            userGroup.MapDelete("/", UserController.DeleteUser).RequireAuthorization();

            return userGroup;
        }
    }
}