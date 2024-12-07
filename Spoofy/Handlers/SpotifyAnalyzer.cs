using SpotifyAPI.Web.Http;

namespace Spoofy.Handlers;

static class SpotifyAnalyzer
{
    public static SpotifyData spotifyData = new SpotifyData();
    public static double timeBarrier = 33.3;

    public static Dictionary<string, int> GetTotalTrackListens()
    {
        return GetTotalListens(
            playedTrack =>
                spotifyData.TrackInfo[playedTrack.TrackID].DurationMs * (timeBarrier / 100.0)
                < playedTrack.TimeListened,
            playedTrack => spotifyData.TrackInfo[playedTrack.TrackID].Uri,
            playedTrack => 1
        );
    }

    public static Dictionary<string, int> GetTotalTrackTimeListened()
    {
        return GetTotalListens(
            playedTrack => true,
            playedTrack => spotifyData.TrackInfo[playedTrack.TrackID].Uri,
            playedTrack => playedTrack.TimeListened
        );
    }

    public static Dictionary<string, int> GetTotalAlbumListens()
    {
        return GetTotalListens(
            playedTrack =>
                spotifyData.TrackInfo[playedTrack.TrackID].DurationMs * (timeBarrier / 100.0)
                < playedTrack.TimeListened,
            playedTrack => spotifyData.TrackInfo[playedTrack.TrackID].Album.Uri,
            playedTrack => 1
        );
    }

    public static Dictionary<string, int> GetTotalAlbumTimeListened()
    {
        return GetTotalListens(
            playedTrack => true,
            playedTrack => spotifyData.TrackInfo[playedTrack.TrackID].Album.Uri,
            playedTrack => playedTrack.TimeListened
        );
    }

    public static Dictionary<string, int> GetTotalListens(
        Func<SpotifyPlay, bool> condition,
        Func<SpotifyPlay, string> keySelector,
        Func<SpotifyPlay, int> incrementSelector
    )
    {
        Dictionary<string, int> TotalListens = new();

        if (spotifyData == null)
        {
            Console.WriteLine("this tea");
            throw new ArgumentNullException(nameof(spotifyData), "spotifyData cannot be null");
        }

        if (spotifyData.UserInfo == null)
        {
            Console.WriteLine("this tea 2");
            throw new ArgumentNullException(nameof(spotifyData.UserInfo), "UserInfo cannot be null");
        }

        foreach (SpotifyPlay playedTrack in spotifyData.UserInfo)
        {
            // Apply the custom condition instead of a hardcoded if statement
            if (condition(playedTrack))
            {
                string key = keySelector(playedTrack);
                TotalListens[key] =
                    TotalListens.GetValueOrDefault(key, 0) + incrementSelector(playedTrack);
            }
        }

        return TotalListens;
    }

    public static Dictionary<string, int> GetTotalDynamicListens(RequestType type)
    {
        switch (type)
        {
            case RequestType.Track:
                return GetTotalTrackListens();
            case RequestType.Album:
                return GetTotalAlbumListens();
            case RequestType.Artist:
                return null; // TBD
        }

        return null;
    }
}
