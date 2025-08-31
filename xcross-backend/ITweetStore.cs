
using static xcross_backend.Controllers.TwitterAPI_TAIO;

namespace xcross_backend.Controllers
{
    /// <summary>
    /// Interface to get the TweetsList or add new tweets. Could/should be extracted into a seperate class outside of the current API.
    /// </summary>
    public interface ITweetStore
    {
        List<BasicTweet> TweetsList { get; }
        void AddToTweets(IEnumerable<BasicTweet> tweets);
    }

    public class TweetStore : ITweetStore
    {
        //this list remains until the server or app is restarted, needs some refinement as it is not read-only
        public List<BasicTweet> TweetsList { get; set; } = new();
        int MaxAmount = 30;
        public void AddToTweets(IEnumerable<BasicTweet> newTweets)
        {
            TweetsList.AddRange(newTweets);
            TweetsList = TweetsList.OrderByDescending(t => t.Date).ToList();
            if (TweetsList.Count >= MaxAmount)
            {
                // Remove everything after the Max
                TweetsList = TweetsList[..MaxAmount];
            }
        }
    }
}