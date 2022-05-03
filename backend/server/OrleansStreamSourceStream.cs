using System.Threading.Channels;
using HotChocolate.Execution;
using HotChocolate.Subscriptions.InMemory;
using Orleans.Streams;

namespace DragonAttack
{
    public class OrleansStreamSourceStream<T> : ISourceStream<T> where T : class
    {
        private readonly Channel<T> channel;
        private readonly IAsyncStream<T> orleansStream;
        private readonly InMemorySourceStream<T> channelWrapper;
        private StreamSubscriptionHandle<T> subscriptionHandle;

        public OrleansStreamSourceStream(IAsyncStream<T> stream)
        {
            this.orleansStream = stream;
            this.channel = Channel.CreateBounded<T>(100);
            this.channelWrapper = new InMemorySourceStream<T>(channel);
        }

        public async ValueTask DisposeAsync()
        {
            if (subscriptionHandle != null)
            {
                await subscriptionHandle.UnsubscribeAsync();
            }
            await channelWrapper.DisposeAsync();
        }

        public async IAsyncEnumerable<T> ReadEventsAsync()
        {
            subscriptionHandle = await orleansStream.SubscribeAsync<T>(HandleNewEvent);
            await foreach (var ev in channelWrapper.ReadEventsAsync())
            {
                yield return ev;
            }
        }

        private async Task HandleNewEvent(T eve, StreamSequenceToken token)
        {
            await channel.Writer.WriteAsync(eve);
        }

        IAsyncEnumerable<object> ISourceStream.ReadEventsAsync() => ReadEventsAsync();
    }
}