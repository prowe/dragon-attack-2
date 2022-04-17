using GraphQL;
using Orleans;
using Orleans.Streams;

namespace DragonAttack
{
    [GraphQL.GraphQLMetadata("Subscription")]
    public class SubscriptionResolvers
    {
        private readonly ILogger<SubscriptionResolvers> logger;
        private readonly IClusterClient clusterClient;

        public SubscriptionResolvers(ILogger<SubscriptionResolvers> logger, IClusterClient clusterClient)
        {
            this.logger = logger;
            this.clusterClient = clusterClient;
        }

        public IObservable<GameCharacter> WatchCharacterStream(Guid id)
        {
            logger.LogInformation("Watching character {id}", id);
            var streamProvider = clusterClient.GetStreamProvider("default");
            var stream = streamProvider.GetStream<GameCharacter>(id, "GameCharacter");
            logger.LogInformation("Got stream: {stream}", stream);
            return new GameCharacterStreamWrapper(stream, logger);
        }

        private class GameCharacterStreamWrapper : IObservable<GameCharacter>
        {
            private readonly IAsyncStream<GameCharacter> stream;
            private readonly ILogger<SubscriptionResolvers> logger;

            public GameCharacterStreamWrapper(IAsyncStream<GameCharacter> stream, ILogger<SubscriptionResolvers> logger)
            {
                this.stream = stream;
                this.logger = logger;
            }

            public IDisposable Subscribe(IObserver<GameCharacter> observer)
            {
                logger.LogInformation("Subscribing observer");
                var task = SubscribeAsync(observer);
                task.Wait();
                logger.LogInformation("Subscribed observer");
                return task.Result;
            }

            private async Task<IDisposable> SubscribeAsync(IObserver<GameCharacter> observer)
            {
                Func<GameCharacter, StreamSequenceToken, Task> onNext = (value, token) =>
                {
                    observer.OnNext(value);
                    return Task.CompletedTask;
                };
                var streamSubscriptionHandle = await stream.SubscribeAsync(onNext);
                return new UnSubscriber(streamSubscriptionHandle);
            }

            private class UnSubscriber : IDisposable
            {
                private readonly StreamSubscriptionHandle<GameCharacter> handle;

                public UnSubscriber(StreamSubscriptionHandle<GameCharacter> handle)
                {
                    this.handle = handle;
                }

                public void Dispose()
                {
                    handle.UnsubscribeAsync();
                }
            }

        }
    }
}