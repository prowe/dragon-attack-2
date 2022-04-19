using GraphQL;
using Orleans;
using Orleans.Streams;

namespace DragonAttack
{
    public interface IGameCharacterEvent
    {
    }

    public class AttackedEvent : IGameCharacterEvent
    {
        public int Damage { get; set; }
        public int ResultingHealthPercent { get; set; }
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

        private IAsyncStream<IGameCharacterEvent> EventStream => clusterClient
            .GetStreamProvider("default")
            .GetStream<IGameCharacterEvent>(this.GetPrimaryKey(), nameof(IGameCharacterEvent));
       
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
            var damageTaken = Math.Min(currentHitPoints, damage);
            currentHitPoints -= damageTaken;
            logger.LogInformation("Took {damage} damage. Down to {currentHitPoints} HP", damageTaken, currentHitPoints);
            int healthPercent = currentHitPoints * 100 / maxHitPoints;

            await EventStream.OnNextAsync(new AttackedEvent
            {
                Damage = damageTaken,
                ResultingHealthPercent = healthPercent
            });
        }
    }
}