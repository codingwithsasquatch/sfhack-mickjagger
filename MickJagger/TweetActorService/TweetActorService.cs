using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;


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
    }


}
