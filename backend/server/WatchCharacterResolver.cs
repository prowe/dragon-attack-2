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
            var id = context.GetArgument<Guid>("id");
            logger.LogInformation("Watching character {id}", id);
            var streamProvider = clusterClient.GetStreamProvider("default");
            var stream = streamProvider.GetStream<IGameCharacterEvent>(id, nameof(IGameCharacterEvent));
            logger.LogInformation("Got stream: {stream}", stream);
            IObservable<object?> observable = new StreamWrapper<IGameCharacterEvent>(stream);
            return ValueTask.FromResult(observable);
        }

        
    }
}