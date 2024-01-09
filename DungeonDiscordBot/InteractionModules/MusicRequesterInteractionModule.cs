using Discord;
using Discord.Interactions;

using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.MusicProvidersControllers;
using DungeonDiscordBot.Services.Abstraction;
using DungeonDiscordBot.Utilities;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

using ILogger = Serilog.ILogger;

namespace DungeonDiscordBot.InteractionModules;

public class MusicRequesterInteractionModule : BaseInteractionModule<SocketInteractionContext>
{
    private readonly IUserInterfaceService _UI;
    private readonly ILogger<BaseInteractionModule<SocketInteractionContext>> _logger;

    protected MusicRequesterInteractionModule(ILogger<BaseInteractionModule<SocketInteractionContext>> logger, 
        IUserInterfaceService ui) : base(logger)
    {
        _logger = logger;
        _UI = ui;
    }

    /// <summary>
    /// Finds target <see cref="MusicProvider"/> by <paramref name="audiosUri"/>
    /// and executes <see cref="BaseMusicProviderController.GetAudiosFromLinkAsync"/>
    /// returning the music collection that is located at <paramref name="audiosUri"/>
    /// and handling the errors by modifying the bot original response as to specify the error occurred.  
    /// </summary>
    /// <param name="audiosUri">Specifies the resource url the music collection is located at.</param>
    /// <param name="audiosQuantity">
    /// Specified the maximum amount of audios that will be placed in the collection.
    /// </param>
    /// <param name="handleErrors">
    /// Specifies whether this method will modify the original bot response with correlative error messages.
    /// </param>
    /// <returns>
    /// Response of the <see cref="BaseMusicProviderController.GetAudiosFromLinkAsync"/>
    /// if the target <see cref="MusicProvider"/> was successfully resolved, null otherwise.
    /// </returns>
    protected async Task<MusicCollectionResponse?> FetchMusicCollectionFromUrlAsync(Uri audiosUri,
        int audiosQuantity, bool handleErrors)
    {
        BaseMusicProviderController? controller = audiosUri.FindMusicProviderController();
        if (controller is null) {
            if (handleErrors) {
                await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(
                    _UI.GenerateMusicServiceNotFoundMessage(Context.Guild.CurrentUser, audiosUri.AbsoluteUri)));
            }
            
            return null;
        }
        
        MusicCollectionResponse collection = await controller.GetAudiosFromLinkAsync(audiosUri, audiosQuantity);
        if (collection.IsError) {
            _logger.LogInformation($"Error while getting music from {collection.Provider.Name} music provider " +
                                   $"[guildId: {Context.Guild.Id}; query: {audiosUri.AbsoluteUri}]: " +
                                   $"{collection.ErrorType} - {collection.ErrorMessage}");

            if (!handleErrors) {
                return collection;
            }
            
            switch (collection.ErrorType) {
                case MusicResponseErrorType.PermissionDenied:
                    await ModifyOriginalResponseAsync((m) 
                        => m.Content = $"Permission to audio was denied");
                    break;

                case MusicResponseErrorType.NoAudioFound:
                    await ModifyOriginalResponseAsync((m) 
                        => m.Content = $"No audio was found by the requested url");
                    break;
                    
                case MusicResponseErrorType.LinkNotSupported:
                    await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(
                        _UI.GenerateMusicServiceLinkNotSupportedMessage(controller, audiosUri.AbsoluteUri)));
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(collection.ErrorType), 
                        "Received a music collection response with invalid error type");
            }
        }

        return collection;
    }
}