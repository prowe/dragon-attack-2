using System.Collections;
using Orleans;
using System.Linq;

namespace DragonAttack
{
    public interface INPCController
    {
        Task TakeTurn(IGameCharacterGrain characterGrain);

        void OnHealthChange(HealthChangedEvent attackedEvent);
    }

    public class DragonController : INPCController
    {
        private readonly ILogger<DragonController> logger;
        private readonly IClusterClient clusterClient;
        private readonly HateList hateList = new HateList();
        private int flameBreathCooldownRemaining = 5;

        public DragonController(ILogger<DragonController> logger, IClusterClient clusterClient)
        {
            this.logger = logger;
            this.clusterClient = clusterClient;
        }

        public void OnHealthChange(HealthChangedEvent attackedEvent)
        {
            if (attackedEvent.Difference < 0)
            {
                var damage = attackedEvent.Difference * -1;
                logger.LogInformation("Logging damage taken {damage}", damage);
                hateList.RegisterDamage(attackedEvent.Source, damage);
            }
        }

        public async Task TakeTurn(IGameCharacterGrain characterGrain)
        {
            logger.LogInformation("Taking a turn {id}", characterGrain.GetPrimaryKey());
            if (flameBreathCooldownRemaining == 0)
            {
                await ExecuteFlameBreath(characterGrain);
                flameBreathCooldownRemaining = 5;
                return;
            }

            if (!hateList.Empty)
            {
                var mostHatedId = hateList.First().Key;
                await characterGrain.UseAbility("claw", mostHatedId);
                flameBreathCooldownRemaining--;
            }
        }

        private async Task ExecuteFlameBreath(IGameCharacterGrain characterGrain)
        {
            var state = await characterGrain.GetState();
            var myId = characterGrain.GetPrimaryKey();
            var area = clusterClient.GetGrain<IAreaGrain>(state.LocationAreaId);
            logger.LogInformation("Executing Flame Breath, {areaId}", state.LocationAreaId);
            var areaState = await area.GetState();
            var targetIds = areaState.CharactersPresentIds
                .Where(id => id != myId)
                .ToArray();
            logger.LogInformation("Flame Breath targets: {myId} -> {targetIds}", myId, targetIds);
            if (targetIds.Any())
            {
                await characterGrain.UseAbility("flame-breath", targetIds);
            }
        }
    }

    public class HateList : IEnumerable<KeyValuePair<Guid, int>>
    {
        private Dictionary<Guid, int> values = new Dictionary<Guid, int>();

        public void RegisterDamage(Guid attacker, int damage)
        {
            var currentDamage = values.TryGetValue(attacker, out int perviousDamage) ? perviousDamage : 0;
            values[attacker] = currentDamage + damage;
        }

        public bool Empty => !values.Any();

        public IEnumerator<KeyValuePair<Guid, int>> GetEnumerator()
        {
            return values.AsEnumerable()
                .OrderByDescending(kp => kp.Value)
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}