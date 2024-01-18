using Microsoft.Extensions.DependencyInjection;

using VkNet.AudioApi.AudioBypassService.Abstractions;
using VkNet.AudioApi.AudioBypassService.Extensions;
using VkNet.AudioApi.AudioBypassService.Utils;
using VkNet.AudioApi.DIExtensions;

namespace VkNet.AudioApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVkAudioApi(this IServiceCollection collection)
    {
        return collection
            .AddSingleton<IVkTokenStore, DefaultVkTokenStore>()
            .AddSingleton<IDeviceIdStore, DefaultDeviceIdStore>()
            .AddSingleton<IExchangeTokenStore, DefaultExchangeTokenStore>()
            .AddAudioBypass()
            .AddVkNet()
            .AddSingleton<IVkAudioApi, VkAudioApi>();
    }
}