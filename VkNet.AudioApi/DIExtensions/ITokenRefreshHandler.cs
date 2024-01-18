namespace VkNet.AudioApi.DIExtensions;

public interface ITokenRefreshHandler
{
    Task<string?> RefreshTokenAsync(string oldToken);
}