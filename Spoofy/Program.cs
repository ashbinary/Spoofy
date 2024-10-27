using System.ComponentModel;
using System.Diagnostics;
using Spoofy.Handlers;
using SpotifyAPI.Web;

namespace Spoofy;

public class Program
{
    public static void Main(string[] args)
    {
        SpotifyParser trackData = new SpotifyParser();
        string dataFolder = $"{AppDomain.CurrentDomain.BaseDirectory}/data";

        INIFile APIConfig = new INIFile($"{dataFolder}/client.ini");
        trackData.SetupAPI(
            APIConfig.IniReadValue("keys", "client_id"),
            APIConfig.IniReadValue("keys", "client_secret")
        );

        long startTime = Stopwatch.GetTimestamp();
        trackData.PopulateTrackData(dataFolder).GetAwaiter().GetResult();
        TimeSpan elapsedTime = Stopwatch.GetElapsedTime(startTime);
        Console.WriteLine($"Completed populating track info in {elapsedTime}");
    }

    public static string ToHumanReadableString(TimeSpan t)
    {
        if (t.TotalSeconds <= 1)
        {
            return $@"{t:s\.ff} seconds";
        }
        if (t.TotalMinutes <= 1)
        {
            return $@"{t:%s} seconds";
        }
        if (t.TotalHours <= 1)
        {
            return $@"{t:%m} minutes";
        }
        if (t.TotalDays <= 1)
        {
            return $@"{t:%h} hours";
        }

        return $@"{t:%d} days";
    }
}
