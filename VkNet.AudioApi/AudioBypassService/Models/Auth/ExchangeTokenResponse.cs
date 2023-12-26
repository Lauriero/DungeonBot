using VkNet.Model;

namespace VkNet.AudioApi.AudioBypassService.Models.Auth;

public record ExchangeTokenResponse(string ExchangeToken, User Profile);