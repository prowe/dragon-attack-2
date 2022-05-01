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
                IsPlayerCharacter = true,
                TotalHitPoints = 100,
                CurrentHitPoints = 100,
                LocationAreaId = IAreaGrain.StartingArea,
                Abilities = new List<Ability>
                {
                    new Ability
                    {
                        Id = Guid.NewGuid(),
                        Name = "Stab",
                        Dice = new DiceSpecification { Rolls = 1, Sides = 6, Constant = 0},
                        Effect = AbilityEffect.Damage
                    },
                    new Ability
                    {
                        Id = Guid.NewGuid(),
                        Name = "Slash",
                        Dice = new DiceSpecification { Rolls = 2, Sides = 4, Constant = 0},
                        Effect = AbilityEffect.Damage
                    }
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