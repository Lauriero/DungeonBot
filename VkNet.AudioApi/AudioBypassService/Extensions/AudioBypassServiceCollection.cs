using JetBrains.Annotations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using VkNet.Abstractions;
using VkNet.Abstractions.Authorization;
using VkNet.Abstractions.Utils;
using VkNet.AudioApi.AudioBypassService.Abstractions;
using VkNet.AudioApi.AudioBypassService.Abstractions.Categories;
using VkNet.AudioApi.AudioBypassService.Categories;
using VkNet.AudioApi.AudioBypassService.Flows;
using VkNet.AudioApi.AudioBypassService.Models.Auth;
using VkNet.AudioApi.AudioBypassService.Utils;
using VkNet.AudioApi.DIExtensions;

using IAuthCategory = VkNet.AudioApi.AudioBypassService.Abstractions.Categories.IAuthCategory;
using VkApiAuth = VkNet.AudioApi.AudioBypassService.Utils.VkApiAuth;
using VkApiInvoke = VkNet.AudioApi.AudioBypassService.Utils.VkApiInvoke;

namespace VkNet.AudioApi.AudioBypassService.Extensions
{
	public static class AudioBypassServiceCollection
	{
		public static IServiceCollection AddAudioBypass([NotNull] this IServiceCollection services)
		{
			if (services == null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			services.TryAddSingleton<FakeSafetyNetClient>();
			services.TryAddSingleton<LibVerifyClient>();
			services.TryAddSingleton<IRestClient, RestClientWithUserAgent>();
			services.TryAddSingleton<IDeviceIdStore, DefaultDeviceIdStore>();
			services.TryAddSingleton<ITokenRefreshHandler, TokenRefreshHandler>();
			services.TryAddSingleton<IVkApiInvoke, VkApiInvoke>();
			services.TryAddSingleton<IVkApiAuthAsync, VkApiAuth>();
			
			services.TryAddKeyedSingleton<IAuthorizationFlow, PasswordAuthorizationFlow>(AndroidGrantType.Password);
			services.TryAddKeyedSingleton(AndroidGrantType.PhoneConfirmationSid,
				(s, _) => s.GetRequiredKeyedService<IAuthorizationFlow>(AndroidGrantType.Password));
			services.TryAddKeyedSingleton<IAuthorizationFlow, WithoutPasswordAuthorizationFlow>(AndroidGrantType.WithoutPassword);
			services.TryAddKeyedSingleton<IAuthorizationFlow, PasskeyAuthorizationFlow>(AndroidGrantType.Passkey);
			
			services.TryAddSingleton(s => s.GetRequiredKeyedService<IAuthorizationFlow>(AndroidGrantType.Password));
			
			services.TryAddSingleton<IAuthCategory, AuthCategory>();
			services.TryAddSingleton<ILoginCategory, LoginCategory>();
			services.TryAddSingleton<IEcosystemCategory, EcosystemCategory>();

			return services;
		}
	}
}