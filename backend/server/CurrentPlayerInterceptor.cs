using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Execution;

namespace DragonAttack
{
    // This isn't working
    public class CurrentPlayerInterceptor : ISocketSessionInterceptor
    {
        private readonly ILogger<CurrentPlayerInterceptor> logger;

        public CurrentPlayerInterceptor(ILogger<CurrentPlayerInterceptor> logger)
        {
            this.logger = logger;
        }

        public ValueTask<ConnectionStatus> OnConnectAsync(ISocketConnection connection, InitializeConnectionMessage message, CancellationToken cancellationToken)
        {
            if (message.Payload?.TryGetValue("playerId", out var value) ?? false)
            {
                if(value is string playerId)
                {
                    logger.LogInformation("Handling current playerId {playerId}", playerId);
                    connection.HttpContext.Items["playerId"] = Guid.Parse(playerId);
                }
            }
            return ValueTask.FromResult(ConnectionStatus.Accept());
        }

        public ValueTask OnRequestAsync(ISocketConnection connection, IQueryRequestBuilder requestBuilder, CancellationToken cancellationToken)
        {
            if (connection.HttpContext.Items.TryGetValue("playerId", out var value))
            {
                requestBuilder.AddProperty("playerId", value);
            }
            return ValueTask.CompletedTask;
        }

        public ValueTask OnCloseAsync(ISocketConnection connection, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }
}