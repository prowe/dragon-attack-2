using Orleans;

namespace DragonAttack
{
    [GraphQL.GraphQLMetadata("Mutation")]
    public class MutationResolvers
    {
        private readonly IClusterClient clusterClient;
        private readonly ILogger<MutationResolvers> logger;

        public MutationResolvers(ILogger<MutationResolvers> logger, IClusterClient clusterClient)
        {
            this.logger = logger;
            this.clusterClient = clusterClient;
        }

        public async Task<int> AttackWithAbility(Guid targetId, string abilityId)
        {
            var playerId = Guid.Parse("1DA1118C-8004-4641-A031-13B624F795D5");
            return await clusterClient.GetGrain<IGameCharacterGrain>(playerId).AttackWithAbility(targetId, abilityId);
        }
    }
}