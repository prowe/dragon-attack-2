using Orleans;

namespace DragonAttack
{
    [GraphQL.GraphQLMetadata("Mutation")]
    public class MutationResolvers
    {
        private readonly IClusterClient clusterClient;
        private readonly ILogger<MutationResolvers> logger;

        public MutationResolvers(ILogger<MutationResolvers> logger, IClusterClient clusterClient)
        {
            this.logger = logger;
            this.clusterClient = clusterClient;
        }

        public async Task<GameCharacter> JoinGame(string name)
        {
            var id = Guid.NewGuid();
            logger.LogInformation("Joining game {id} = {name}", id, name);
            var player = new GameCharacter
            {
                Id = id,
                Name = name,
                TotalHitPoints = 100,
                CurrentHitPoints = 100,
                LocationAreaId = IAreaGrain.StartingArea,
            };
            await clusterClient.GetGrain<IGameCharacterGrain>(id).Spawn(player);
            return player;
        }

        public async Task<int> AttackWithAbility(Guid playerId, Guid targetId, string abilityId)
        {
            return await clusterClient.GetGrain<IGameCharacterGrain>(playerId).UseAbility(abilityId, targetId);
        }
    }
}