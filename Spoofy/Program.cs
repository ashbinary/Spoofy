using System.ComponentModel;
using SpotifyAPI.Web;

namespace Spoofy;

public class Program
{
    public static void Main(string[] args)
    {
        SpotifyData trackData = new SpotifyData();

        INIFile APIConfig = new INIFile($"{AppDomain.CurrentDomain.BaseDirectory}/data/client.ini");
        trackData.SetupAPI(
            APIConfig.IniReadValue("keys", "client_id"),
            APIConfig.IniReadValue("keys", "client_secret")
        );
        trackData.PopulateTrackData($"{AppDomain.CurrentDomain.BaseDirectory}/data");

        foreach (SimpleTrack data in trackData.TrackInfo.Values)
        {
            Console.WriteLine(data.Name);
        }
    }
}
