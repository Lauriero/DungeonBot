using VkNet.AudioApi.AudioBypassService.Exceptions;
using VkNet.Exception;
using VkNet.Model;

namespace VkNet.AudioApi.AudioBypassService.Utils
{
	public static class VkAuthErrorFactory
	{
		public static System.Exception Create(VkAuthError error)
		{
			switch (error.Error)
			{
				case "need_captcha":
					return new CaptchaNeededException(new VkError
					{
						CaptchaImg = error.CaptchaImg,
						CaptchaSid = error.CaptchaSid.GetValueOrDefault(),
						ErrorMessage = error.ErrorDescription
					});
				default:
					return new VkAuthException(error);
			}
		}
	}
}