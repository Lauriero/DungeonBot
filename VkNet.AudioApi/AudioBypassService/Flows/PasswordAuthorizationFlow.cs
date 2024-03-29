﻿using VkNet.Abstractions.Core;
using VkNet.Abstractions.Utils;
using VkNet.AudioApi.AudioBypassService.Abstractions;
using VkNet.AudioApi.AudioBypassService.Models.Auth;
using VkNet.AudioApi.AudioBypassService.Utils;
using VkNet.AudioApi.DIExtensions;
using VkNet.Model;
using VkNet.Utils;

namespace VkNet.AudioApi.AudioBypassService.Flows;

internal class PasswordAuthorizationFlow : VkAndroidAuthorizationBase
{
    protected override Task<AuthorizationResult> AuthorizeAsync(AndroidApiAuthParams authParams)
    {
        if (string.IsNullOrEmpty(authParams.Password) && !authParams.IsAnonymous)
            throw new ArgumentException("Password is required for this flow type", nameof(authParams));
        
        return AuthAsync(authParams);
    }

    protected override async ValueTask<VkParameters> BuildParameters(AndroidApiAuthParams authParams)
    {
        var parameters = await base.BuildParameters(authParams);
        
        parameters.Add("username", authParams.Login);
        parameters.Add("password", authParams.Password);
        parameters.Add("flow_type", "tg_flow");
        parameters.Add("2fa_supported", true);
        parameters.Add("vk_connect_auth", true);
        
        return parameters;
    }

    public PasswordAuthorizationFlow(IVkTokenStore tokenStore, FakeSafetyNetClient safetyNetClient,
        IDeviceIdStore deviceIdStore, IVkApiVersionManager versionManager, ILanguageService languageService,
        IAsyncRateLimiter rateLimiter, IRestClient restClient, ICaptchaHandler captchaHandler,
        LibVerifyClient libVerifyClient) : base(tokenStore, safetyNetClient, deviceIdStore,
        versionManager, languageService, rateLimiter, restClient, captchaHandler, libVerifyClient)
    {
    }
}