using System.Text.Json;
using SpotifyAPI.Web;

namespace Spoofy.Handlers;

static class SpotifyParser
{
    private static SpotifyClient APIClient; 
    private static List<Task> APITaskList = new();

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
        if (!string.IsNullOrEmpty(token) && DateTimeOffset.UtcNow.ToUnixTimeSeconds() - tokenCreationDate < 3550)
        {
            Console.WriteLine($"Existing token is valid, expires in {3600 - (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - tokenCreationDate)} seconds.");
            APIClient = new SpotifyClient(clientConfig.WithToken(token));
            return; // Exit method if token is valid
        }

        Console.WriteLine("Token is invalid, requesting a new one.");

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
        Console.WriteLine("New token acquired and saved.");
    }

    // Loads track data and fetches more info from the API if needed
    public static async Task PopulateTrackData(this SpotifyData spotifyData, string pathToData = @"/data")
    {
        DirectoryInfo dataDirectory = new DirectoryInfo(pathToData); // Directory with data files

        // Read each JSON file in the directory and add track data to UserInfo
        foreach (FileInfo fileData in dataDirectory.GetFiles("*.json"))
        {
            spotifyData.UserInfo.AddRange(JsonSerializer.Deserialize<List<SpotifyPlay>>(fileData.OpenRead()));
        }

        // Remove entries with null TrackID
        spotifyData.UserInfo.RemoveAll(track  => track.TrackID == null);

        List<string> SpotifyURIList = new List<string>(); // List to store track URIs

        // Load track info if a saved file exists
        if (File.Exists(Path.Combine(pathToData, "TrackInfo.msgpack")))
            TrackExtensions.OpenTrackInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"));

        // Go through each track and prepare to request details for missing ones
        foreach (SpotifyPlay playedTrack in spotifyData.UserInfo)
        {
            playedTrack.TrackID = playedTrack.TrackID.Replace("spotify:track:", ""); // Remove URI prefix

            // Add to TrackInfo and URI list if not already present
            if (playedTrack.TrackID != null && !spotifyData.TrackInfo.ContainsKey(playedTrack.TrackID))
            {
                spotifyData.TrackInfo.Add(playedTrack.TrackID, new SimpleTrack()); // Placeholder for track data
                SpotifyURIList.Add(playedTrack.TrackID);

                // If we reach 50 tracks, process the list
                if (SpotifyURIList.Count == 50)
                {
                    APITaskList.Add(spotifyData.RequestAndParseTrack(SpotifyURIList));
                    SpotifyURIList.Clear(); // Clear URI list
                }
            }
        }

        // Process any remaining tracks in the URI list
        if (SpotifyURIList.Count > 0)
            APITaskList.Add(spotifyData.RequestAndParseTrack(SpotifyURIList));

        await Task.WhenAll(APITaskList); // Wait for all API tasks to finish
    }

    // Requests track info from Spotify and updates TrackInfo
    public static async Task RequestAndParseTrack(this SpotifyData spotifyData, List<string> trackIDList)
    {
        var APIRequest = new TracksRequest(trackIDList); // Create request for multiple tracks
        TracksResponse APIResponse = await APIClient.Tracks.GetSeveral(APIRequest); // Send request

        // Add each track's info to TrackInfo
        foreach (SimpleTrack trackData in APIResponse.ParseToSimpleTrack())
        {
            lock (spotifyData.TrackInfo) // Ensure only one thread updates at a time
                spotifyData.TrackInfo[trackData.Id] = trackData;

            Console.WriteLine($"Added {trackData.Name} to database!"); // Log each added track
        }
    }
}