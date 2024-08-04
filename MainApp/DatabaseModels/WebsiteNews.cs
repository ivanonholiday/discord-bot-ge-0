using System;
using System.Collections.Generic;

namespace MainApp.DatabaseModels;

public partial class WebsiteNews
{
    public int NewsId { get; set; }

    public int? Id { get; set; }

    public string? Type { get; set; }

    public string? Title { get; set; }

    public string? Url { get; set; }

    public string? EventTime { get; set; }

    public string? PublishOn { get; set; }

    public string Md5Hash { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public bool Retired { get; set; }
}
