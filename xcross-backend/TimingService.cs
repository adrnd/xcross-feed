using xcross_backend.Controllers;



public class TimingService : IHostedService, IDisposable
{
    //reference: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-9.0&tabs=visual-studio

    private readonly ILogger<TimingService> _logger;
    private Timer? _timer = null;

    /// <summary>
    /// Simple tick event that other services can subscribe to.
    /// </summary>
    public event Func<Task> OnTickAsync = delegate { return Task.CompletedTask; };


    /// <summary>
    /// simple reflection whether the timer is currently disposed (inactive) or not to avoid overlapping timers are exceptions when a second one is started
    /// needs to be replaced with a more reliable option
    /// </summary>
    public bool _disposed = true; 

    public TimingService(ILogger<TimingService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _disposed = false;
        _logger.LogInformation("Timed Hosted Service running.");

        _timer = new Timer(DoWorkAsync, null, TimeSpan.Zero,
            TimeSpan.FromSeconds(30));
        return Task.CompletedTask;

    }
    
    private async void DoWorkAsync(object? state)
    {
        _disposed = false;
        //var count = Interlocked.Increment(ref executionCount);
        _logger.LogInformation("Ping");
        await OnTickAsync(); //only useful thing it does right now

    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service is stopping.");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _disposed = true;
    }
}