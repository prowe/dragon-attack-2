using System.Text.Json;
using GraphQL.Server.Transports.Subscriptions.Abstractions;

namespace DragonAttack
{
    public class PlayerContextListener : IOperationMessageListener
    {
        private readonly ILogger<PlayerContextListener> _logger;

        public PlayerContextListener(ILogger<PlayerContextListener> logger)
        {
            _logger = logger;
        }

        public Task AfterHandleAsync(MessageHandlingContext context) => Task.CompletedTask;

        public Task BeforeHandleAsync(MessageHandlingContext context) => Task.CompletedTask;

        public Task HandleAsync(MessageHandlingContext context)
        {
            if (context.Message.Type == MessageType.GQL_CONNECTION_INIT)
            {
                var jsonString = context.Message.Payload as string;
                _logger.LogInformation("Got init message: {payload}", jsonString);
                if (jsonString != null) {
                    var playerContext = JsonSerializer.Deserialize<PlayerContext>(jsonString);
                    // context["playerContext"] = playerContext;
                    context.Properties["player"] = playerContext;
                }
            }
            return Task.CompletedTask;
        }
    }
}