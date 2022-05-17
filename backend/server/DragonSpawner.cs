using Orleans;
using Orleans.Runtime;

namespace DragonAttack
{
    class DragonSpawner : IStartupTask
    {
        private readonly ILogger<DragonSpawner> logger;
        private readonly IClusterClient clusterClient;

        public DragonSpawner(ILogger<DragonSpawner> logger, IClusterClient clusterClient)
        {
            this.logger = logger;
            this.clusterClient = clusterClient;
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            var dragonId = Guid.NewGuid();
            try
            {
                await clusterClient.GetGrain<IGameCharacterGrain>(dragonId).Spawn(new GameCharacter
                {
                    Id = dragonId,
                    Name = "Dragon",
                    CurrentHitPoints = 1000,
                    TotalHitPoints = 1000,
                    LocationAreaId = IAreaGrain.StartingArea,
                    AbilityIds = new[] 
                    {
                        Guid.Parse("7d86e255-72b0-43e6-9d64-ec19d90ae353"),
                        Guid.Parse("666e12fa-9bb8-4420-b38e-37d987447633"),
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
    }
}