using MsgPack.Serialization;
using SpotifyAPI.Web;

namespace Spoofy;

static class TrackExtensions
{
    public static void SaveTrackInfo(
        this Dictionary<string, FullTrack> TrackInfo,
        string pathToData = @"/data"
    )
    {
        MessagePackSerializer dictSerializer = MessagePackSerializer.Get<object>();
        MemoryStream packedFileData = new MemoryStream();
        dictSerializer.Pack(packedFileData, TrackInfo);
        File.WriteAllBytes($"{pathToData}/TrackInfo.msgpack", packedFileData.ToArray());
    }

    public static Dictionary<string, FullTrack> OpenTrackInfo(string pathToData = @"/data")
    {
        MessagePackSerializer dictSerializer = MessagePackSerializer.Get<
            Dictionary<string, FullTrack>
        >();
        return (Dictionary<string, FullTrack>)
            dictSerializer.Unpack(
                new MemoryStream(File.ReadAllBytes($"{pathToData}/TrackInfo.msgpack"))
            );
    }
}
