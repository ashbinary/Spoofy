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

        List<string> requestKeys = totalListens.Select(kvp => kvp.Key[(9 + RequestEnum.ToString().Length)..]).ToList();
        // Splits it into a certain value so it can properly go through Spotify.
        List<List<string>> requestKeysSplit = Utilities.SplitList(requestKeys, RequestEnum != RequestType.Album ? 50 : 20);

        switch (RequestEnum)
        {
            case RequestType.Track:
                foreach (List<string> requests in requestKeysSplit)
                {
                    DataResponseList.AddRange(await SpotifyRequest.RequestAndReturnTrack(requests));
                }
                break;
            default:
                break;
        }

        Console.WriteLine("---------------------------------------------------------------------------------------------------------------------\n                                   TOP LISTENED ALBUMS\n---------------------------------------------------------------------------------------------------------------------");

        int a = 1;

        foreach (KeyValuePair<string, int> spotifyData in totalListens)
        {
            var timeListened = TimeSpan.FromMilliseconds(spotifyData.Value);
            string timeListenedParsed = string.Format("{0:D2}h:{1:D2}m:{2:D2}s.{3:D3}ms", 
                        timeListened.Days * 24 + timeListened.Hours,
                        timeListened.Minutes, 
                        timeListened.Seconds, 
                        timeListened.Milliseconds);

            var album = DataResponseList.OfType<FullTrack>().FirstOrDefault(album => album.Uri == spotifyData.Key);
            var result = album?.Name.Length > 25
                ? album.Name.Substring(0, 22) + "..."
                : album?.Name;

            Console.WriteLine(
                $"#{a, -3}: {result.PadLeft((32 + result.Length) / 2),-32} | {DataResponseList.OfType<FullTrack>().FirstOrDefault(album => album.Uri == spotifyData.Key).Artists[0].Name.PadLeft((26 + DataResponseList.OfType<FullTrack>().FirstOrDefault(album => album.Uri == spotifyData.Key).Artists[0].Name.Length) / 2),-26} | {spotifyData.Value, 20} listens"
            );
            a++;
        }
        // return default;
    }
}

public enum RequestType
{
    Track,
    Album,
    Artist
}
