using Cronos;
using DNS_BLM.Domain.Configuration;
using Microsoft.Extensions.Options;

namespace DNS_BLM.Api.TimedTasks;

public abstract class TimedHostedService : IDisposable, IHostedService
{
    private protected readonly ILogger _logger;
    private Timer? _timer;
    private CronExpression? _expression;
    private readonly IOptions<AppConfiguration> _appConfiguration;

    protected TimedHostedService(ILogger logger, IOptions<AppConfiguration> appConfiguration)
    {
        _logger = logger;
        _appConfiguration = appConfiguration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Timed Hosted Service ({TaskName}) running.");
        
        var timedTasks = _appConfiguration.Value.TimedTasks;
        string cronExpression = string.Empty;
        var propertyInfo = timedTasks.GetType().GetProperty(TaskName);
        if (propertyInfo != null)
        {
            var taskSchedule = propertyInfo.GetValue(timedTasks);
            cronExpression = taskSchedule?.ToString() ?? string.Empty;
        }
        else
        {
            _logger.LogError($"Property '{TaskName}' not found in TimedTasks.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);

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

        var nextUtc = _expression.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Local);
        if (nextUtc.HasValue)
        {
            var nextLocal = TimeZoneInfo.ConvertTimeFromUtc(nextUtc.Value, TimeZoneInfo.Local);
            var delay = nextLocal - DateTime.Now;
            _timer?.Dispose();
            _timer = new Timer(ExecuteTimedTaskWrapper, null, delay, Timeout.InfiniteTimeSpan);
            _logger.LogInformation("Next scheduled execution for {TaskName} at {NextExecution} (in {Delay})",
                TaskName, nextLocal, delay);
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