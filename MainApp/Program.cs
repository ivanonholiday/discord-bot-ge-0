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

var schedulerFactory = builder.Services.GetRequiredService<ISchedulerFactory>();
var scheduler = await schedulerFactory.GetScheduler();

const string cronExpression = "0 * * ? * * *"; // every minute

var jobTypes = new List<Type>
{
    typeof(FetchNewsJob),
};

var i = 1;
foreach (var type in jobTypes)
{
    const string name = nameof(type);
    var jobName = $"job_{i}_{name}";
    var triggerName = $"trigger_{i}_{name}";
    var groupName = $"group_{i}_{name}";

    var job = JobBuilder.Create(type).WithIdentity(jobName, groupName).Build();
    var trigger = TriggerBuilder.Create()
        .WithIdentity(triggerName, groupName)
        .WithCronSchedule(cronExpression)
        .Build();

    await scheduler.ScheduleJob(job, trigger);
    i++;
}

await scheduler.Start();

// will block until the last running job completes
await builder.RunAsync();