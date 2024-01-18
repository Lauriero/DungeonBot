namespace VkNet.AudioApi.AudioBypassService.Models.Auth;

public record AnonymousTokenResponse(string Token, int ExpiredAt);