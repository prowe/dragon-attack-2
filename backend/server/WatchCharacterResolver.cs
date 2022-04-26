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
            var channel = Channel.CreateBounded<IGameCharacterEvent>(100);
            
            var streamProvider = clusterClient.GetStreamProvider("default");
            var stream = streamProvider.GetStream<IGameCharacterEvent>(id, nameof(IGameCharacterEvent));
            stream.SubscribeAsync(async (IGameCharacterEvent ev, StreamSequenceToken token) => 
            {
                await channel.Writer.WriteAsync(ev);
            });
            // TODO: I think this leaks
            ISourceStream<IGameCharacterEvent> sourceStream = new InMemorySourceStream<IGameCharacterEvent>(channel);
            return ValueTask.FromResult(sourceStream);
        }
    }
}