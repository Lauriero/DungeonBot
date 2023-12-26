namespace VkNet.AudioApi.AudioBypassService.Models.Auth;

public record AuthCodeResponse(string AuthCode, string AuthHash, string AuthId, string AuthUrl, int ExpiresIn);