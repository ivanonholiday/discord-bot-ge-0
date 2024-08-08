using MainApp.DatabaseModels;
using MainApp.Jobs;
using Microsoft.EntityFrameworkCore;
using Quartz;

var builder = Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services.AddQuartz();
        services.AddQuartzHostedService(p => p.WaitForJobsToComplete = true);
        services.AddDbContext<DataContext>(p =>
        {
            p.UseSqlServer(Environment.GetEnvironmentVariable("DB_CONN"),
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        });
    })
    .Build();

var envName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
var isDevelopment = !string.IsNullOrWhiteSpace(envName) && envName == "Development";

var schedulerFactory = builder.Services.GetRequiredService<ISchedulerFactory>();
var scheduler = await schedulerFactory.GetScheduler();

const string cronExpression1 = "0 * * ? * * *"; // every minute
const string cronExpression2 = "0 0/10 * ? * * *"; // every 10 minute

var jobTypes = new Dictionary<Type, string>
{
    [typeof(FetchNewsJob)] = cronExpression1,
    [typeof(FetchCurrentIpJob)] = cronExpression2,
};

var i = 1;
foreach (var (type, cronExpression) in jobTypes)
{
    const string name = nameof(type);
    var jobName = $"job_{i}_{name}";
    var triggerName = $"trigger_{i}_{name}";
    var groupName = $"group_{i}_{name}";

    var job = JobBuilder.Create(type).WithIdentity(jobName, groupName).Build();

    var trigger = isDevelopment
        ? TriggerBuilder.Create().WithIdentity(triggerName, groupName).StartNow().Build()
        : TriggerBuilder.Create().WithIdentity(triggerName, groupName).WithCronSchedule(cronExpression).Build();

    await scheduler.ScheduleJob(job, trigger);
    i++;
}

await scheduler.Start();

// will block until the last running job completes
await builder.RunAsync();