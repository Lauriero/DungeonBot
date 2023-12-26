namespace VkNet.AudioApi.AudioBypassService.Models.Auth;

public record AuthValidatePhoneResponse(LoginWay ValidationType, LoginWay ValidationResend, string Sid, int Delay, int CodeLength, bool LibverifySupport, string Phone);