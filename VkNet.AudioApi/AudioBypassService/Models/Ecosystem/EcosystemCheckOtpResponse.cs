﻿namespace VkNet.AudioApi.AudioBypassService.Models.Ecosystem;

public record EcosystemCheckOtpResponse(string Sid, bool ProfileExist, bool CanSkipPassword, EcosystemProfile Profile);