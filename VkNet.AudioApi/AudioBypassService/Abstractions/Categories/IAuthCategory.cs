using VkNet.AudioApi.AudioBypassService.Models.Auth;
using VkNet.Enums.Filters;

namespace VkNet.AudioApi.AudioBypassService.Abstractions.Categories;

public interface IAuthCategory
{
    Task<AuthValidateAccountResponse> ValidateAccountAsync(string login, bool forcePassword = false, bool passkeySupported = false, IEnumerable<LoginWay>? loginWays = null);
    Task<AuthValidatePhoneResponse> ValidatePhoneAsync(string phone, string sid, bool allowCallReset = true, IEnumerable<LoginWay>? loginWays = null);

    Task<AuthCodeResponse> GetAuthCodeAsync(string deviceName, bool forceRegenerate = true);
    
    Task<AuthCheckResponse> CheckAuthCodeAsync(string authHash);
    
    Task<TokenInfo?> RefreshTokensAsync(string oldToken, string exchangeToken);

    Task<ExchangeTokenResponse> GetExchangeToken(UsersFields? fields = null);
    
    Task<PasskeyBeginResponse> BeginPasskeyAsync(string sid);
}