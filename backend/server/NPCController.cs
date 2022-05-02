using System.Collections;
using Orleans;
using Orleans.Streaming;
using System.Linq;
using Orleans.Streams;

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
            await GetGameCharacterStream(gameCharacterId).SubscribeAsync(HandleCharacterEvent);
            RegisterTimer(TakeTurn, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
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
            if (healthChangedEvent.Difference >= 0)
            {
                return;
            }
            if (hateList.TryGetValue(healthChangedEvent.Source, out var entry))
            {
                entry.damage -= healthChangedEvent.Difference;
            }
            else
            {
                var handle = await GetGameCharacterStream(healthChangedEvent.Source).SubscribeAsync(HandleHatedCharacterEvent);
                hateList[healthChangedEvent.Source] = new HateListEntry(-1 * healthChangedEvent.Difference, handle);
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
            var targets = await ChooseTargets(ability, currentState);
            if(targets.Any())
            {
                logger.LogInformation("Using ability {abilityid} ({abilityName}) on {targetIds}", ability.Id, ability.Name, targets);
                await gameCharacter.UseAbility(ability.Id, targets.ToArray());
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

        private async Task<IEnumerable<Guid>> ChooseTargets(Ability ability, GameCharacter currentState)
        {
            return ability.TargetType switch
            {
                TargetType.Single => hateList
                    .OrderByDescending(kv => kv.Value.damage)
                    .Take(1)
                    .Select(kv => kv.Key),
                TargetType.Area => await GetOtherCharactersInArea(currentState.LocationAreaId),
                _ => Enumerable.Empty<Guid>(),
            };
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
            public int damage = 0;
            internal readonly StreamSubscriptionHandle<IGameCharacterEvent> subscriptionHandle;
            public HateListEntry(int initialDamage, StreamSubscriptionHandle<IGameCharacterEvent> handle)
            {
                this.subscriptionHandle = handle;
                this.damage = initialDamage;
            }
        }
    }
}