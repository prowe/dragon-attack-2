using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Utilities;
using Orleans;
using Orleans.Streams;

namespace DragonAttack
{
    [GraphQL.GraphQLMetadata("Subscription")]
    public class WatchCharacterResolver : ISourceStreamResolver
    {
        private readonly ILogger<WatchCharacterResolver> logger;
        private readonly IClusterClient clusterClient;

        public WatchCharacterResolver(ILogger<WatchCharacterResolver> logger, IClusterClient clusterClient)
        {
            this.logger = logger;
            this.clusterClient = clusterClient;
        }

        public void ConfigureField(TypeSettings types)
        {
            var field = types.For("Subscription").FieldFor("watchCharacter");
            field.StreamResolver = this;
            field.Resolver = new ExpressionFieldResolver<IGameCharacterEvent, IGameCharacterEvent>(ev => ev);
        }

        public ValueTask<IObservable<object?>> ResolveAsync(IResolveFieldContext context)
        {
            // var messageHandlingContext = (MessageHandlingContext)context.UserContext;
            // logger.LogInformation("Context: {keys}", string.Join(',', messageHandlingContext.Properties.Keys));

            // var playerContext = messageHandlingContext.Get<PlayerContext>("player");

            var id = context.GetArgument<Guid>("id");
            logger.LogInformation("Watching character {id}", id);
            var streamProvider = clusterClient.GetStreamProvider("default");
            var stream = streamProvider.GetStream<IGameCharacterEvent>(id, nameof(IGameCharacterEvent));
            logger.LogInformation("Got stream: {stream}", stream);
            IObservable<object?> observable = new GameCharacterStreamWrapper(stream, logger);
            return ValueTask.FromResult(observable);
        }

        private class GameCharacterStreamWrapper : IObservable<IGameCharacterEvent>
        {
            private readonly IAsyncStream<IGameCharacterEvent> stream;
            private readonly ILogger<WatchCharacterResolver> logger;

            public GameCharacterStreamWrapper(IAsyncStream<IGameCharacterEvent> stream, ILogger<WatchCharacterResolver> logger)
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