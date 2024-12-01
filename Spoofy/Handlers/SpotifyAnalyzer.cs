namespace Spoofy.Handlers;

static class SpotifyAnalyzer
{
    public static Dictionary<string, int> GetTotalTrackListens(
        this SpotifyData data,
        double timeBarrier = 33.3
    )
    {
        return data.GetTotalListens(
            playedTrack =>
                data.TrackInfo[playedTrack.TrackID].DurationMs * (timeBarrier / 100.0)
                < playedTrack.TimeListened,
            playedTrack => playedTrack.TrackID,
            playedTrack => 1
        );
    }

    public static Dictionary<string, int> GetTotalTrackTimeListened(this SpotifyData data)
    {
        return data.GetTotalListens(
            playedTrack => true,
            playedTrack => data.TrackInfo[playedTrack.TrackID].Uri,
            playedTrack => playedTrack.TimeListened
        );
    }

    public static Dictionary<string, int> GetTotalAlbumListens(
        this SpotifyData data,
        double timeBarrier = 33.3
    )
    {
        return data.GetTotalListens(
            playedTrack =>
                data.TrackInfo[playedTrack.TrackID].DurationMs * (timeBarrier / 100.0)
                < playedTrack.TimeListened,
            playedTrack => data.TrackInfo[playedTrack.TrackID].Album.Uri,
            playedTrack => 1
        );
    }

    public static Dictionary<string, int> GetTotalAlbumTimeListened(this SpotifyData data)
    {
        return data.GetTotalListens(
            playedTrack => true,
            playedTrack => data.TrackInfo[playedTrack.TrackID].Album.Uri,
            playedTrack => playedTrack.TimeListened
        );
    }

    public static Dictionary<string, int> GetTotalListens(
        this SpotifyData data,
        Func<SpotifyPlay, bool> condition,
        Func<SpotifyPlay, string> keySelector,
        Func<SpotifyPlay, int> incrementSelector
    )
    {
        Dictionary<string, int> TotalListens = new();

        foreach (SpotifyPlay playedTrack in data.UserInfo)
        {
            // Apply the custom condition instead of a hardcoded if statement
            if (condition(playedTrack))
            {
                string key = keySelector(playedTrack);
                TotalListens[key] =
                    TotalListens.GetValueOrDefault(key, 0)
                    + incrementSelector(playedTrack);
            }
        }

        return TotalListens;
    }
}
