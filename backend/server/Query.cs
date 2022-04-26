namespace DragonAttack
{
    public class Query
    {
        private readonly ILogger<Query> logger;

        public Query(ILogger<Query> logger)
        {
            this.logger = logger;
        }

        public int Counter()
        {
            return 0;
        }
    }
}