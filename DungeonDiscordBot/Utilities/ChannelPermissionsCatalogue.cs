using Discord;

namespace DungeonDiscordBot.Utilities;

public class ChannelPermissionsCatalogue
{
    public static readonly ChannelPermission[] ForVoiceChannel = {
        ChannelPermission.Speak,
    };
    
    public static readonly ChannelPermission[] ForMusicControlChannel = {
        ChannelPermission.SendMessages,
        ChannelPermission.ManageMessages,
        ChannelPermission.ReadMessageHistory,
        ChannelPermission.UseExternalEmojis,
        ChannelPermission.EmbedLinks
    };
}