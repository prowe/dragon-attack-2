using Orleans;
using Orleans.Streams;
using Orleans.Core;

namespace DragonAttack
{
    public class Area {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public interface IAreaGrain : IGrainWithGuidKey
    {
        public static readonly Guid StartingArea = Guid.Parse("3A3F5245-BE2F-4DC8-AEE1-1EDB84025F17");
    }

    public interface IAreaEvent
    {
    }

    public class CharacterEnteredAreaEvent : IAreaEvent
    {
        public Guid GameCharacterId { get; set; }
        public Guid AreaId { get; set; }

        public override string ToString()
        {
            return $"CharacterEnteredAreaEvent {GameCharacterId} {AreaId}";
        }
    }


    [ImplicitStreamSubscription(nameof(IAreaGrain))]
    public class AreaGrain : Orleans.Grain, IAreaGrain, IAsyncObserver<IAreaEvent>
    {
        private readonly ILogger<AreaGrain> logger;
        private readonly IClusterClient clusterClient;
        private readonly HashSet<Guid> charactersPresent = new HashSet<Guid>();

        public AreaGrain(IClusterClient clusterClient, ILogger<AreaGrain> logger)
        {
            this.clusterClient = clusterClient;
            this.logger = logger;
        }

        public override async Task OnActivateAsync()
        {
            var streamProvider = base.GetStreamProvider("default");
            var stream = streamProvider.GetStream<IAreaEvent>(this.GetPrimaryKey(), nameof(IAreaGrain));
            var subscription = await stream.SubscribeAsync(this);
            await base.OnActivateAsync();
        }

        public Task OnCompletedAsync() => Task.CompletedTask;
        public Task OnErrorAsync(Exception ex) => Task.CompletedTask;
        
        public Task OnNextAsync(IAreaEvent item, StreamSequenceToken? token = null)
        {
            logger.LogInformation("Got event: {event}", item);
            switch (item)
            {
                case CharacterEnteredAreaEvent enteredAreaEvent:
                    charactersPresent.Add(enteredAreaEvent.GameCharacterId);
                    break;
                default:
                    break;
            }
            return Task.CompletedTask;
        }
    }
}
