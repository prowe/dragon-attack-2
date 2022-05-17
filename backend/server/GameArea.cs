using Orleans;
using Orleans.Streams;
using Orleans.Runtime;

namespace DragonAttack
{
    public class Area {
        public Guid Id { get; set; }
        public string Name { get; set; } = "Unknown";
        internal HashSet<Guid> CharactersPresentIds { get; } = new HashSet<Guid>();

        public Task<GameCharacter[]> CharactersPresent([Service] IClusterClient clusterClient)
        {
            return Task.WhenAll(CharactersPresentIds.Select(id => clusterClient.GetGrain<IGameCharacterGrain>(id).GetState()));
        }
    }

    public interface IAreaGrain : IGrainWithGuidKey
    {
        public static readonly Guid StartingArea = Guid.Parse("3A3F5245-BE2F-4DC8-AEE1-1EDB84025F17");

        public Task<Area> GetState();

        public Task<ISet<Guid>> GetPresentCharacterIds();
    }

    public interface IAreaEvent
    {
        internal Guid AreaId { get; set; }
        public string Name { get; }
    }

    public class CharacterEnteredAreaEvent : IAreaEvent
    {
        public string Name { get; } = "Character Entered Area";
        public Guid AreaId { get; set; }
        internal Guid GameCharacterId { get; set; }

        public Task<GameCharacter> GameCharacter([Service] IClusterClient clusterClient)
        {
            return clusterClient.GetGrain<IGameCharacterGrain>(GameCharacterId).GetState();
        }

        public Task<Area> Area([Service] IClusterClient clusterClient)
        {
            return clusterClient.GetGrain<IAreaGrain>(AreaId).GetState();
        }

        public override string ToString()
        {
            return $"CharacterEnteredAreaEvent {GameCharacterId} {AreaId}";
        }
    }

    public class CharacterExitedAreaEvent : IAreaEvent
    {
        public string Name { get; } = "Character Exited Area";
        public Guid AreaId { get; set; }
        internal Guid GameCharacterId { get; set; }

        public Task<GameCharacter> GameCharacter([Service] IClusterClient clusterClient)
        {
            return clusterClient.GetGrain<IGameCharacterGrain>(GameCharacterId).GetState();
        }

        public Task<Area> Area([Service] IClusterClient clusterClient)
        {
            return clusterClient.GetGrain<IAreaGrain>(AreaId).GetState();
        }
    }


    [ImplicitStreamSubscription(nameof(IAreaGrain))]
    public class AreaGrain : Orleans.Grain, IAreaGrain
    {
        private readonly ILogger<AreaGrain> logger;
        private readonly IPersistentState<Area> areaState;
        private readonly IClusterClient clusterClient;

        public AreaGrain(
            IClusterClient clusterClient, 
            ILogger<AreaGrain> logger,
            [PersistentState("Area")] IPersistentState<Area> areaState)
        {
            this.clusterClient = clusterClient;
            this.logger = logger;
            this.areaState = areaState;
        }

        public override async Task OnActivateAsync()
        {
            if(!areaState.RecordExists)
            {
                areaState.State= new Area
                {
                    Id = this.GetPrimaryKey(),
                    Name = "Starting Area",
                };
                await areaState.WriteStateAsync();
            }

            var streamProvider = base.GetStreamProvider("default");
            var stream = streamProvider.GetStream<IAreaEvent>(this.GetPrimaryKey(), nameof(IAreaGrain));
            var subscription = await stream.SubscribeAsync(OnAreaEvent);
            await base.OnActivateAsync();
        }

        public Task<Area> GetState() => Task.FromResult(areaState.State ?? throw new NullReferenceException());

        public Task<ISet<Guid>> GetPresentCharacterIds()
        {
            var present = areaState.State?.CharactersPresentIds ?? Enumerable.Empty<Guid>();
            ISet<Guid> presentSet = present.ToHashSet();
            return Task.FromResult(presentSet);
        }
        
        public async Task OnAreaEvent(IAreaEvent item, StreamSequenceToken? token = null)
        {
            logger.LogInformation("Got event: {event}", item);
            switch (item)
            {
                case CharacterEnteredAreaEvent enteredAreaEvent:
                    areaState.State.CharactersPresentIds.Add(enteredAreaEvent.GameCharacterId);
                    await areaState.WriteStateAsync();
                    break;
                default:
                    break;
            }
        }
    }
}
