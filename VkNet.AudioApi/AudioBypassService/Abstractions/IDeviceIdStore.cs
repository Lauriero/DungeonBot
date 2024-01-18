namespace VkNet.AudioApi.AudioBypassService.Abstractions;

public interface IDeviceIdStore
{
    ValueTask<string?> GetDeviceIdAsync();
    ValueTask SetDeviceIdAsync(string deviceId);
}