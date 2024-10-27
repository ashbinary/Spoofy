using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using MsgPack.Serialization;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;

namespace Spoofy;

class SpotifyData
{
    public List<SpotifyPlay> UserInfo { get; }
    public Dictionary<string, SimpleTrack> TrackInfo { get; set; }
    private SpotifyClient APIClient;
    private List<Task> APITaskList = new();

    public SpotifyData()
    {
        UserInfo = new();
        TrackInfo = new();
    }

    public void SetupAPI(string clientID, string clientSecret)
    {
        string tokenPath = "token";

        // Ensure token file exists or create it if it doesn't
        if (!File.Exists(tokenPath))
            File.Create(tokenPath);

        string[] fileData = File.ReadAllText(tokenPath).Split("\n");

        string token = fileData[0];
        long tokenCreationDate = Convert.ToInt64(fileData[1]);

        var clientConfig = SpotifyClientConfig.CreateDefault();

        // Check if we have a saved token
        if (!string.IsNullOrEmpty(token))
        {
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - tokenCreationDate < 3550)
            {
                Console.WriteLine(
                    $"Existing token is valid, has {3600 - (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - tokenCreationDate)} seconds until expiration."
                );
                APIClient = new SpotifyClient(clientConfig.WithToken(token));
                return;
            }

            Console.WriteLine("Token is invalid, requesting a new one.");
        }

        // Request a new token since we either don't have one or the old one is invalid
        var request = new ClientCredentialsRequest(clientID, clientSecret);
        var response = new OAuthClient(clientConfig).RequestToken(request).Result;

        // Save the new token
        File.Delete(tokenPath);
        File.WriteAllText(
            tokenPath,
            $"{response.AccessToken}\n{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
        );

        // Initialize the Spotify client with the new token
        APIClient = new SpotifyClient(clientConfig.WithToken(response.AccessToken));
        Console.WriteLine("New token acquired and saved.");
    }

    public async void PopulateTrackData(string pathToData = @"/data")
    {
        DirectoryInfo dataDirectory = new DirectoryInfo(pathToData);

        foreach (FileInfo fileData in dataDirectory.GetFiles("*.json"))
        {
            UserInfo.AddRange(JsonSerializer.Deserialize<List<SpotifyPlay>>(fileData.OpenRead()));
        }

        UserInfo.RemoveAll(playedTrack => playedTrack.TrackID == null);

        List<string> SpotifyURIList = new List<string>();
        OpenTrackInfo($"{AppDomain.CurrentDomain.BaseDirectory}/data");

        foreach (SpotifyPlay playedTrack in UserInfo)
        {
            playedTrack.TrackID = playedTrack.TrackID.Replace("spotify:track:", "");

            if (playedTrack.TrackID != null && !TrackInfo.ContainsKey(playedTrack.TrackID))
            {
                TrackInfo.Add(playedTrack.TrackID, new SimpleTrack()); // Instantiate a null value here in order to prevent other copies of the song from coming through
                SpotifyURIList.Add(playedTrack.TrackID);

                if (SpotifyURIList.Count == 50)
                {
                    APITaskList.Add(RequestAndParseTrack(SpotifyURIList));
                    SpotifyURIList = []; // Empty the URI list
                }
            }
        }

        if (SpotifyURIList.Count > 0)
            APITaskList.Add(RequestAndParseTrack(SpotifyURIList)); // make one more call for the remaining songs

        await Task.WhenAll(APITaskList);
    }

    public async Task RequestAndParseTrack(List<string> trackIDList)
    {
        TracksRequest APIRequest = new TracksRequest(trackIDList);
        TracksResponse APIResponse = APIClient.Tracks.GetSeveral(APIRequest).Result;

        foreach (SimpleTrack trackData in ParseToSimpleTrack(APIResponse))
        {
            lock (TrackInfo)
                TrackInfo[trackData.Id] = trackData;
            Console.WriteLine($"Added {trackData.Name} to database!");
        }
    }

    public List<SimpleTrack> ParseToSimpleTrack(TracksResponse response)
    {
        List<SimpleTrack> simpleTrackData = new();

        foreach (FullTrack fullTrackData in response.Tracks)
        {
            simpleTrackData.Add(
                new SimpleTrack // Deep copy
                {
                    Artists = fullTrackData.Artists,
                    AvailableMarkets = fullTrackData.AvailableMarkets,
                    DiscNumber = fullTrackData.DiscNumber,
                    DurationMs = fullTrackData.DurationMs,
                    Explicit = fullTrackData.Explicit,
                    ExternalUrls = fullTrackData.ExternalUrls,
                    Href = fullTrackData.Href,
                    Id = fullTrackData.Id,
                    IsPlayable = fullTrackData.IsPlayable,
                    LinkedFrom = fullTrackData.LinkedFrom,
                    Name = fullTrackData.Name,
                    PreviewUrl = fullTrackData.PreviewUrl,
                    TrackNumber = fullTrackData.TrackNumber,
                    Uri = fullTrackData.Uri,
                }
            );
        }

        return simpleTrackData;
    }

    public void SaveTrackInfo(string pathToData = @"/data")
    {
        MessagePackSerializer dictSerializer = MessagePackSerializer.Get<object>();
        MemoryStream packedFileData = new MemoryStream();
        dictSerializer.Pack(packedFileData, TrackInfo);
        File.WriteAllBytes($"{pathToData}/TrackInfo.msgpack", packedFileData.ToArray());
    }

    void OpenTrackInfo(string pathToData = @"/data")
    {
        MessagePackSerializer dictSerializer = MessagePackSerializer.Get<
            Dictionary<string, SimpleTrack>
        >();
        TrackInfo =
            (Dictionary<string, SimpleTrack>)
                dictSerializer.Unpack(
                    new MemoryStream(File.ReadAllBytes($"{pathToData}/TrackInfo.msgpack"))
                );
    }
}

class SpotifyPlay
{
    [JsonPropertyName("ms_played")]
    public int TimeListened { get; set; }

    [JsonPropertyName("spotify_track_uri")]
    public string TrackID { get; set; }
}
