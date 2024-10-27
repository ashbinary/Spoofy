using System.Text.Json.Serialization;
using SpotifyAPI.Web;

namespace Spoofy.Handlers;

class SpotifyData
{
    public List<SpotifyPlay> UserInfo { get; set; }
    public Dictionary<string, SimpleTrack> TrackInfo { get; set; }

    public void SaveTrackInfo(string pathToData = @"/data")
    {
        TrackInfo.SaveTrackInfo(pathToData);
    }
}

class SpotifyPlay
{
    [JsonPropertyName("ms_played")]
    public int TimeListened { get; set; }

    [JsonPropertyName("spotify_track_uri")]
    public string TrackID { get; set; }
}
