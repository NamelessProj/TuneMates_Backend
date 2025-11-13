using TuneMates_Backend.Controller;
using TuneMates_Backend.Infrastructure.RateLimiting;

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
            var userGroup = group.MapGroup("/users");

            userGroup.MapGet("/", UserController.GetAllUser);
            userGroup.MapGet("/me", UserController.GetCurrentUser).RequireAuthorization();
            userGroup.MapGet("/{id:int}", UserController.GetUserById);
            userGroup.MapPost("/register", UserController.Register);
            userGroup.MapPost("/login", UserController.Login);
            userGroup.MapPost("/connect/spotify/{code}/{state}", UserController.GetUserSpotifyAccessToken).RequireRateLimiting(RateLimitPolicies.Mutations).RequireAuthorization();
            userGroup.MapPost("/delete/me", UserController.DeleteUser).RequireAuthorization();
            userGroup.MapPut("/me", UserController.EditUser).RequireRateLimiting(RateLimitPolicies.Mutations).RequireAuthorization();
            userGroup.MapPut("/me/password", UserController.EditUserPassword).RequireRateLimiting(RateLimitPolicies.Mutations).RequireAuthorization();

            return userGroup;
        }
    }
}