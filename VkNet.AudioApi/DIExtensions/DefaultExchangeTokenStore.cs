using VkNet.AudioApi.AudioBypassService.Abstractions;

namespace VkNet.AudioApi.DIExtensions;

public class DefaultExchangeTokenStore : IExchangeTokenStore
{
    private string? exchangeToken;

    public ValueTask<string?> GetExchangeTokenAsync()
    {
        return new(exchangeToken);
    }

    public async ValueTask SetExchangeTokenAsync(string? token)
    {
        exchangeToken = token;
    }
}