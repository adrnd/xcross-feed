using Xunit.Abstractions;
namespace TwitterTest;
using xcross_backend.Controllers;

using xcross_backend;

public class UnitTest1
{
    private TweetStore _tweetStore = new TweetStore();
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
        var tweeter = new TwitterAPI_TAIO(_tweetStore);
        var tweetList = await tweeter.PullTweets(); // Replace with actual method to fetch tweets
        Assert.True(tweetList.Count > 0);
        foreach (var tweet in tweetList)
        {
            _output.WriteLine($"Tweet from {tweet.Account} at {tweet.Date}: {tweet.TweetText}");
        }
    }
}
