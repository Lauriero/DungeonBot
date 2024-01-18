namespace VkNet.AudioApi.AudioBypassService.Abstractions;

public interface IExchangeTokenStore
{
    ValueTask<string?> GetExchangeTokenAsync();
    ValueTask SetExchangeTokenAsync(string? token);
}