namespace DragonAttack
{
    [GraphQL.GraphQLMetadata("Query")]
    public class QueryResolvers
    {
        private readonly CounterHolder holder;
        private readonly ILogger<QueryResolvers> logger;

        public QueryResolvers(ILogger<QueryResolvers> logger, CounterHolder holder)
        {
            this.logger = logger;
            this.holder = holder;
        }

        public int Counter()
        {
            return holder.Counter;
        }
    }
}