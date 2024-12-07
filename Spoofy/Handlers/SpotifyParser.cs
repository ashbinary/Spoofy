using System.Text.Json;
using SpotifyAPI.Web;

namespace Spoofy.Handlers;

static class SpotifyParser
{
    public static SpotifyClient APIClient; // Probably not a good idea to make this accessible? But who cares
    private static readonly List<Task> APITaskList = new();

    // Sets up the Spotify API client
    public static void SetupAPI(string clientID, string clientSecret)
    {
        string tokenPath = "token"; // Path to the token file

        // Check if the token file exists, create it if it doesn't
        if (!File.Exists(tokenPath))
            File.Create(tokenPath);

        // Read token and token creation date from file
        string[] fileData = File.ReadAllText(tokenPath).Split("\n");
        string token = fileData.Length > 0 ? fileData[0] : string.Empty;
        long tokenCreationDate = fileData.Length > 1 ? Convert.ToInt64(fileData[1]) : 0;

        var clientConfig = SpotifyClientConfig.CreateDefault(); // Default Spotify API config

        // Use the existing token if it's still valid (less than ~1 hour old)
        if (
            !string.IsNullOrEmpty(token)
            && DateTimeOffset.UtcNow.ToUnixTimeSeconds() - tokenCreationDate < 3550
        )
        {
            APIClient = new SpotifyClient(clientConfig.WithToken(token));
            return; // Exit method if token is valid
        }

        // Request a new token if none is valid
        var request = new ClientCredentialsRequest(clientID, clientSecret);
        var response = new OAuthClient(clientConfig).RequestToken(request).Result;

        // Save the new token and current time to file
        File.Delete(tokenPath);
        File.WriteAllText(
            tokenPath,
            $"{response.AccessToken}\n{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
        );

        // Set up the Spotify client with the new token
        APIClient = new SpotifyClient(clientConfig.WithToken(response.AccessToken));
    }

    // Loads track data and fetches more info from the API if needed
    public static async Task PopulateTrackData(
        string clientID,
        string clientSecret,
        string pathToData = @"/data"
    )
    {
        SetupAPI(clientID, clientSecret);

        DirectoryInfo dataDirectory = new DirectoryInfo(pathToData); // Directory with data files

        // Read each JSON file in the directory and add track data to UserInfo
        foreach (FileInfo fileData in dataDirectory.GetFiles("*.json"))
        {
            SpotifyAnalyzer.spotifyData.UserInfo.AddRange(
                JsonSerializer.Deserialize<List<SpotifyPlay>>(fileData.OpenRead())
            );
        }

        // Remove entries with null TrackID
        SpotifyAnalyzer.spotifyData.UserInfo.RemoveAll(track => track.TrackID == null);

        List<string> SpotifyURIList = []; // List to store track URIs

        // Load track info if a saved file exists
        if (File.Exists(Path.Combine(pathToData, "TrackInfo.msgpack")))
            SpotifyAnalyzer.spotifyData.TrackInfo = TrackExtensions.OpenTrackInfo(pathToData);

        // Go through each track and prepare to request details for missing ones
        foreach (SpotifyPlay playedTrack in SpotifyAnalyzer.spotifyData.UserInfo)
        {
            playedTrack.TrackID = playedTrack.TrackID.Replace("spotify:track:", ""); // Remove URI prefix

            // Add to TrackInfo and URI list if not already present
            if (
                playedTrack.TrackID != null
                && !SpotifyAnalyzer.spotifyData.TrackInfo.ContainsKey(playedTrack.TrackID)
            )
            {
                SpotifyAnalyzer.spotifyData.TrackInfo.Add(playedTrack.TrackID, new FullTrack()); // Placeholder for track data
                SpotifyURIList.Add(playedTrack.TrackID);

                // If we reach 50 tracks (max for Spotify GetSeveral tracks), process the list
                if (SpotifyURIList.Count == 50)
                {
                    APITaskList.Add(RequestAndParseTrack(SpotifyURIList));
                    SpotifyURIList.Clear(); // Clear URI list
                }
            }
        }

        // Process any remaining tracks in the URI list
        if (SpotifyURIList.Count > 0)
            APITaskList.Add(RequestAndParseTrack(SpotifyURIList));

        await Task.WhenAll(APITaskList); // Wait for all API tasks to finish

        SpotifyAnalyzer.spotifyData.SaveTrackInfo(pathToData);
    }

    // Requests track info from Spotify and updates TrackInfo
    public static async Task RequestAndParseTrack(
        List<string> trackIDList
    )
    {
        List<FullTrack> trackDataList = await SpotifyRequest.RequestAndReturnTrack(trackIDList);
        // Add each track's info to TrackInfo
        foreach (FullTrack trackData in trackDataList)
        {
            lock (SpotifyAnalyzer.spotifyData.TrackInfo) // Ensure only one thread updates at a time
                SpotifyAnalyzer.spotifyData.TrackInfo[trackData.Id] = trackData;
        }
    }
}
