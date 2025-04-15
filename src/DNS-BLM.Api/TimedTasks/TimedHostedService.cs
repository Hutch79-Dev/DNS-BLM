namespace DNS_BLM.Api.TimedTasks;

public abstract class TimedHostedService : IDisposable, IHostedService
{
    private protected readonly ILogger _logger;
    private Timer _timer;
    private protected readonly IServiceProvider _serviceProvider;

    protected TimedHostedService(ILogger logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Timed Hosted Service ({TaskName}) running.");

        var now = DateTime.UtcNow;
        DateTime nextExecutionTime = DateTime.UtcNow.Date.Add(GetExecutionTime());
        if (now > nextExecutionTime)
        {
            nextExecutionTime = nextExecutionTime.AddDays(1);
        }

        var dueTime = nextExecutionTime - now;
        _timer = new Timer(ExecuteTimedTaskWrapper, null, dueTime, GetInterval());
        return Task.CompletedTask;
    }


    /// <summary>
    /// Wrapper method for executing a timed task. Logs the start and end of the task execution.
    /// </summary>
    /// <param name="state">An optional parameter that can represent state information used in the task execution.</param>
    async void ExecuteTimedTaskWrapper(object? state = null)
    {
        _logger.LogInformation("Executing Timed Task: " + TaskName);
        await ExecuteTimedTask(state);
        _logger.LogInformation("Finished Timed Task: " + TaskName);
    }

    protected abstract Task ExecuteTimedTask(object? state = null);

    /// <summary>
    /// Time when to execute a scheduled Task in UTC.
    /// e.g. TimeSpan(2, 0, 0) meaning 2AM.
    /// </summary>
    /// <returns></returns>
    protected abstract TimeSpan GetExecutionTime();

    /// <summary>
    /// Interval for executing scheduled Task.
    /// e.g. TimeSpan.FromDays(1) meaning once a day.
    /// </summary>
    /// <returns></returns>
    protected abstract TimeSpan GetInterval();

    protected abstract string TaskName { get; }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        _logger.LogInformation("Hosted Service is stopping.");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}