namespace Spoofy;

using System.ComponentModel;
using System.Reflection;
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
    public required RequestType RequestEnum { get; init; } = RequestType.Track;

    [CommandOption("num", 'n', Description = "Amount of tracks / albums / artists to request.")]
    public int RequestAmount { get; init; } = 20;

    [CommandOption("path", 'p', Description = "Folder for the Spotify data files.")]
    public string DataPath { get; init; } = Path.Combine(Directory.GetCurrentDirectory(), "data");

    [CommandOption(
        "time",
        't',
        Description = "Get time listened for a number of tracks instead of the amount of listens."
    )]
    public bool IsTimeListened { get; init; } = false;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        INIFile APIConfig = new INIFile($"{DataPath}/client.ini");

        string ClientID = APIConfig.IniReadValue("keys", "client_id");
        string ClientSecret = APIConfig.IniReadValue("keys", "client_secret");

        await SpotifyParser.PopulateTrackData(ClientID, ClientSecret, DataPath);
        List<object> DataResponseList = new();

        // Incredibly overcomplicated way to sort and order all the data and cut off the spotify:xxxxx: prefix for proper parsing using LINQ.
        var totalListens = SpotifyAnalyzer
            .GetAllData(RequestEnum, IsTimeListened)
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

        string[] boxTitles = ["#"];
        string listenName = IsTimeListened ? "Time Listened" : "Listens";

        int[] genericBoxSize = [5, 27, 27, 18];
        Printer.BoxCount = 4;

        switch (RequestEnum)
        {
            case RequestType.Track:
                boxTitles = ["#", "Song Name", "Artist Name", listenName];
                break;
            case RequestType.Album:
                boxTitles = ["#", "Album Name", "Artist Name", listenName];
                break;
            case RequestType.Artist:
                boxTitles = ["#", "Artist Name", listenName];
                Printer.BoxCount = 3;
                genericBoxSize = [5, 27, 18];
                break;
            default:
                break;
        }

        Printer.BoxSize = genericBoxSize;

        Printer.PrintBoxLine();
        Printer.PrintInfoLine(boxTitles);
        Printer.PrintBoxLine();

        int CurrentRank = 1; // Iterated upon for rankings.

        foreach (KeyValuePair<string, int> spotifyData in totalListens)
        {
            var response = DataResponseList.ReturnFromUri(spotifyData.Key, RequestEnum);

            string responseName = Printer.HandleLength(response?.Name);
            string responseArtist = "";

            if (RequestEnum != RequestType.Artist)
                responseArtist = Printer.HandleLength(response?.Artists[0].Name);

            string[] boxData =
            [
                CurrentRank.ToString(),
                responseName,
                "unused",
                spotifyData.Value.ToString(),
            ];

            if (IsTimeListened)
                boxData[3] = Utilities.ConvertToReadableTime(spotifyData.Value);

            if (RequestEnum == RequestType.Artist)
                Utilities.RemoveAt(ref boxData, 2);
            else
                boxData[2] = responseArtist;

            Printer.BoxSize =
            [
                5,
                27 - responseName.Count(Printer.IsDoubleWidth),
                (RequestEnum == RequestType.Artist ? 18 : 27)
                    - responseArtist.Count(Printer.IsDoubleWidth),
                18,
            ];

            Printer.PrintInfoLine(boxData);

            CurrentRank++;
        }

        Printer.PrintBoxLine();
    }
}

public enum RequestType
{
    Track,
    Album,
    Artist,
}
