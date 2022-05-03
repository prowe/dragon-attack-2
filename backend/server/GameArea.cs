using Orleans;
using Orleans.Streams;
using Orleans.Core;
using Orleans.Runtime;

namespace DragonAttack
{
    public class Area {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public HashSet<Guid> CharactersPresentIds { get; } = new HashSet<Guid>();

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

    [UnionType("AreaEvent")]
    public interface IAreaEvent
    {
    }

    public class CharacterEnteredAreaEvent : IAreaEvent
    {
        public Guid GameCharacterId { get; set; }
        public Task<GameCharacter> GameCharacter([Service] IClusterClient clusterClient)
        {
            return clusterClient.GetGrain<IGameCharacterGrain>(GameCharacterId).GetState();
        }

        public Guid AreaId { get; set; }
        public Task<Area> Area([Service] IClusterClient clusterClient)
        {
            return clusterClient.GetGrain<IAreaGrain>(AreaId).GetState();
        }

        public override string ToString()
        {
            return $"CharacterEnteredAreaEvent {GameCharacterId} {AreaId}";
        }
    }


    [ImplicitStreamSubscription(nameof(IAreaGrain))]
    public class AreaGrain : Orleans.Grain, IAreaGrain, IAsyncObserver<IAreaEvent>
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
            var subscription = await stream.SubscribeAsync(this);
            await base.OnActivateAsync();
        }

        public Task<Area> GetState() => Task.FromResult(areaState.State ?? throw new NullReferenceException());

        public Task<ISet<Guid>> GetPresentCharacterIds()
        {
            var present = areaState.State?.CharactersPresentIds ?? Enumerable.Empty<Guid>();
            ISet<Guid> presentSet = present.ToHashSet();
            return Task.FromResult(presentSet);
        }

        public Task OnCompletedAsync() => Task.CompletedTask;

        public Task OnErrorAsync(Exception ex) => Task.CompletedTask;
        
        public async Task OnNextAsync(IAreaEvent item, StreamSequenceToken token = null)
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
