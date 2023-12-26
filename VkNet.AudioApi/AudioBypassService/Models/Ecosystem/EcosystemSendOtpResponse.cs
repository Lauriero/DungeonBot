namespace VkNet.AudioApi.AudioBypassService.Models.Ecosystem;

public record EcosystemSendOtpResponse(int Status, string Sid, string Info, int CodeLength);