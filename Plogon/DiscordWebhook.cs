using System;
using System.Collections.Generic;
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
    public DiscordWebhookClient? Client { get; }

    /// <summary>
    /// Init with webhook from env var
    /// </summary>
    public DiscordWebhook(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return;

        this.Client = new DiscordWebhookClient(url);
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
        if (this.Client == null) 
            throw new Exception("Webhooks not set up");
        
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

    public async Task SendSplitting(Color color, string message, string title, string footer)
    {
        var messages = new List<string>();

        var buffer = "";
        foreach (var part in message.Split("\n"))
        {
            if (buffer.Length + part.Length > 2000)
            {
                messages.Add(buffer.Trim());
                buffer = "";
            }

            buffer += part + "\n";
        }
        
        // flush final buffer
        messages.Add(buffer.Trim());

        foreach (var body in messages)
        {
            await this.Send(color, body, title, footer);
        }
    }
}
