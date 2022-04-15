using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Streams;

namespace DragonAttack
{
    public interface ICounter : Orleans.IGrainWithIntegerKey
    {
        Task<int> Increment();
    }

    public class CounterGrain : Orleans.Grain, ICounter
    {
        private readonly ILogger<CounterGrain> logger;
        private readonly IAsyncStream<GameCharacter> gameCharacterStream;
        private int counter = 0;

        public CounterGrain(ILogger<CounterGrain> logger, IClusterClient clusterClient)
        {
            this.logger = logger;
            var streamProvider = clusterClient.GetStreamProvider("default");
            gameCharacterStream = streamProvider.GetStream<GameCharacter>(Guid.Empty, "GameCharacter");
        }

        public async Task<int> Increment()
        {
            counter++;
            // OnCounterChanged?.Invoke(this, counter);
            logger.LogInformation("Incrementing counter: {value}", counter);
            await gameCharacterStream.OnNextAsync(new GameCharacter
            {
                Id = this.GetPrimaryKeyLong().ToString(),
                Name = "nothing",
                HealthPercent = counter
            });
            return counter;
        }
    }
}