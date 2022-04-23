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
                    CurrentHitPoints = 1000,
                    TotalHitPoints = 1000,
                });
                logger.LogWarning("Dragon spawned");
            } 
            catch (AlreadySpawnedException e)
            {
                logger.LogInformation("Dragon already spawned");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}