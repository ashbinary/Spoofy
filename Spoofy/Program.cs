using System.ComponentModel;
using System.Diagnostics;
using Spoofy.Handlers;
using SpotifyAPI.Web;

namespace Spoofy;

public class Program
{
    public static void Main(string[] args)
    {
        string dataFolder = @"C:\Projects\Spoofy\Spoofy\data";

        INIFile APIConfig = new INIFile($"{dataFolder}/client.ini");

        long startTime = Stopwatch.GetTimestamp();
        SpotifyData parsedData = SpotifyParser
            .PopulateTrackData(
                APIConfig.IniReadValue("keys", "client_id"),
                APIConfig.IniReadValue("keys", "client_secret"),
                dataFolder
            )
            .GetAwaiter()
            .GetResult();
        parsedData.SaveTrackInfo(dataFolder);
        Console.WriteLine(
            $"Completed populating track info in {Stopwatch.GetElapsedTime(startTime)}"
        );

        var totalListens = parsedData.GetTotalAlbumListens().OrderByDescending(x => x.Value);

        int amountToPrint = 20;
        int a = 0;

        foreach (KeyValuePair<string, int> spotifyData in totalListens)
        {
            Console.WriteLine(
                $"{spotifyData.Key} - {spotifyData.Value} listens"
            );
            a++;
            if (a > 20)
                break;
        }
    }
}
