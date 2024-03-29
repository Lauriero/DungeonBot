﻿using Newtonsoft.Json;

using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Utils.JsonConverter;

namespace VkNet.AudioApi.AudioBypassService.Models.Auth;

public delegate ValueTask<string> ActionRequestedDelegate(LoginWay requestedLoginWay, AuthState? state);

public record AndroidApiAuthParams(string? Login, string? Sid, ActionRequestedDelegate? ActionRequestedAsync, IEnumerable<LoginWay>? SupportedWays,
    string? Password = null, AndroidGrantType? AndroidGrantType = null, string? PasskeyData = null) : IApiAuthParams
{
    public AndroidApiAuthParams() : this(null, null, null, null)
    {
        IsAnonymous = true;
    }

    public AndroidApiAuthParams(string Login, string Password)
        : this(Login, null, null, null, Password)
    {
        IsAnonymous = false;
    }

    public ulong ApplicationId { get; set; } = 2274003;
    public string? Login { get; set; } = Login;
    public string? Password { get; set; } = Password;
    public Settings Settings { get; set; }

    [Obsolete($"Set {nameof(ActionRequestedAsync)} event", true)]
    public Func<string>? TwoFactorAuthorization { get; set; }

    public string? AccessToken { get; set; }
    public int TokenExpireTime { get; set; }
    public long UserId { get; set; }
    public ulong? CaptchaSid { get; set; }
    public string CaptchaKey { get; set; }
    public string Host { get; set; }
    public int? Port { get; set; }
    public string ProxyLogin { get; set; }
    public string ProxyPassword { get; set; }

    [Obsolete($"Use {nameof(Login)} instead", true)]
    public string? Phone { get; set; } = Login;

    public string ClientSecret { get; set; } = "hHbZxrka2uZ6jB1inYsH";
    public bool? ForceSms { get; set; }
    public Display Display { get; set; }
    public Uri RedirectUri { get; set; }
    public string State { get; set; }
    public bool? TwoFactorSupported { get; set; } = true;

    [Obsolete($"Use {nameof(AndroidGrantType)} instead", true)]
    public GrantType GrantType { get; set; }

    public AndroidGrantType? AndroidGrantType { get; init; } = AndroidGrantType ?? AndroidGrantType.Password;
    public ResponseType ResponseType { get; set; } = ResponseType.Token;
    public bool? Revoke { get; set; }
    public string Code { get; set; }
    public bool IsTokenUpdateAutomatically { get; set; }
    
    public bool IsAnonymous { get; }

    public bool IsValid =>IsAnonymous ||
                          (!string.IsNullOrEmpty(Login) && !string.IsNullOrEmpty(Sid) &&
                           ActionRequestedAsync is not null && SupportedWays?.Any() is true);
}

[JsonConverter(typeof(SafetyEnumJsonConverter))]
public class AndroidGrantType : SafetyEnum<AndroidGrantType>
{
    public static readonly AndroidGrantType? Password = RegisterPossibleValue("password");
    public static readonly AndroidGrantType Passkey = RegisterPossibleValue("passkey");
    public static readonly AndroidGrantType WithoutPassword = RegisterPossibleValue("without_password");
    public static readonly AndroidGrantType PhoneConfirmationSid = RegisterPossibleValue("phone_confirmation_sid");
}

public record AuthState;

public record TwoFactorAuthState(string PhoneMask, bool IsSms, int CodeLength) : AuthState;