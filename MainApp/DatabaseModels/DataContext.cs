using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MainApp.DatabaseModels;

public partial class DataContext : DbContext
{
    public DataContext()
    {
    }

    public DataContext(DbContextOptions<DataContext> options)
        : base(options)
    {
    }

    public virtual DbSet<WebsiteNews> WebsiteNews { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(Environment.GetEnvironmentVariable("DB_CONN"), o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WebsiteNews>(entity =>
        {
            entity.HasKey(e => e.NewsId);

            entity.ToTable("website_news");

            entity.Property(e => e.NewsId).HasColumnName("news_id");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_on");
            entity.Property(e => e.EventTime).HasColumnName("event_time");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Md5Hash).HasColumnName("md5_hash");
            entity.Property(e => e.PublishOn).HasColumnName("publish_on");
            entity.Property(e => e.Retired).HasColumnName("retired");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Url).HasColumnName("url");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
