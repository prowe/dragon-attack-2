using System.Collections;
using Orleans;
using Orleans.Streaming;
using System.Linq;
using Orleans.Streams;


/*
Watch area
everyone in hate list
number of targets instead of type
*/
namespace DragonAttack
{
    public interface INPCControllerGrain : IGrainWithGuidKey
    {
        public Task TakeControl(Guid gameCharacterId);
    }

    public class NPCControllerGrain : Orleans.Grain, INPCControllerGrain
    {
        private readonly IClusterClient clusterClient;
        private readonly ILogger<NPCControllerGrain> logger;
        private readonly IDictionary<Guid, Ability> abilityMap;
        private IGameCharacterGrain gameCharacter;
        private Dictionary<Guid, HateListEntry> hateList = new Dictionary<Guid, HateListEntry>();
        private Dictionary<Guid, DateTime> abilitiesOnCooldown = new Dictionary<Guid, DateTime>();
        private StreamSubscriptionHandle<IGameCharacterEvent> gameCharacterStreamHandle;
        private StreamSubscriptionHandle<IAreaEvent> areaStreamHandle;

        public NPCControllerGrain(
            IClusterClient clusterClient, 
            ILogger<NPCControllerGrain> logger,
            IDictionary<Guid, Ability> abilityMap)
        {
            this.clusterClient = clusterClient;
            this.logger = logger;
            this.abilityMap = abilityMap;
        }

        public async Task TakeControl(Guid gameCharacterId)
        {
            this.gameCharacter = clusterClient.GetGrain<IGameCharacterGrain>(gameCharacterId);
            gameCharacterStreamHandle = await GetGameCharacterStream(gameCharacterId).SubscribeAsync(HandleCharacterEvent);
            await SetupHateList();
            RegisterTimer(TakeTurn, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
        }

        private async Task SetupHateList()
        {
            var currentState = await gameCharacter.GetState();
            var areaId = currentState.LocationAreaId;
            areaStreamHandle = await clusterClient.GetStreamProvider("default")
                .GetStream<IAreaEvent>(areaId, nameof(IAreaGrain))
                .SubscribeAsync((areaEvent, token) => areaEvent switch
                {
                    CharacterEnteredAreaEvent enteredEvent => RegisterHateForCharacter(enteredEvent.GameCharacterId, 0),
                    _ => Task.CompletedTask,
                });
            var areaCharacterIds = await clusterClient.GetGrain<IAreaGrain>(areaId).GetPresentCharacterIds();
            await Task.WhenAll(areaCharacterIds
                .Where(id => id != gameCharacter.GetPrimaryKey())
                .Select(id => RegisterHateForCharacter(id, 0))
            );
        }

        private Task HandleCharacterEvent(IGameCharacterEvent characterEvent, StreamSequenceToken token)
        {
            return characterEvent switch {
                HealthChangedEvent healthChangedEvent => OnHealthChange(healthChangedEvent),
                _ => Task.CompletedTask,
            };
        }

        private async Task OnHealthChange(HealthChangedEvent healthChangedEvent)
        {
            if (healthChangedEvent.Difference < 0)
            {
                await RegisterHateForCharacter(healthChangedEvent.Source, healthChangedEvent.Difference * -1);
            }
        }

        private async Task RegisterHateForCharacter(Guid gameCharacterId, int hate)
        {
            if (hateList.TryGetValue(gameCharacterId, out var entry))
            {
                entry.hate += hate;
            }
            else
            {
                var handle = await GetGameCharacterStream(gameCharacterId).SubscribeAsync(HandleHatedCharacterEvent);
                hateList[gameCharacterId] = new HateListEntry(hate, handle);
            }
        }

        private async Task HandleHatedCharacterEvent(IGameCharacterEvent characterEvent, StreamSequenceToken token)
        {
            if (characterEvent is HealthChangedEvent healthChanged)
            {
                var targetId = healthChanged.Target;
                if(healthChanged.ResultingHealthPercent == 0
                    && hateList.TryGetValue(targetId, out var entry))
                {
                    await entry.subscriptionHandle.UnsubscribeAsync();
                    hateList.Remove(targetId);
                    logger.LogInformation("Forgot dead character from hatelist {id}",targetId);
                }
            }
        }

        private async Task TakeTurn(object _)
        {
            logger.LogInformation("Taking a turn {id}", this.GetPrimaryKey());
            CleanupCooldowns();
            var currentState = await gameCharacter.GetState();
            var ability = ChooseAbility(currentState);
            var targets = ChooseTargets(ability);
            if(targets.Any())
            {
                logger.LogInformation("Using ability {abilityid} ({abilityName}) on {targetIds}", ability.Id, ability.Name, targets);
                await gameCharacter.UseAbility(ability.Id, targets);
                RegisterCooldown(ability);
            }
        }

        private void RegisterCooldown(Ability ability)
        {
            if (ability.Cooldown > TimeSpan.Zero)
            {
                abilitiesOnCooldown[ability.Id] = DateTime.Now + ability.Cooldown;
            }
        }

        private void CleanupCooldowns()
        {
            var now = DateTime.Now;
            abilitiesOnCooldown
                .Where(kv => kv.Value < now)
                .ToList()
                .ForEach(kv => abilitiesOnCooldown.Remove(kv.Key));
        }

        private Ability ChooseAbility(GameCharacter currentState)
        {
            var bestNotOnCooldown = currentState.Abilities(abilityMap)
                .Where(a => !abilitiesOnCooldown.ContainsKey(a.Id))
                .OrderByDescending(a => a.Dice.Average)
                .FirstOrDefault();
            return bestNotOnCooldown;
        }

        private Guid[] ChooseTargets(Ability ability)
        {
            return hateList
                .OrderByDescending(kv => kv.Value.hate)
                .Take(ability.MaxTargets)
                .Select(kv => kv.Key)
                .ToArray();
        }

        private async Task<IEnumerable<Guid>> GetOtherCharactersInArea(Guid areaId)
        {
            var area = await clusterClient.GetGrain<IAreaGrain>(areaId).GetState();
            return area.CharactersPresentIds
                .Where(id => id != gameCharacter.GetPrimaryKey());
        }

        private IAsyncStream<IGameCharacterEvent> GetGameCharacterStream(Guid id)
        {
            return clusterClient.GetStreamProvider("default").GetStream<IGameCharacterEvent>(id, nameof(IGameCharacterEvent));
        }

        private class HateListEntry
        {
            public int hate = 0;
            internal readonly StreamSubscriptionHandle<IGameCharacterEvent> subscriptionHandle;
            public HateListEntry(int initalHate, StreamSubscriptionHandle<IGameCharacterEvent> handle)
            {
                this.subscriptionHandle = handle;
                this.hate = initalHate;
            }
        }
    }
}