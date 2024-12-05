using SpotifyAPI.Web;

namespace Spoofy.Handlers;

static class SpotifyRequest
{
    // Requests track info from Spotify
    public static async Task<List<FullTrack>> RequestAndReturnTrack(List<string> trackIDList)
    {
        var APIRequest = new TracksRequest(trackIDList);
        TracksResponse APIResponse = await SpotifyParser.APIClient.Tracks.GetSeveral(APIRequest); // Send request

        return APIResponse.Tracks;
    }

    // Requests album info from Spotify
    public static async Task<List<FullAlbum>> RequestAndReturnAlbum(List<string> albumIDList)
    {
        var APIRequest = new AlbumsRequest(albumIDList);
        AlbumsResponse APIResponse = await SpotifyParser.APIClient.Albums.GetSeveral(APIRequest); // Send request

        return APIResponse.Albums;
    }

    // Requests artist info from Spotify
    public static async Task<List<FullArtist>> RequestAndReturnArtist(List<string> artistIDList)
    {
        var APIRequest = new ArtistsRequest(artistIDList);
        ArtistsResponse APIResponse = await SpotifyParser.APIClient.Artists.GetSeveral(APIRequest); // Send request

        return APIResponse.Artists;
    }
}
