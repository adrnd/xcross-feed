using xcross_backend.Controllers;



public class TimingService : IHostedService, IDisposable
{
    //reference: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-9.0&tabs=visual-studio

    private int executionCount = 0;
    private readonly ILogger<TimingService> _logger;
    private Timer? _timer = null;

    public event Func<Task> OnTickAsync = delegate { return Task.CompletedTask; };

    public TimingService(ILogger<TimingService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service running.");

        _timer = new Timer(DoWorkAsync, null, TimeSpan.Zero,
            TimeSpan.FromSeconds(30));
        return Task.CompletedTask;
        

    }

    private async void DoWorkAsync(object? state)
    {
        //var count = Interlocked.Increment(ref executionCount);
        _logger.LogInformation(
            "Ping");
        await OnTickAsync();

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
    }
}