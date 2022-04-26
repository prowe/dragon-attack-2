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

        public Task<Area> Location([FromServices] IClusterClient clusterClient)
        {
            return clusterClient.GetGrain<IAreaGrain>(LocationAreaId).GetState();
        }  

        public int CurrentHealthPercent => CurrentHitPoints * 100 / TotalHitPoints;

        public override string ToString()
        {
            return $"GameCharacter #{Id}: {Name}";
        }
    }

    public class AlreadySpawnedException : Exception
    {}

    [UnionType("GameCharacterEvent")]
    public interface IGameCharacterEvent
    {
    }

    public class AttackedEvent : IGameCharacterEvent
    {
        public Guid Attacker { get; set; }
        public Guid Target { get; set; }
        public int Damage { get; set; }
        public int ResultingHealthPercent { get; set; }
    }

    public interface IGameCharacterGrain : IGrainWithGuidKey
    {
        public Task<GameCharacter> GetState();
        
        Task Spawn(GameCharacter player, bool isPlayerCharacter);

        public Task<int> UseAbility(string abilityId, params Guid[] targetIds);

        public Task TakeDamage(int damage, Guid sourceCharacterId);
    }

    public class GameCharacterGrain : Orleans.Grain, IGameCharacterGrain
    {
        private readonly ILogger<GameCharacterGrain> logger;
        private readonly IClusterClient clusterClient;
        private GameCharacter? State {get; set;}
        private INPCController controller;
        private IDisposable controllerTurnHandle;

        public GameCharacterGrain(ILogger<GameCharacterGrain> logger, IClusterClient clusterClient)
        {
            this.logger = logger;
            this.clusterClient = clusterClient;
        }

        private IAsyncStream<IGameCharacterEvent> EventStream => clusterClient
            .GetStreamProvider("default")
            .GetStream<IGameCharacterEvent>(this.GetPrimaryKey(), nameof(IGameCharacterEvent));

        public Task<GameCharacter> GetState() => Task.FromResult(State ?? throw new NullReferenceException());

        public async Task Spawn(GameCharacter gameCharacter, bool isPlayerCharacter = false)
        {
            if (State != null)
            {
                throw new AlreadySpawnedException();
            }
            State = gameCharacter;
            if (!isPlayerCharacter)
            {
                SetupController();
            }
            var enterAreaEvent = new CharacterEnteredAreaEvent
            {
                AreaId = gameCharacter.LocationAreaId,
                GameCharacterId = gameCharacter.Id
            };
            await clusterClient
                .GetStreamProvider("default")
                .GetStream<IAreaEvent>(enterAreaEvent.AreaId, nameof(IAreaGrain))
                .OnNextAsync(enterAreaEvent);
            logger.LogInformation("Spawned character {character}", gameCharacter);
        }

        private void SetupController()
        {
            controller = this.ServiceProvider.GetRequiredService<INPCController>();
            controllerTurnHandle = RegisterTimer(obj => controller.TakeTurn(this), null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
        }

        public async Task<int> UseAbility(string abilityId, params Guid[] targetIds)
        {
            logger.LogInformation("Attacking {targets}", targetIds);
            var damages = await Task.WhenAll(targetIds.Select(async targetId =>
            {
                var target = GrainFactory.GetGrain<IGameCharacterGrain>(targetId);
                var damage = CalculateDamage(abilityId);
                await target.TakeDamage(damage, this.GetPrimaryKey());
                return damage;
            }));
            return damages.Sum();
        }

        private int CalculateDamage(string abilityId)
        {
            // TODO: better way to do this
            return abilityId switch
            {
                "stab" => Roll(10),
                "claw" => Roll(5) + Roll(5),
                "flame-breath" => Roll(10) + 5,
                _ => 0,
            };
        }

        public async Task TakeDamage(int damage, Guid sourceCharacterId)
        {
            if (State == null)
            {
                throw new Exception("State for character is null");
            }
            var damageTaken = Math.Min(State.CurrentHitPoints, damage);
            State.CurrentHitPoints -= damageTaken;
            logger.LogInformation("Took {damage} damage. Down to {currentHitPoints} HP", damageTaken, State.CurrentHitPoints);

            var attackedEvent = new AttackedEvent
            {
                Attacker = sourceCharacterId,
                Target = this.GetPrimaryKey(),
                Damage = damageTaken,
                ResultingHealthPercent = State.CurrentHealthPercent
            };
            controller?.OnDamageTaken(attackedEvent);
            await EventStream.OnNextAsync(attackedEvent);
        }

        private static int Roll(int sides, int rolls = 1)
        {
            return Enumerable.Range(0, rolls)
                .Select((int index) => Random.Shared.Next(sides) + 1)
                .Sum();
        }
    }
}