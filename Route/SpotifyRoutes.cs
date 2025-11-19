using TuneMates_Backend.Controller;
using TuneMates_Backend.Infrastructure.RateLimiting;

namespace TuneMates_Backend.Route
{
    public static class SpotifyRoutes
    {
        public static RouteGroupBuilder MapSpotifyRoutes(this RouteGroupBuilder group)
        {
            var spotifyGroup = group.MapGroup("/spotify");

            spotifyGroup.MapGet("/token", SpotifyController.GetOwnerToken).RequireAuthorization();
            spotifyGroup.MapGet("/oathlink", SpotifyController.SendUserOathLink);
            spotifyGroup.MapGet("/search/{q}/{offset:int}/{market}", SpotifyController.SearchSongs).RequireRateLimiting(RateLimitPolicies.SearchTight);
            spotifyGroup.MapGet("/playlist/me", SpotifyController.GetUserSpotifyPlaylists).RequireRateLimiting(RateLimitPolicies.SearchTight).RequireAuthorization();
            spotifyGroup.MapPost("/playlist/{roomId:int}/{songId:int}", SpotifyController.AddSongToPlaylist).RequireRateLimiting(RateLimitPolicies.Mutations).RequireAuthorization();

            return spotifyGroup;
        }
    }
}