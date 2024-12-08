using System.Collections;
using MsgPack.Serialization;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;

namespace Spoofy;

public static class Utilities
{
    public static List<List<T>> SplitList<T>(List<T> list, int chunkSize)
    {
        return list.Select((item, index) => new { item, index })
            .GroupBy(x => x.index / chunkSize)
            .Select(g => g.Select(x => x.item).ToList())
            .ToList();
    }

    // Separate function for getting URI since FirstOrDefault is strongly typed
    public static dynamic ReturnFromUri(this List<object> data, string uri, RequestType request)
    {
#pragma warning disable CS8603 // Possible null reference return.
        return request switch
        {
            RequestType.Track => GetByType<FullTrack>(data, uri),
            RequestType.Album => GetByType<FullAlbum>(data, uri),
            RequestType.Artist => GetByType<FullArtist>(data, uri),
            _ => null,
        };
#pragma warning restore CS8603 // Possible null reference return.
    }

    // Wildly overcomplicated way of getting stuff idk man I should probably rewrite this better
    private static T GetByType<T>(List<object> data, string uri)
        where T : class
    {
        return data.OfType<T>().FirstOrDefault(item => (item as dynamic).Uri == uri);
    }

    // Milliseconds -> Readable Time for Time Listened statistics
    public static string ConvertToReadableTime(long ms)
    {
        var readableTimeSpan = TimeSpan.FromMilliseconds(ms);

        return string.Format(
            "{0:D2}h:{1:D2}m:{2:D2}s",
            readableTimeSpan.Days * 24 + readableTimeSpan.Hours,
            readableTimeSpan.Minutes,
            readableTimeSpan.Seconds
        );
    }

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

    // https://stackoverflow.com/questions/457453/remove-element-of-a-regular-array
    public static void RemoveAt<T>(ref T[] arr, int index)
    {
        for (int a = index; a < arr.Length - 1; a++)
        {
            // moving elements downwards, to fill the gap at [index]
            arr[a] = arr[a + 1];
        }
        // finally, let's decrement Array's size by one
        Array.Resize(ref arr, arr.Length - 1);
    }
}
