using System.Text.Json.Serialization;

namespace TuneMates_Backend.DataBase
{
    public static class SpotifyDTO
    {
        public record PageResult<T>(
            IReadOnlyList<T> Items,
            int Limit,
            int Offset,
            int Total,
            bool HasNext,
            int? NextOffset
        );

        public record TrackDTO(
            string Id,
            string Name,
            string Artist,
            string Album,
            string AlbumImageUrl,
            int DurationMs,
            string Uri,
            string ExternalUri
        );

        public class SpotifyAlbum
        {
            public string? Name { get; set; }
            public List<SpotifyImage>? Images { get; set; }
        }

        public class SpotifyArtist
        {
            public string? Name { get; set; }
        }

        public class SpotifyImage
        {
            public string Url { get; set; } = string.Empty;
            public int Height { get; set; }
            public int Width { get; set; }
        }

        public class SpotifyTrack
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            [JsonPropertyName("duration_ms")]
            public int DurationMs { get; set; }
            public string? Uri { get; set; }
            [JsonPropertyName("external_urls")]
            public Dictionary<string, string>? ExternalUrls { get; set; }
            public SpotifyAlbum? Album { get; set; }
            public List<SpotifyArtist>? Artists { get; set; }
        }

        public class TracksContainer
        {
            public string? Href { get; set; }
            public int Limit { get; set; }
            public string? Next { get; set; }
            public int Offset { get; set; }
            public int Total { get; set; }
            public List<SpotifyTrack>? Items { get; set; }
        }

        public class SearchResponse
        {
            public TracksContainer? Tracks { get; set; }
        }

        public class Playlist
        {
            public string Description { get; set; } = string.Empty;
            public string Href { get; set; } = string.Empty;
            public string Id { get; set; } = string.Empty;
            public List<SpotifyImage>? Images { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool Public { get; set; }
            public string Uri { get; set; } = string.Empty;
            [JsonIgnore]
            public string ImageUrl => Images != null && Images.Count > 0 ? Images[0].Url : string.Empty;
        }
        
        public class PlaylistResponse
        {
            public List<Playlist>? Items { get; set; }
        }
    }
}