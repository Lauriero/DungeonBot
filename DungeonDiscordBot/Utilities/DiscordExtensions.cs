using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;

using VkNet.AudioApi.Model;

namespace DungeonDiscordBot.Utilities;

public static class DiscordExtensions
{
    /// <summary>
    /// Checks if current user has all permissions from <paramref name="requiredPermissions"/>
    /// for the <paramref name="channel"/>.
    /// </summary>
    /// <returns>True if user has all the permissions listed, false otherwise.</returns>
    public static bool CheckChannelPermissions(this SocketGuildChannel channel, IEnumerable<ChannelPermission> requiredPermissions)
    {
        ChannelPermissions permissions = channel.Guild.CurrentUser.GetPermissions(channel);
        foreach (ChannelPermission permission in requiredPermissions) {
            if (!permissions.Has(permission)) {
                return false;
            }
        }

        return true;
    }

    public static Task<RestUserMessage> SendMessageAsync(this ISocketMessageChannel channel, MessageProperties properties, 
        RequestOptions? options = null)
    {
        return channel.SendMessageAsync(
            text: properties.Content.GetValueOrDefault(),
            embed: properties.Embed.GetValueOrDefault(),
            allowedMentions: properties.AllowedMentions.GetValueOrDefault(),
            components: properties.Components.GetValueOrDefault(),
            embeds: properties.Embeds.GetValueOrDefault(),
            flags: properties.Flags.GetValueOrDefault() ?? MessageFlags.None,
            options: options);
    }

    public static SocketVoiceChannel? GetVoiceChannelWithCurrentUser(this SocketInteractionContext context)
    {
        foreach (SocketVoiceChannel? channel in context.Guild.VoiceChannels) {
            foreach (SocketGuildUser user in channel.ConnectedUsers) {
                if (user.Id == context.User.Id) {
                    return channel;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Replaces all properties of the target message with properties from the specified object.
    /// </summary>
    public static void ApplyMessageProperties(this MessageProperties target, MessageProperties properties)
    {
        target.Attachments = properties.Attachments;
        target.Components = properties.Components;
        target.Content = properties.Content;
        target.Embed = properties.Embed;
        target.Embeds = properties.Embeds;
        target.Flags = properties.Flags;
        target.AllowedMentions = properties.AllowedMentions;
    }
}