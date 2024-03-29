﻿using VkNet.Abstractions.Core;
using VkNet.Abstractions.Utils;
using VkNet.AudioApi.AudioBypassService.Abstractions;
using VkNet.AudioApi.AudioBypassService.Models.Auth;
using VkNet.AudioApi.AudioBypassService.Utils;
using VkNet.AudioApi.DIExtensions;
using VkNet.Model;
using VkNet.Utils;

namespace VkNet.AudioApi.AudioBypassService.Flows;

internal class PasskeyAuthorizationFlow : VkAndroidAuthorizationBase
{
    public PasskeyAuthorizationFlow(IVkTokenStore tokenStore, FakeSafetyNetClient safetyNetClient,
        IDeviceIdStore deviceIdStore, IVkApiVersionManager versionManager, ILanguageService languageService,
        IAsyncRateLimiter rateLimiter, IRestClient restClient, ICaptchaHandler captchaHandler,
        LibVerifyClient libVerifyClient) : base(tokenStore, safetyNetClient, deviceIdStore, versionManager,
        languageService, rateLimiter, restClient, captchaHandler, libVerifyClient)
    {
    }

    protected override Task<AuthorizationResult> AuthorizeAsync(AndroidApiAuthParams authParams)
    {
        if (string.IsNullOrEmpty(authParams.PasskeyData))
            throw new ArgumentException("Passkey data is empty", nameof(authParams));
        
        return AuthAsync(authParams);
    }

    protected override async ValueTask<VkParameters> BuildParameters(AndroidApiAuthParams authParams)
    {
        var parameters = await base.BuildParameters(authParams);
        
        parameters.Add("passkey_data", authParams.PasskeyData);
        parameters.Add("flow_type", "tg_flow");
        
        return parameters;
    }
}