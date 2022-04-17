using GraphQL;
using Orleans;
using Orleans.Streams;

namespace DragonAttack
{
    public class GameCharacter
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int HealthPercent { get; set; }
    }

    public interface IGameCharacterGrain : IGrainWithGuidKey
    {
        public Task<int> AttackWithAbility(Guid targetCharacterId, string abilityId);

        public Task TakeDamage(int damage);
    }

    public class GameCharacterGrain : Orleans.Grain, IGameCharacterGrain
    {
        private readonly ILogger<GameCharacterGrain> logger;
        private readonly IClusterClient clusterClient;
        private int maxHitPoints = 100;
        private int currentHitPoints = 100;

        public GameCharacterGrain(ILogger<GameCharacterGrain> logger, IClusterClient clusterClient)
        {
            this.logger = logger;
            this.clusterClient = clusterClient;
        }

        private IAsyncStream<GameCharacter> GameCharacterStream
        {
            get
            {
                var streamProvider = clusterClient.GetStreamProvider("default");
                return streamProvider.GetStream<GameCharacter>(this.GetPrimaryKey(), "GameCharacter");
            }
        }
       
        public async Task<int> AttackWithAbility(Guid targetCharacterId, string abilityId)
        {
            logger.LogInformation("Attacking {target}", targetCharacterId);
            // TODO: lookup ability and calculate damage
            var target = GrainFactory.GetGrain<IGameCharacterGrain>(targetCharacterId);
            var damage = 1;
            await target.TakeDamage(damage);
            return damage;
        }

        public async Task TakeDamage(int damage)
        {
            currentHitPoints = Math.Max(0, currentHitPoints - damage);
            logger.LogInformation("Took {damage} damage. Down to {currentHitPoints} HP", damage, currentHitPoints);
            var damageEvent = new GameCharacter
            {
                Id = this.GetPrimaryKey(),
                Name = "Unknown",
                HealthPercent = currentHitPoints * 100 / maxHitPoints
            };
            await GameCharacterStream.OnNextAsync(damageEvent);
        }
    }
}