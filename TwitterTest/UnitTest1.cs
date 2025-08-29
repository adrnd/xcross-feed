using System.Diagnostics;
using DotNetEnv;
using Xunit.Abstractions;
namespace TwitterTest;

public class UnitTest1
{
    private readonly ITestOutputHelper _output;
    public UnitTest1(ITestOutputHelper output)
    {
        _output = output;
    }
    /// <summary>
    /// This test scrapes tweets from a specified Twitter account using the Twitter AIO API via RapidAPI. In the actual implementation. Goal for the test is to ensure that we find at least one valid tweet and that the API works.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [Fact]
    public async Task ScrapeTwitter_Test()
    {
        //.env file should be put in the TwitterTest folder, the variables below are supposed to be formatted like this:
        //ApiKey_TAIO=your_rapidapi_key
        var test = Env.TraversePath().Load();
        string? apiHost = Environment.GetEnvironmentVariable("ApiHost_TAIO");
        string? apiKey = Environment.GetEnvironmentVariable("ApiKey_TAIO");
        string? apiUri = Environment.GetEnvironmentVariable("ApiRequestUri_TAIO");
        if (apiHost == null || apiKey == null || apiUri == null)
        {
            throw new Exception("Missing API credentials in environment variables.");
        }
        _output.WriteLine("Environment variables loaded successfully.");

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
            Assert.True(tweetlist.GetArrayLength() > 0); //we should have more than one tweet with an ID at this point, under any circumstances
            _output.WriteLine("Tweetlist length: " + tweetlist.GetArrayLength());
            //we use this to ensure we found at least one actual tweet with all properties
            bool actualTweetFound = false;
            foreach (var tweet in tweetlist.EnumerateArray())
            {
                try
                {
                    //we get the entryID for any tweet/item early on, we'll write to the console to check which tweets are not parsed correctly for debugging
                    var result = tweet.GetProperty("content");
                    string tweetID = tweet.GetProperty("entryId").GetString() ?? "no-id";
                    _output.WriteLine(tweetID);
                    //filter out non-tweet entries in multiple steps, the try catch will handle the keys not being found, TryGetProperty would be cleaner but it didn't work for some reason
                    if (result.GetProperty("entryType").ToString() == "TimelineTimelineModule")
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
                    var tweet_results0 = itemContent.GetProperty("tweet_results");
                    var result0 = tweet_results0.GetProperty("result");
                    var content = tweet.GetProperty("content");
                    var itemcontent = content.GetProperty("itemContent");
                    var tweet_results = itemcontent.GetProperty("tweet_results");
                    var result2 = tweet_results.GetProperty("result");
                    var legacy = result2.GetProperty("legacy");
                    var full_text = legacy.GetProperty("full_text").GetString() ?? "";
                    //if we get here, we have a tweet with a full_text (the actual content of the tweet), so we can create our BasicTweet object
                    //TODO: check what happens when we only have a picture and no text
                    var scrapedTweet = new BasicTweet
                    {
                        TweetId = tweetID,
                        Account = "BirdCalls", //TODO: actually parse the account name from the JSON
                        TweetText = full_text,
                    };
                    if (!actualTweetFound)
                    {
                        Assert.True(scrapedTweet.TweetId != null && scrapedTweet.TweetText != null && scrapedTweet.Account != null);
                        actualTweetFound = true;
                        //we only need to check this once, if we found one tweet with all properties, we're good
                    }
                    //add them to a local list or database as needed, for the test we just output them
                    _output.WriteLine($"{scrapedTweet.Account} tweeted ID: {scrapedTweet.TweetId}: {scrapedTweet.TweetText}");
                }
                catch (KeyNotFoundException ex)
                {
                    //we just skip tweets that don't have the expected structure, as they probably are not standard tweets
                    _output.WriteLine($"Parsing error " + ex.Message);

                }

            }
        }
        return;
    }

    private class BasicTweet
    {
        required public string TweetId { get; set; }
        required public string Account { get; set; }
        required public string TweetText { get; set; }
        //required public string Date { get; set; }//TODO: parse date and pictures properly
        //public string profilepic { get; set; } 
        //public string? image { get; set; }
    }
}
