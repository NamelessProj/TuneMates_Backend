using TuneMates_Backend.Controller;

namespace TuneMates_Backend.Route
{
    public static class SpotifyRoutes
    {
        public static RouteGroupBuilder MapSpotifyRoutes(this RouteGroupBuilder group)
        {
            var spotifyGroup = group.MapGroup("/spotify");

            spotifyGroup.MapGet("/token", SpotifyController.GetOwnerToken).RequireAuthorization();
            spotifyGroup.MapGet("/oathlink", SpotifyController.SendUserOathLink);
            spotifyGroup.MapGet("/search/{q}/{offset:int}/{market}", SpotifyController.SearchSongs);
            spotifyGroup.MapPost("/playlist/{roomId:int}/{songId:int}", SpotifyController.AddSongToPlaylist).RequireAuthorization();

            return spotifyGroup;
        }
    }
}