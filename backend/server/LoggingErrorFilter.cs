namespace DragonAttack
{
    public class LoggingErrorFilter : IErrorFilter
    {
        private readonly ILogger<LoggingErrorFilter> _logger;

        public LoggingErrorFilter(ILogger<LoggingErrorFilter> logger)
        {
            _logger = logger;
        }

        public IError OnError(IError error)
        {
            _logger.LogError(error.Exception, "Error executing {message}", error.Message);
            return error;
        }
    }
}