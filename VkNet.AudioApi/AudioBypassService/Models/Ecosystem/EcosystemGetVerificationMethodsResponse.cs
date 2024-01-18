using System.Collections.ObjectModel;

using VkNet.AudioApi.AudioBypassService.Models.Auth;

namespace VkNet.AudioApi.AudioBypassService.Models.Ecosystem;

public record EcosystemGetVerificationMethodsResponse(ReadOnlyCollection<EcosystemVerificationMethod> Methods);

public record EcosystemVerificationMethod(LoginWay Name, int Priority, int Timeout, string Info, bool CanFallback);