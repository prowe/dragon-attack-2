using Orleans.Streams;

namespace DragonAttack
{
    public class StreamWrapper<T> : IObservable<T> where T : class
    {
        private readonly IAsyncStream<T> stream;

        public StreamWrapper(IAsyncStream<T> stream)
        {
            this.stream = stream;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var task = SubscribeAsync(observer);
            task.Wait();
            return task.Result;
        }

        private async Task<IDisposable> SubscribeAsync(IObserver<T> observer)
        {
            Func<T, StreamSequenceToken, Task> onNext = (value, token) =>
            {
                observer.OnNext(value);
                return Task.CompletedTask;
            };
            var streamSubscriptionHandle = await stream.SubscribeAsync(onNext);
            return new UnSubscriber<T>(streamSubscriptionHandle);
        }

        private class UnSubscriber<T> : IDisposable
        {
            private readonly StreamSubscriptionHandle<T> handle;

            public UnSubscriber(StreamSubscriptionHandle<T> handle)
            {
                this.handle = handle;
            }

            public void Dispose()
            {
                handle.UnsubscribeAsync();
            }
        }

    }
}