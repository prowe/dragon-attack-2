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
        private IGameCharacterGrain gameCharacter;
        private Dictionary<Guid, HateListEntry> hateList = new Dictionary<Guid, HateListEntry>();

        public NPCControllerGrain(IClusterClient clusterClient, ILogger<NPCControllerGrain> logger)
        {
            this.clusterClient = clusterClient;
            this.logger = logger;
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
            var ability = await ChooseAbility();
            var targets = ChooseTargets(ability);
            if(targets.Any())
            {
                await gameCharacter.UseAbility(ability.Id, targets.ToArray());
            }
        }

        private async Task<Ability> ChooseAbility()
        {
            var state = await gameCharacter.GetState();
            return state.Abilities.First();
        }

        private IEnumerable<Guid> ChooseTargets(Ability ability)
        {
            if (!hateList.Any())
            {
                return Array.Empty<Guid>();
            }
            return hateList
                .OrderByDescending(kv => kv.Value.damage)
                .Take(1)
                .Select(kv => kv.Key);
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