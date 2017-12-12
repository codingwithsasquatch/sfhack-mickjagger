using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using TweetActorService.Interfaces;
using Tweetinvi;


namespace TweetActorService
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class TweetActorService : Actor, ITweetActorService, IRemindable
    {
        private static HttpClient _httpClient;
        ActorService myActorService;
        private const string ReminderName = "TweetReminder";
        private const string StateName = "TweetData";

        /// <summary>
        /// Initializes a new instance of TweetActorService
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public TweetActorService(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
            myActorService = actorService;
            _httpClient = new HttpClient();
        }

        public async Task ReceiveReminderAsync(string reminderName, 
            byte[] state, 
            TimeSpan dueTime, 
            TimeSpan period)
        {
            if (reminderName.Equals(ReminderName, StringComparison.OrdinalIgnoreCase))
            
                // go out and get the tweet messages

                // store the tweet information in the state dictionary

                await this.StateManager.SetStateAsync<string>(StateName, "jsonstuff");
            }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            // The StateManager is this actor's private state store.
            // Data stored in the StateManager will be replicated for high-availability for actors that use volatile or persisted state storage.
            // Any serializable object can be saved in the StateManager.
            // For more information, see https://aka.ms/servicefabricactorsstateserialization


            return this.StateManager.TryAddStateAsync("jsonstuff", 0);
        }
        public async Task StartProcessingAsync(CancellationToken cancellationToken)
        {
            try
            {
                // the first time StartProcessingAsync is called, there won't be a reminder, so
                // an exception will be thrown and then the reminder parameters will be set
                this.GetReminder(ReminderName);

                //a reminder exists, go add state in to the actor

                bool added = await this.StateManager.TryAddStateAsync<string>(StateName, "jsonstuff");

                if (!added)
                {
                    // value already exists, which means processing has already started.
                    throw new InvalidOperationException("Processing for this actor has already started.");
                }
            }
            catch (ReminderNotFoundException)
            {
                await this.RegisterReminderAsync(ReminderName, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10));
            }
        }

        Task<string> ITweetActorService.GetTweetAsync(string accountToSearch, string querytext)
        {
            var appCreds = Auth.SetApplicationOnlyCredentials("UxM5snIMFxzXI5P2pEcRRsrDs", "LVt2gs9UopjFDvJ0UGF1hOQ4XgtwBWJD1pejTQYdBbG1gqThUC", true);
            Auth.InitializeApplicationOnlyCredentials(appCreds);

            Auth.SetCredentials(appCreds);

            return Task.FromResult(Tweetinvi.Json.SearchJson.SearchTweets(querytext + " AND from:" + accountToSearch));
        }

        public async Task<double> GetSentimentScore(string tweetId, string tweetText)
        {
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "6b2d3f7ec0e94d46a5113007952254ed");
            var uri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";
            var contentString = string.Concat("{\"documents\":[{\"language\":\"en\",\"id\":\"" + tweetId + "\",\"text\":\"" + tweetText + "\"}]}");
            var content = new StringContent(contentString, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(uri, content);
            if (response.IsSuccessStatusCode)
            {
                var contents = await response.Content.ReadAsStringAsync();
                dynamic body = JsonConvert.DeserializeObject<dynamic>(contents);
                return body.documents[0].score;
            }

            // If there's an issue, we'll just return a mediocre 0.5...
            return 0.5;
        }

    }


}
