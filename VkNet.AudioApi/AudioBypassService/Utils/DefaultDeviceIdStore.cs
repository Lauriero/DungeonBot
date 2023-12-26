using VkNet.AudioApi.AudioBypassService.Abstractions;

namespace VkNet.AudioApi.AudioBypassService.Utils;

public class DefaultDeviceIdStore : IDeviceIdStore
{
    private string? _deviceId;
    
    public ValueTask<string?> GetDeviceIdAsync()
    {
        return new(_deviceId);
    }

    public ValueTask SetDeviceIdAsync(string deviceId)
    {
        _deviceId = deviceId;
        return ValueTask.CompletedTask;
    }
}