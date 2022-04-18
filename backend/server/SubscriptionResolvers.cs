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

        public IObservable<IGameCharacterEvent> WatchCharacterStream(Guid id)
        {
            logger.LogInformation("Watching character {id}", id);
            var streamProvider = clusterClient.GetStreamProvider("default");
            var stream = streamProvider.GetStream<IGameCharacterEvent>(id, nameof(IGameCharacterEvent));
            logger.LogInformation("Got stream: {stream}", stream);
            return new GameCharacterStreamWrapper(stream, logger);
        }

        private class GameCharacterStreamWrapper : IObservable<IGameCharacterEvent>
        {
            private readonly IAsyncStream<IGameCharacterEvent> stream;
            private readonly ILogger<SubscriptionResolvers> logger;

            public GameCharacterStreamWrapper(IAsyncStream<IGameCharacterEvent> stream, ILogger<SubscriptionResolvers> logger)
            {
                this.stream = stream;
                this.logger = logger;
            }

            public IDisposable Subscribe(IObserver<IGameCharacterEvent> observer)
            {
                logger.LogInformation("Subscribing observer");
                var task = SubscribeAsync(observer);
                task.Wait();
                logger.LogInformation("Subscribed observer");
                return task.Result;
            }

            private async Task<IDisposable> SubscribeAsync(IObserver<IGameCharacterEvent> observer)
            {
                Func<IGameCharacterEvent, StreamSequenceToken, Task> onNext = (value, token) =>
                {
                    observer.OnNext(value);
                    return Task.CompletedTask;
                };
                var streamSubscriptionHandle = await stream.SubscribeAsync(onNext);
                return new UnSubscriber<IGameCharacterEvent>(streamSubscriptionHandle);
            }

            private class UnSubscriber<T> : IDisposable
            {
                private readonly StreamSubscriptionHandle<T> handle;

                public UnSubscriber(StreamSubscriptionHandle<T> handle)
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