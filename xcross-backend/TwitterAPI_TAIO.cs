using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;

namespace xcross_backend.Controllers;
/// <summary>
/// Class to pull tweets from Twitter using the Twitter AIO API (https://rapidapi.com/viperscores-viperscores-default/api/twitter-aio/)
/// </summary>
public class TwitterAPI_TAIO : ControllerBase
{
    public interface ITweetStore
    {
        List<BasicTweet> TweetsList { get; }
        void AddToTweets(IEnumerable<BasicTweet> tweets);
    }

    public class TweetStore : ITweetStore
    {
        // This list lives as long as the app does
        public List<BasicTweet> TweetsList { get; set; } = new();
        int MaxAmount = 30;
        public void AddToTweets(IEnumerable<BasicTweet> newTweets)
        {
            TweetsList.AddRange(newTweets);
            TweetsList = TweetsList.OrderByDescending(t => t.Date).ToList();
            if (TweetsList.Count >= MaxAmount)
            {
                // Remove everything after the Max
                TweetsList.Slice(0, MaxAmount);
            }
        }
    }
    private readonly ITweetStore _tweetStore;
    public TwitterAPI_TAIO(ITweetStore tweetStore)
    {
        _tweetStore = tweetStore;
    }

    static List<string> usernames = ["rodekorsnorge", "ICRC"];//add more usernames as needed

    public class BasicTweet
    {
        public string TweetId { get; set; }
        public string Account { get; set; }
        public string TweetText { get; set; }
        public string Date { get; set; }//TODO: parse date and pictures properly
        public string? ProfilePic { get; set; }
        public string? MediaURL { get; set; }
    }
    public async Task<List<BasicTweet>> PullTweets()
    {

        var config = new ConfigurationBuilder()
        .AddUserSecrets<Program>()
        .Build();

        int tweetCount = 10; //number of tweets to pull per user, adjust as needed
        bool ignoreDuplicates = true; //if the ID is already on the list, we skip it early. This could be disabled to allow updating statistics like retweets, or likes.
        string? apiHost = config["ApiHost_TAIO"];
        if (apiHost == null)
        {
            apiHost = Environment.GetEnvironmentVariable("ApiHost_TAIO");
            Console.WriteLine("apiHost from env: " + apiHost);
        }
        string? apiKey = config["ApiKey_TAIO"];
        if (apiKey == null)
        {
            apiKey = Environment.GetEnvironmentVariable("ApiKey_TAIO");
            Console.WriteLine("apiKey: " + apiKey);

        }
        bool listUpdated = false;
        foreach (string user in usernames)
        {


            string? apiUri = config["ApiRequestUri_TAIO"];
            if (apiUri == null)
            {
                apiUri = Environment.GetEnvironmentVariable("ApiRequestUri_TAIO");
                Console.WriteLine("ApiRequestUri_TAIO: " + apiUri);

            }
            if (apiHost == null || apiKey == null || apiUri == null)
            {
                throw new Exception("Missing API credentials in environment variables.");
            }
            apiUri = apiUri.Replace("%COUNT%", tweetCount.ToString()) + user;
            Console.WriteLine("Environment variables loaded successfully.");

            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(apiUri),
                Headers =
            {
                { "x-rapidapi-key", apiKey },
                { "x-rapidapi-host", apiHost},
                },
            };
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                var tweetsjson = System.Text.Json.JsonDocument.Parse(body);
                var root = tweetsjson.RootElement;
                //the returned json is quite nested, so we need to take it apart bit by bit
                //TODO: add error handling for missing properties or failed attempts, we just assume it works for now
                var tweetlist_helper = root.GetProperty("user")
                                       .GetProperty("result")
                                       .GetProperty("timeline")
                                       .GetProperty("timeline")
                                       .GetProperty("instructions");
                //the API returns multiple lists, we need to identify the one with tweets since it's not always in the same spot
                //we do this by finding the longest list, which should be the one with tweets
                var identifiedList = new System.Text.Json.JsonElement();
                foreach (var key in tweetlist_helper.EnumerateArray())
                {
                    if (key.ToString().Length > identifiedList.ToString().Length)
                    {
                        identifiedList = key;
                    }
                }
                //this is where the tweets are, let's go through them
                var tweetlist = identifiedList.GetProperty("entries");
                Console.WriteLine("Tweetlist length: " + tweetlist.GetArrayLength());
                if (tweetlist.GetArrayLength() == 0)
                {
                    continue;
                }
                if (MergeTweetData(tweetlist, ignoreDuplicates) == true)
                {
                    listUpdated = true;
                    Console.WriteLine("Tweets merged.");
                }
                else
                {
                    Console.WriteLine("No no tweets.");
                }
            }

        }
        if (listUpdated)
        {
            //TODO: add even to subscribe to?
        }
        return _tweetStore.TweetsList;
    }

    bool MergeTweetData(System.Text.Json.JsonElement tweetlist, bool ignoreDuplicates = true)
    {
        List<BasicTweet> newTweets = new List<BasicTweet>();
        JsonElement tweet_legacy;
        foreach (var tweet in tweetlist.EnumerateArray())
        {
            var scrapedTweet = new BasicTweet();
            try
            {
                //we get the entryID for any tweet/item early on, we'll write to the console to check which tweets are not parsed correctly for debugging
                var result = tweet.GetProperty("content");
                string tweetID = tweet.GetProperty("entryId").GetString() ?? "no-id";
                //Console.WriteLine(tweetID);
                if (ignoreDuplicates && _tweetStore.TweetsList.Any(t => t.TweetId == tweetID))
                {
                    continue; //skip already known tweets if the flag is set
                }
                //filter out non-tweet entries in multiple steps, the try catch will handle the keys not being found, TryGetProperty would be cleaner but it didn't work for some reason
                if (result.GetProperty("entryType").ToString() != "TimelineTimelineItem" || !tweetID.StartsWith("tweet-"))
                {
                    continue;
                }
                //we skip quoted tweets for now anything that is not a "TimelineTweet", as they usually lack context which is more complex to parse
                var itemContent = result.GetProperty("itemContent");
                if (itemContent.GetProperty("itemType").ToString() != "TimelineTweet" || result.ToString().Contains("quoted_status_result") || result.ToString().Contains("quoted_status_id_str"))
                {
                    continue;
                }
                //we do some drilling (for debugging purposes), we can these intermediate variables later
                //idea: we could just hunt and parse for the "full_text" property directly, but this way we ensure we're in the right place and we can extract more info later
                var tweet_details = itemContent.GetProperty("tweet_results");
                tweet_details = tweet_details.GetProperty("result");
                tweet_legacy = tweet_details.GetProperty("legacy"); //legacy is where the details about the tweet itself are located

                var full_text = tweet_legacy.GetProperty("full_text").GetString() ?? "";
                if (full_text.Contains("RT @"))
                {
                    continue; //temporary way to skip retweets until I find a better filter
                }
                var date_helper = tweet_legacy.GetProperty("created_at").GetString() ?? "";
                if (date_helper != "")
                {
                    date_helper = DateTime.ParseExact(date_helper, "ddd MMM dd HH:mm:ss zzzz yyyy",
                    CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal).ToString("s");
                }
                var tweetdate = date_helper;
                //user information is nested deeper
                var tweet_user_details = tweet_details.GetProperty("core").GetProperty("user_results").GetProperty("result").GetProperty("legacy");
                var displayName = tweet_user_details.GetProperty("name").GetString() ?? "";
                var profilePicUrl = tweet_user_details.GetProperty("profile_image_url_https").GetString() ?? "";
                scrapedTweet = new BasicTweet
                {
                    TweetId = tweetID,
                    Account = displayName,
                    TweetText = full_text,
                    Date = tweetdate,
                    ProfilePic = profilePicUrl,
                };
            }
            catch (KeyNotFoundException ex)
            {
                //we just skip tweets that don't have the expected structure, as they probably are not standard tweets
                Console.WriteLine($"Parsing error " + ex.Message);
                throw;
            }
            string mediaURL = string.Empty;
            try
            {
                var test1 = tweet_legacy.GetProperty("entities");
                var test2 = test1.GetProperty("media").EnumerateArray();
                foreach (var medi in test2)
                {
                    mediaURL = medi.GetProperty("media_url_https").ToString();
                }
                Console.WriteLine($"Parsing error ");
            }
            catch (KeyNotFoundException)
            { mediaURL = null; }

            //if we get here, we can create our BasicTweet object
            //TODO: check what happens when we only have a picture and no text

            if (mediaURL != null || mediaURL == string.Empty) { scrapedTweet.MediaURL = mediaURL; }
            newTweets.Add(scrapedTweet);
            //add them to a local list or database as needed, for the test we just output them
            Console.WriteLine($"{scrapedTweet.Account} tweeted ID: {scrapedTweet.TweetId}: {scrapedTweet.TweetText}");
        }


        if (newTweets.Count == 0)
        {
            return false;
        }
        else
        {
            //writing only new entries to the TweetStore Interface
            _tweetStore.AddToTweets(newTweets);
            return true;
        }

    }

}