using Cronos;

namespace DNS_BLM.Api.TimedTasks;

public abstract class TimedHostedService : IDisposable, IHostedService
{
    private protected readonly ILogger _logger;
    private Timer? _timer;
    private CronExpression? _expression;
    private protected readonly IServiceProvider _serviceProvider;

    protected TimedHostedService(ILogger logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Timed Hosted Service ({TaskName}) running.");

        using var scope = _serviceProvider.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var cronExpression = configuration.GetValue<string>($"DNS-BLM:TimedTasks:{TaskName}");
        
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            throw new Exception($"Cron expression for task {TaskName} not found in configuration. Please set DNS-BLM:TimedTasks:{TaskName} environment variable or configuration.");
        }

        _logger.LogInformation("Using cron schedule: {CronExpression} for task {TaskName}", cronExpression, TaskName);
        _expression = CronExpression.Parse(cronExpression);
        ScheduleNext();
        
        return Task.CompletedTask;
    }

    private void ScheduleNext()
    {
        if (_expression == null) return;

        var next = _expression.GetNextOccurrence(DateTime.Now);
        if (next.HasValue)
        {
            var delay = next.Value - DateTime.Now;
            _timer?.Dispose();
            _timer = new Timer(ExecuteTimedTaskWrapper, null, delay, Timeout.InfiniteTimeSpan);
            _logger.LogInformation("Next scheduled execution for {TaskName} at {NextExecution} (in {Delay})", 
                TaskName, next.Value, delay);
        }
    }

    /// <summary>
    /// Wrapper method for executing a timed task. Logs the start and end of the task execution.
    /// </summary>
    /// <param name="state">An optional parameter that can represent state information used in the task execution.</param>
    async void ExecuteTimedTaskWrapper(object? state = null)
    {
        try
        {
            _logger.LogInformation("Executing Timed Task: " + TaskName);
            await ExecuteTimedTask(state);
            _logger.LogInformation("Finished Timed Task: " + TaskName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while executing timed task {TaskName}", TaskName);
        }
        finally
        {
            ScheduleNext();
        }
    }

    protected abstract Task ExecuteTimedTask(object? state = null);
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
        GC.SuppressFinalize(this);
    }
}