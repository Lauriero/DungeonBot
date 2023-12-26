namespace VkNet.AudioApi.DIExtensions;

public interface IVkTokenStore
{
    string Token { get; }
    Task SetAsync(string? token, DateTimeOffset? expiration = null);
}