using System.Collections;
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
}
