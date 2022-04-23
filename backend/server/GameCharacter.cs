using GraphQL;
using Orleans;
using Orleans.Streams;

namespace DragonAttack
{
    public class GameCharacter
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int TotalHitPoints { get; set; }
        public int CurrentHitPoints { get; set; }
        public Guid LocationAreaId { get; set; }
        public int CurrentHealthPercent => CurrentHitPoints * 100 / TotalHitPoints;

        public override string ToString()
        {
            return $"GameCharacter #{Id}: {Name}";
        }
    }

    public class AlreadySpawnedException : Exception
    {}

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
        Task Spawn(GameCharacter player);

        public Task<int> AttackWithAbility(Guid targetCharacterId, string abilityId);

        public Task TakeDamage(int damage);
    }

    public class GameCharacterGrain : Orleans.Grain, IGameCharacterGrain
    {
        private readonly ILogger<GameCharacterGrain> logger;
        private readonly IClusterClient clusterClient;
        private GameCharacter? State {get; set;}

        public GameCharacterGrain(ILogger<GameCharacterGrain> logger, IClusterClient clusterClient)
        {
            this.logger = logger;
            this.clusterClient = clusterClient;
        }

        private IAsyncStream<IGameCharacterEvent> EventStream => clusterClient
            .GetStreamProvider("default")
            .GetStream<IGameCharacterEvent>(this.GetPrimaryKey(), nameof(IGameCharacterEvent));

        public Task<GameCharacter> GetState() => Task.FromResult(State ?? throw new NullReferenceException());

        public async Task Spawn(GameCharacter gameCharacter)
        {
            if (State != null)
            {
                throw new AlreadySpawnedException();
            }
            State = gameCharacter;
            await clusterClient
                .GetStreamProvider("default")
                .GetStream<IAreaEvent>(IAreaGrain.StartingArea, nameof(IAreaGrain))
                .OnNextAsync(new CharacterEnteredAreaEvent
                {
                    AreaId = gameCharacter.LocationAreaId,
                    GameCharacterId = gameCharacter.Id
                });
            logger.LogInformation("Spawned character {character}", gameCharacter);
        }

        public async Task<int> AttackWithAbility(Guid targetCharacterId, string abilityId)
        {
            logger.LogInformation("Attacking {target}", targetCharacterId);
            // TODO: lookup ability and calculate damage
            var target = GrainFactory.GetGrain<IGameCharacterGrain>(targetCharacterId);
            var damage = Random.Shared.Next(10) + 1;
            await target.TakeDamage(damage);
            return damage;
        }

        public async Task TakeDamage(int damage)
        {
            if (State == null)
            {
                throw new Exception("State for character is null");
            }
            var damageTaken = Math.Min(State.CurrentHitPoints, damage);
            State.CurrentHitPoints -= damageTaken;
            logger.LogInformation("Took {damage} damage. Down to {currentHitPoints} HP", damageTaken, State.CurrentHitPoints);

            await EventStream.OnNextAsync(new AttackedEvent
            {
                Damage = damageTaken,
                ResultingHealthPercent = State.CurrentHealthPercent
            });
        }
    }
}