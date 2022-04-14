using GraphQL;

namespace DragonAttack
{
    [GraphQL.GraphQLMetadata("Subscription")]
    public class SubscriptionResolvers
    {
        private readonly CounterHolder holder;
        private readonly ILogger<SubscriptionResolvers> logger;

        public SubscriptionResolvers(CounterHolder holder, ILogger<SubscriptionResolvers> logger)
        {
            this.holder = holder;
            this.logger = logger;
        }

        public GameCharacter WatchCharacter(GameCharacter source)
        {
            logger.LogInformation("Resolving to myself");
            return source;
        }

        public IObservable<GameCharacter> WatchCharacterStream(string id)
        {
            logger.LogInformation("Watching character {id}", id);
            return new GameCharacterObserable(id, holder, logger);
        }
    }

    public class GameCharacterObserable : IObservable<GameCharacter>
    {
        private readonly string characterId;
        private readonly CounterHolder holder;
        private readonly ILogger<SubscriptionResolvers> logger;

        public GameCharacterObserable(string characterId, CounterHolder holder, ILogger<SubscriptionResolvers> logger)
        {
            this.characterId = characterId;
            this.holder = holder;
            this.logger = logger;
        }

        public IDisposable Subscribe(IObserver<GameCharacter> observer)
        {
            EventHandler<int> handler = (sender, newValue) => 
            {
                logger.LogInformation("Handling event for {value}", newValue);
                observer.OnNext(new GameCharacter
                {
                    Id = characterId,
                    Name = "[name here]",
                    HealthPercent = newValue
                });
            };
            holder.OnCounterChanged += handler;
            return new UnSubscriber(holder, handler);
        }
    }

    public class UnSubscriber : IDisposable
    {
        private readonly EventHandler<int> handler;
        private readonly CounterHolder holder;

        public UnSubscriber(CounterHolder holder, EventHandler<int> handler)
        {
            this.holder = holder;
            this.handler = handler;
        }

        public void Dispose() => holder.OnCounterChanged -= handler;
    }
}