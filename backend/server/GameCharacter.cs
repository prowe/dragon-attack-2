using Orleans;
using Orleans.Runtime;
using Orleans.Streams;

namespace DragonAttack
{
    public class GameCharacter
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "Unknown";
        public int TotalHitPoints { get; set; }
        public int CurrentHitPoints { get; set; }
        internal Guid LocationAreaId { get; set; }
        internal IEnumerable<Guid> AbilityIds { get; set; } = Enumerable.Empty<Guid>();
        public List<Ability> Abilities([Service] IDictionary<Guid, Ability> abilityMap)
        {
            return (AbilityIds ?? Enumerable.Empty<Guid>())
                .Select(id => abilityMap[id])
                .ToList();
        }

        public Task<Area> Location([Service] IClusterClient clusterClient)
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

    public interface IGameCharacterEvent
    {
        public string Name { get; }
    }

    public class HealthChangedEvent : IGameCharacterEvent
    {
        internal Guid SourceId { get; set; }
        internal Guid TargetId { get; set; }
        public int Difference { get; set; }
        public int ResultingHealthPercent { get; set; }
        public string Name => Difference < 0 ? "Damage Taken" : $"Healed";

        public Task<GameCharacter> Target([Service] IClusterClient clusterClient)
        {
            return clusterClient.GetGrain<IGameCharacterGrain>(TargetId).GetState();
        }
    }

    public interface IGameCharacterGrain : IGrainWithGuidKey
    {
        public Task<GameCharacter> GetState();
        
        Task Spawn(GameCharacter player);
        public Task Despawn();

        public Task<int> UseAbility(Guid abilityId, params Guid[] targetIds);

        public Task ModifyHealth(int damage, Guid sourceCharacterId);
    }

    public class GameCharacterGrain : Orleans.Grain, IGameCharacterGrain
    {
        private readonly ILogger<GameCharacterGrain> logger;
        private readonly IClusterClient clusterClient;
        private readonly IDictionary<Guid, Ability> abilityMap;
        private readonly IPersistentState<GameCharacter> gameCharacterState;

        public GameCharacterGrain(
            ILogger<GameCharacterGrain> logger, 
            IClusterClient clusterClient,
            IDictionary<Guid, Ability> abilityMap,
            [PersistentState("GameCharacter")] IPersistentState<GameCharacter> gameCharacterState)
        {
            this.logger = logger;
            this.clusterClient = clusterClient;
            this.abilityMap = abilityMap;
            this.gameCharacterState = gameCharacterState;
        }

        private IAsyncStream<IGameCharacterEvent> EventStream => clusterClient
            .GetStreamProvider("default")
            .GetStream<IGameCharacterEvent>(this.GetPrimaryKey(), nameof(IGameCharacterEvent));

        public Task<GameCharacter> GetState() => Task.FromResult(gameCharacterState.State ?? throw new NullReferenceException());

        public async Task Spawn(GameCharacter gameCharacter)
        {
            if (gameCharacterState.RecordExists)
            {
                throw new AlreadySpawnedException();
            }
            gameCharacterState.State = gameCharacter;
            var enterAreaEvent = new CharacterEnteredAreaEvent
            {
                AreaId = gameCharacter.LocationAreaId,
                GameCharacterId = gameCharacter.Id
            };
            await FireEventToArea(enterAreaEvent);
            await gameCharacterState.WriteStateAsync();
            logger.LogInformation("Spawned character {character}", gameCharacter);
        }

        public async Task Despawn()
        {
            var exitAreaEvent = new CharacterExitedAreaEvent
            {
                AreaId = gameCharacterState.State.LocationAreaId,
                GameCharacterId = this.GetPrimaryKey(),
            };
            await FireEventToArea(exitAreaEvent);
            logger.LogInformation("Despawned character {character}", exitAreaEvent);
        }

        private Task FireEventToArea(IAreaEvent areaEvent)
        {
            return clusterClient
                .GetStreamProvider("default")
                .GetStream<IAreaEvent>(areaEvent.AreaId, nameof(IAreaGrain))
                .OnNextAsync(areaEvent);
        }

        public async Task<int> UseAbility(Guid abilityId, params Guid[] targetIds)
        {
            logger.LogInformation("Attacking {targets}", targetIds);
            var ability = abilityMap[abilityId];
            var multiplyer = ability.Effect == AbilityEffect.Damage ? -1 : 1;
            var damages = await Task.WhenAll(targetIds.Select(async targetId =>
            {
                var target = GrainFactory.GetGrain<IGameCharacterGrain>(targetId);
                var delta = multiplyer * ability.Dice.Roll();
                await target.ModifyHealth(delta, this.GetPrimaryKey());
                return delta;
            }));
            return damages.Sum();
        }

        public async Task ModifyHealth(int attemptedDelta, Guid sourceCharacterId)
        {
            if (!gameCharacterState.RecordExists)
            {
                throw new Exception("State for character is null");
            }
            var actualDelta = ComputeActualHealthChange(attemptedDelta);
            gameCharacterState.State.CurrentHitPoints += actualDelta;
            logger.LogInformation("Health changed by {actualDelta} to {currentHitPoints} HP", actualDelta, gameCharacterState.State.CurrentHitPoints);

            var healthChangedEvent = new HealthChangedEvent
            {
                SourceId = sourceCharacterId,
                TargetId = this.GetPrimaryKey(),
                Difference = actualDelta,
                ResultingHealthPercent = gameCharacterState.State.CurrentHealthPercent
            };
            await EventStream.OnNextAsync(healthChangedEvent);

            //TODO: debounce
            await gameCharacterState.WriteStateAsync();
        }

        private int ComputeActualHealthChange(int attemptedDelta)
        {
            var maxDamage = -1 * gameCharacterState.State.CurrentHitPoints;
            var maxHeal = gameCharacterState.State.TotalHitPoints - gameCharacterState.State.CurrentHitPoints;
            return Math.Max(maxDamage, Math.Min(maxHeal, attemptedDelta));
        }
    }
}