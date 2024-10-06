using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using MainApp.DatabaseModels;
using Quartz;
using RestSharp;

namespace MainApp.Jobs;

public class FetchNewsJob(DataContext db) : IJob
{
    private const string BaseUrl = "https://ge.mangot5.com";

    public async Task Execute(IJobExecutionContext context)
    {
        Console.WriteLine($"  > invoked at {DateTime.UtcNow.AddHours(8):yyyy-MM;dd HH:mm:ss}");
        await Scraping(db, BaseUrl);
        Console.WriteLine($"  > completed at {DateTime.UtcNow.AddHours(8):yyyy-MM;dd HH:mm:ss}");
        Console.WriteLine("---");
    }

    private static async Task Scraping(DataContext db, string webpageUrl)
    {
        var web = new HtmlWeb();
        var document = await web.LoadFromWebAsync($"{webpageUrl}/ge/index");

        const string selector = ".newsinfo-section .tab-content li";
        var items = document.QuerySelectorAll(selector);

        var newsList = new List<News>();

        foreach (var node in items.Where(p => p is not null).Select(p => p!))
        {
            var news = new News();

            var typeNode = node.QuerySelector("a.badge");
            var type = typeNode?.GetClasses().FirstOrDefault(p => p.StartsWith("badge-"));
            if (type != null)
            {
                news.Type = type["badge-".Length..].Trim();
                news.TypeText = typeNode?.InnerText.Trim();
            }

            var link = node.QuerySelector(".tit a");
            if (link != null)
            {
                var url = link.GetAttributeValue("href", "");
                news.Url = (url.StartsWith("https://") ? "" : BaseUrl) + url.Replace("/?", "?");
                news.Title = link.InnerText.Trim();

                if (!string.IsNullOrWhiteSpace(url))
                {
                    var uri = new Uri(news.Url);
                    var qs = HttpUtility.ParseQueryString(uri.Query);
                    var contentNo = qs.Get("contentNo");

                    if (int.TryParse(contentNo, out var id))
                    {
                        news.Id = id;
                    }
                }
            }

            var eventDate = node.QuerySelector(".event-time");

            news.EventTime = eventDate?.InnerText.Trim() ?? "";
            news.EventTime = Regex.Replace(news.EventTime, "\\s+", " ");

            var pub = node.QuerySelector(".info-icon .time");
            news.PublishOn = pub?.InnerText.Trim();

            newsList.Add(news);
        }

        newsList = newsList.DistinctBy(p => p.Id).OrderBy(p => p.Id).ToList();

        var urlString = Environment.GetEnvironmentVariable("POST_URLS");
        if (string.IsNullOrWhiteSpace(urlString)) return;
        var urls = urlString.Split(",").Select(p => p.Trim()).ToList();

        foreach (var news in newsList)
        {
            var hash = Md5($"{news.Type}|{news.Title}|{news.Url}|{news.EventTime}|{news.PublishOn}");
            var exist = db.WebsiteNews.Any(p => !p.Retired && p.Url == news.Url & p.Md5Hash == hash);
            if (exist) continue;

            await db.WebsiteNews.AddAsync(new WebsiteNews
            {
                Id = news.Id,
                Type = news.Type,
                Title = news.Title,
                Url = news.Url,
                EventTime = news.EventTime,
                PublishOn = news.PublishOn,
                Md5Hash = hash,
                CreatedOn = DateTime.UtcNow.AddHours(8),
            });

            foreach (var url in urls)
            {
                await Publish(news, url);
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task<bool> Publish(News obj, string postUrl)
    {
        var isValid = Uri.TryCreate(postUrl, UriKind.Absolute, out var uri);
        if (!isValid)
        {
            Console.WriteLine($"Skip invalid PostUrl: [{postUrl}]");
            return false;
        }

        try
        {
            var client = new RestClient(postUrl.Trim());
            var request = new RestRequest();
            var payload = new DiscordPayload
            {
                Embeds =
                [
                    new DiscordEmbedded
                    {
                        Title = $"{obj.TypeText} | {obj.Title}",
                        Url = obj.Url,
                        Description = $"{obj.EventTime + "\r\n"}{obj.PublishOn}".Trim(),
                    },
                ],
            };
            request.AddBody(payload);
            var result = await client.PostAsync(request);
            return result.IsSuccessful;
        }
        catch (Exception e)
        {
            Console.WriteLine($"publish error on [{postUrl}], {e}");
        }

        return false;
    }

    public static string Md5(string text)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(text);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes);
    }

    private class DiscordPayload
    {
        public string? Content { get; set; }
        public DiscordEmbedded[] Embeds { get; set; } = [];
    }

    private class DiscordEmbedded
    {
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string? Description { get; set; }
    }

    private class News
    {
        public int? Id { get; set; }
        public string? Type { get; set; }
        public string? TypeText { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string? EventTime { get; set; }
        public string? PublishOn { get; set; }
    }
}