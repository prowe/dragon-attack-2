using Orleans;

namespace DragonAttack
{
    public class Query
    {
        private readonly ILogger<Query> logger;
        private readonly IClusterClient clusterClient;

        public Query(ILogger<Query> logger, IClusterClient clusterClient)
        {
            this.logger = logger;
            this.clusterClient = clusterClient;
        }

        public Task<GameCharacter> Player([GlobalState("playerId")] Guid playerId)
        {
            return clusterClient.GetGrain<IGameCharacterGrain>(playerId).GetState();
        }
    }
}