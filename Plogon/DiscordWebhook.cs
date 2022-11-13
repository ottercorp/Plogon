using System;
using System.Threading.Tasks;
using Discord;
using Discord.Webhook;

namespace Plogon;

/// <summary>
/// Responsible for sending discord webhooks
/// </summary>
public class DiscordWebhook
{
    /// <summary>
    /// Webhook client
    /// </summary>
    public DiscordWebhookClient Client { get; }

    /// <summary>
    /// Init with webhook from env var
    /// </summary>
    public DiscordWebhook()
    {
        this.Client = new DiscordWebhookClient(Environment.GetEnvironmentVariable("DISCORD_WEBHOOK"));
    }

    private static DateTime GetChinaStandardTime()
    {
        var utc = DateTime.UtcNow;
        var chinaStandardZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
        var chinaStandardTime = TimeZoneInfo.ConvertTimeFromUtc(utc, chinaStandardZone);
        return chinaStandardTime;
    }

    /// <summary>
    /// Send a webhook
    /// </summary>
    /// <param name="color"></param>
    /// <param name="message"></param>
    /// <param name="title"></param>
    /// <param name="footer"></param>
    public async Task<ulong> Send(Color color, string message, string title, string footer)
    {
        if (message.Length > 4000)
        {
            message = message.Substring(0, 4000);
        }
        var embed = new EmbedBuilder()
            .WithColor(color)
            .WithTitle(title)
            .WithFooter(footer)
            .WithDescription(message)
            .Build();

        var time = GetChinaStandardTime();
        var username = "Odder";
        var avatarUrl = "https://ottercorp.github.io/icons/odder.png";
        if (time.Hour is > 20 or < 7)
        {
            username = "Otter";
            avatarUrl = "https://ottercorp.github.io/icons/otter.png";
        }

        return await this.Client.SendMessageAsync(embeds: new[] { embed }, username: username, avatarUrl: avatarUrl);
    }
}