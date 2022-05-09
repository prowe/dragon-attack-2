using Orleans;

namespace DragonAttack
{
    public class Mutation
    {
        private readonly IClusterClient clusterClient;
        private readonly ILogger<Mutation> logger;

        public Mutation(ILogger<Mutation> logger, IClusterClient clusterClient)
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
                AbilityIds = new[]
                {
                    Guid.Parse("566c8543-4ba1-4cdf-b921-b811c3a8db52"),
                    Guid.Parse("781c7a2a-21e0-4203-ad6d-045696250ff9"),
                }
            };
            await clusterClient.GetGrain<IGameCharacterGrain>(id).Spawn(player);
            return player;
        }

        public async Task<int> UseAbility(Guid playerId, Guid abilityId, Guid targetId)
        {
            return await clusterClient.GetGrain<IGameCharacterGrain>(playerId).UseAbility(abilityId, targetId);
        }
    }
}