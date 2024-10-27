using MsgPack.Serialization;
using SpotifyAPI.Web;

namespace Spoofy;

static class TrackExtensions
{
    public static List<SimpleTrack> ParseToSimpleTrack(this TracksResponse response)
    {
        List<SimpleTrack> simpleTrackData = new();

        foreach (FullTrack fullTrackData in response.Tracks)
        {
            simpleTrackData.Add(
                new SimpleTrack // Deep copy
                {
                    Artists = fullTrackData.Artists,
                    AvailableMarkets = fullTrackData.AvailableMarkets,
                    DiscNumber = fullTrackData.DiscNumber,
                    DurationMs = fullTrackData.DurationMs,
                    Explicit = fullTrackData.Explicit,
                    ExternalUrls = fullTrackData.ExternalUrls,
                    Href = fullTrackData.Href,
                    Id = fullTrackData.Id,
                    IsPlayable = fullTrackData.IsPlayable,
                    LinkedFrom = fullTrackData.LinkedFrom,
                    Name = fullTrackData.Name,
                    PreviewUrl = fullTrackData.PreviewUrl,
                    TrackNumber = fullTrackData.TrackNumber,
                    Uri = fullTrackData.Uri,
                }
            );
        }

        return simpleTrackData;
    }

    public static void SaveTrackInfo(
        this Dictionary<string, SimpleTrack> TrackInfo,
        string pathToData = @"/data"
    )
    {
        MessagePackSerializer dictSerializer = MessagePackSerializer.Get<object>();
        MemoryStream packedFileData = new MemoryStream();
        dictSerializer.Pack(packedFileData, TrackInfo);
        File.WriteAllBytes($"{pathToData}/TrackInfo.msgpack", packedFileData.ToArray());
    }

    public static Dictionary<string, SimpleTrack> OpenTrackInfo(string pathToData = @"/data")
    {
        MessagePackSerializer dictSerializer = MessagePackSerializer.Get<
            Dictionary<string, SimpleTrack>
        >();
        return (Dictionary<string, SimpleTrack>)
            dictSerializer.Unpack(
                new MemoryStream(File.ReadAllBytes($"{pathToData}/TrackInfo.msgpack"))
            );
    }
}
