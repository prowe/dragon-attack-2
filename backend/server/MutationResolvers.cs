using Orleans;

namespace DragonAttack
{
    [GraphQL.GraphQLMetadata("Mutation")]
    public class MutationResolvers
    {
        private readonly CounterHolder holder;
        private readonly IClusterClient clusterClient;
        private readonly ILogger<MutationResolvers> logger;

        public MutationResolvers(ILogger<MutationResolvers> logger, CounterHolder holder, IClusterClient clusterClient)
        {
            this.logger = logger;
            this.holder = holder;
            this.clusterClient = clusterClient;
        }

        public async Task<int> IncrementCounter()
        {
            return await clusterClient.GetGrain<ICounter>(33).Increment();
            // var newValue = holder.Counter++;
            // logger.LogInformation("Incremented to: {value}", newValue);
            // return newValue;
        }
    }
}