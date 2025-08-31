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
        var _tweeter = new TwitterAPI_TAIO();
        var TweetList = await _tweeter.PullTweets();
        Assert.True(TweetList.Count > 0);
        foreach (var tweet in TweetList)
        {
            _output.WriteLine($"Tweet from {tweet.Account} at {tweet.Date}: {tweet.TweetText}");
        }
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
