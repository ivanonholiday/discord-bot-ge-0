using Quartz;
using RestSharp;

namespace MainApp.Jobs;

public class FetchCurrentIpJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await FetchIp();
    }

    private static async Task FetchIp()
    {
        try
        {
            var client = new RestClient("https://ifconfig.me/ip");
            var result = await client.GetAsync(new RestRequest());
            var content = result.Content?.Trim();
            Console.WriteLine($" * current IP: {content}");
        }
        catch (Exception e)
        {
            Console.WriteLine($" * Fetch IP error, {e}");
        }
    }
}