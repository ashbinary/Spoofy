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

    public static Dictionary<string, int> GetTotalArtistListens()
    {
        return GetTotalListens(
            playedTrack =>
                spotifyData.TrackInfo[playedTrack.TrackID].DurationMs * (timeBarrier / 100.0)
                < playedTrack.TimeListened,
            playedTrack => spotifyData.TrackInfo[playedTrack.TrackID].Artists[0].Uri,
            playedTrack => 1
        );
    }

    public static Dictionary<string, int> GetTotalArtistTimeListened()
    {
        return GetTotalListens(
            playedTrack => true,
            playedTrack => spotifyData.TrackInfo[playedTrack.TrackID].Artists[0].Uri,
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

    public static Dictionary<string, int> GetAllData(RequestType type, bool isTimeListened)
    {
        if (isTimeListened)
        {
            switch (type)
            {
                case RequestType.Track:
                    return GetTotalTrackTimeListened();
                case RequestType.Album:
                    return GetTotalAlbumTimeListened();
                case RequestType.Artist:
                    return GetTotalArtistTimeListened();
            }
        }
        else
        {
            switch (type)
            {
                case RequestType.Track:
                    return GetTotalTrackListens();
                case RequestType.Album:
                    return GetTotalAlbumListens();
                case RequestType.Artist:
                    return GetTotalArtistListens();
            }
        }

        return null;
    }
}
