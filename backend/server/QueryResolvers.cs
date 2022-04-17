namespace DragonAttack
{
    [GraphQL.GraphQLMetadata("Query")]
    public class QueryResolvers
    {
        private readonly ILogger<QueryResolvers> logger;

        public QueryResolvers(ILogger<QueryResolvers> logger)
        {
            this.logger = logger;
        }

        public int Counter()
        {
            return 0;
        }
    }
}