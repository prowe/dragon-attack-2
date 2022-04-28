// using GraphQL;
// using GraphQL.Resolvers;
// using GraphQL.Server.Transports.Subscriptions.Abstractions;
// using GraphQL.Utilities;
using System.Threading.Channels;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Subscriptions.InMemory;
using Orleans;
using Orleans.Streams;

namespace DragonAttack
{
    public class Subscription
    {
        private readonly ILogger<Subscription> logger;
        private readonly IClusterClient clusterClient;

        public Subscription(ILogger<Subscription> logger, IClusterClient clusterClient)
        {
            this.logger = logger;
            this.clusterClient = clusterClient;
        }

        [SubscribeAndResolve]
        public ValueTask<ISourceStream<IGameCharacterEvent>> WatchCharacter(Guid id)
        {
            logger.LogInformation("Watching character {id}", id);
            
            var streamProvider = clusterClient.GetStreamProvider("default");
            var stream = streamProvider.GetStream<IGameCharacterEvent>(id, nameof(IGameCharacterEvent));
            ISourceStream<IGameCharacterEvent> sourceStream = new OrleansStreamSourceStream<IGameCharacterEvent>(stream);
            return ValueTask.FromResult(sourceStream);
        }

        [SubscribeAndResolve]
        public ValueTask<ISourceStream<IAreaEvent>> WatchArea(Guid id)
        {
            logger.LogInformation("Watching area {id}", id);
            
            var streamProvider = clusterClient.GetStreamProvider("default");
            var stream = streamProvider.GetStream<IAreaEvent>(id, nameof(IAreaGrain));
            ISourceStream<IAreaEvent> sourceStream = new OrleansStreamSourceStream<IAreaEvent>(stream);
            return ValueTask.FromResult(sourceStream);
        }
    }
}