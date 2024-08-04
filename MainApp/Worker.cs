using Cronos;

namespace MainApp;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly CronExpression _cron;

    public Worker(ILogger<Worker> logger)
    {
        _cron = CronExpression.Parse("0/5 * * * * *", CronFormat.IncludeSeconds);
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                var utcNow = DateTime.UtcNow;
                var nextUtc = _cron.GetNextOccurrence(utcNow)!;
                await Task.Delay(nextUtc.Value - utcNow, stoppingToken);
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}