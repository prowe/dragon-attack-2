namespace DragonAttack
{
    [GraphQL.GraphQLMetadata("Mutation")]
    public class MutationResolvers
    {
        private readonly CounterHolder holder;
        private readonly ILogger<MutationResolvers> logger;

        public MutationResolvers(ILogger<MutationResolvers> logger, CounterHolder holder)
        {
            this.logger = logger;
            this.holder = holder;
        }

        public int IncrementCounter()
        {
            var newValue = holder.Counter++;
            logger.LogInformation("Incremented to: {value}", newValue);
            return newValue;
        }
    }
}