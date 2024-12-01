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
            $"Parsed and handled {parsedData.UserInfo.Count} tracks in {Stopwatch.GetElapsedTime(startTime)}"
        );

        var totalListens = parsedData.GetTotalTrackTimeListened().OrderByDescending(x => x.Value).Take(25);
        Console.WriteLine(totalListens);
        var parsedAlbumData = SpotifyParser.RequestAndReturnTrack(totalListens.Select(kvp => kvp.Key.Substring(14)).ToList()).GetAwaiter().GetResult();


        int a = 1;

        Console.WriteLine("---------------------------------------------------------------------------------------------------------------------\n                                   TOP LISTENED ALBUMS\n---------------------------------------------------------------------------------------------------------------------");

        foreach (KeyValuePair<string, int> spotifyData in totalListens)
        {
            var timeListened = TimeSpan.FromMilliseconds(spotifyData.Value);
            string timeListenedParsed = string.Format("{0:D2}h:{1:D2}m:{2:D2}s.{3:D3}ms", 
                        timeListened.Days * 24 + timeListened.Hours,
                        timeListened.Minutes, 
                        timeListened.Seconds, 
                        timeListened.Milliseconds);

            var album = parsedAlbumData.FirstOrDefault(album => album.Uri == spotifyData.Key);
            var result = album?.Name.Length > 25
                ? album.Name.Substring(0, 22) + "..."
                : album?.Name;

            Console.WriteLine(
                $"#{a, -3}: {result.PadLeft((32 + result.Length) / 2),-32} | {parsedAlbumData.FirstOrDefault(album => album.Uri == spotifyData.Key).Artists[0].Name.PadLeft((26 + parsedAlbumData.FirstOrDefault(album => album.Uri == spotifyData.Key).Artists[0].Name.Length) / 2),-26} | {timeListenedParsed, 20}"
            );
            a++;
        }
    }
}
