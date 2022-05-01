using Orleans;

namespace DragonAttack
{
    class DragonSpawner : IHostedService
    {
        private readonly ILogger<DragonSpawner> logger;
        private readonly IClusterClient clusterClient;

        public DragonSpawner(ILogger<DragonSpawner> logger, IClusterClient clusterClient)
        {
            this.logger = logger;
            this.clusterClient = clusterClient;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var dragonId = Guid.Parse("78D6A1E6-F6A0-4A71-AE46-E86881B9B527");
            try
            {
                await clusterClient.GetGrain<IGameCharacterGrain>(dragonId).Spawn(new GameCharacter
                {
                    Id = dragonId,
                    Name = "Dragon",
                    IsPlayerCharacter = false,
                    CurrentHitPoints = 1000,
                    TotalHitPoints = 1000,
                    LocationAreaId = IAreaGrain.StartingArea,
                    Abilities = new List<Ability>
                    {
                        new Ability
                        {
                            Id = Guid.NewGuid(),
                            Name = "Claw",
                            Effect = AbilityEffect.Damage,
                            Dice = new DiceSpecification { Rolls = 3, Sides = 6}
                        }
                    }
                });
                await clusterClient.GetGrain<INPCControllerGrain>(dragonId).TakeControl(dragonId);
                logger.LogWarning("Dragon spawned");
            } 
            catch (AlreadySpawnedException)
            {
                logger.LogInformation("Dragon already spawned");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}