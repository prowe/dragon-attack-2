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

        public int Counter()
        {
            return 0;
        }

        public Task<GameCharacter> Player(Guid id, [GlobalState("playerId")] Guid playerId)
        {
            logger.LogInformation("Got playerid to the other side {playerId}", playerId);
            return clusterClient.GetGrain<IGameCharacterGrain>(id).GetState();
        }
    }
}