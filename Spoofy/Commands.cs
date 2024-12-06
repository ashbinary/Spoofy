namespace Spoofy;

using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Spoofy.Handlers;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;

[Command(Description = "Shows top songs on Spotify.")]
public class Commands : ICommand
{
    [CommandParameter(0, Description = "Track, album, or artist", Name = "type")]
    public RequestType RequestEnum { get; init; } = RequestType.Track;

    [CommandOption("num", 'n', Description = "Amount of tracks / albums / artists to request.")]
    public int RequestAmount { get; init; } = 200;

    [CommandOption("path", 'p', Description = "Folder for the Spotify data files.")]
    public string DataPath { get; init; } = @"C:\Projects\Spoofy\Spoofy\data";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        INIFile APIConfig = new INIFile($"{DataPath}/client.ini");

        string ClientID = APIConfig.IniReadValue("keys", "client_id");
        string ClientSecret = APIConfig.IniReadValue("keys", "client_secret");

        SpotifyData parsedData = SpotifyParser
            .PopulateTrackData(ClientID, ClientSecret, DataPath)
            .GetAwaiter()
            .GetResult();
        parsedData.SaveTrackInfo(DataPath);

        List<object> DataResponseList = new();

        // Incredibly overcomplicated way to sort and order all the data and cut off the spotify:xxxxx: prefix for proper parsing using LINQ.
        var totalListens = parsedData
            .GetTotalDynamicListens(RequestEnum)
            .OrderByDescending(kvp => kvp.Value)
            .Take(RequestAmount);

        Type requestType = RequestEnum switch
        {
            RequestType.Track => typeof(FullTrack),
            RequestType.Album => typeof(FullAlbum),
            RequestType.Artist => typeof(FullArtist),
        };

        int prefixLength = 9 + RequestEnum.ToString().Length;
        List<string> requestKeys = totalListens.Select(kvp => kvp.Key[prefixLength..]).ToList();

        // Splits it into a certain value so it can properly go through Spotify.
        List<List<string>> requestKeysSplit = Utilities.SplitList(
            requestKeys,
            RequestEnum != RequestType.Album ? 50 : 20
        );

        foreach (List<string> requests in requestKeysSplit)
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            dynamic Responses = RequestEnum switch
            {
                RequestType.Track => await SpotifyRequest.RequestAndReturnTrack(requests),
                RequestType.Album => await SpotifyRequest.RequestAndReturnAlbum(requests),
                RequestType.Artist => await SpotifyRequest.RequestAndReturnArtist(requests),
                _ => null,
            };
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            DataResponseList.AddRange(Responses);
        }

        int CurrentRank = 1; // Iterated upon for rankings.

        foreach (KeyValuePair<string, int> spotifyData in totalListens)
        {
            // var timeListened = TimeSpan.FromMilliseconds(spotifyData.Value);
            // string timeListenedParsed = string.Format("{0:D2}h:{1:D2}m:{2:D2}s.{3:D3}ms",
            //             timeListened.Days * 24 + timeListened.Hours,
            //             timeListened.Minutes,
            //             timeListened.Seconds,
            //             timeListened.Milliseconds);

            // to do: how do i support hangul here
            var album = DataResponseList.ReturnFromUri(spotifyData.Key, RequestEnum);
            var result =
                album?.Name.Length > 20
                    ? album.Name.Substring(0, 18) + "..."
                    : album?.Name ?? "Unknown";

            Console.WriteLine(
                $"#{CurrentRank, -3}: {result, -32} | {album?.Artists[0].Name ?? "Unknown", -26} | {spotifyData.Value, 20} listens"
            );

            CurrentRank++;
        }
        // return default;
    }
}

public enum RequestType
{
    Track,
    Album,
    Artist,
}
