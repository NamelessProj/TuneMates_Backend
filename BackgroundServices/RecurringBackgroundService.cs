namespace TuneMates_Backend.BackgroundServices
{
    public abstract class RecurringBackgroundService : BackgroundService
    {
        protected readonly IServiceScopeFactory _scopeFactory;
        protected readonly ILogger _logger;
        private readonly TimeSpan _interval;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringBackgroundService"/> class.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory for creating scopes.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="interval">The interval between job executions.</param>
        protected RecurringBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger logger,
            TimeSpan interval)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _interval = interval;
        }

        /// <summary>
        /// Executes the recurring job at the specified interval until the service is stopped.
        /// </summary>
        /// <param name="stoppingToken">The cancellation token to stop the service.</param>
        /// <returns>A task that represents the background service execution.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{Service} started | Interval: {Interval}", GetType().Name, _interval);

            await ExecuteJobAsync(stoppingToken); // Initial run

            PeriodicTimer timer = new(_interval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await ExecuteJobAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("{Service} stopping at {CurrentTime}", GetType().Name, DateTime.UtcNow);
            }
        }

        /// <summary>
        /// When implemented in a derived class, contains the logic to be executed at each interval.
        /// </summary>
        /// <param name="stoppingToken">The cancellation token to stop the job.</param>
        /// <returns>A task that represents the job execution.</returns>
        protected abstract Task ExecuteJobAsync(CancellationToken stoppingToken);
    }
}