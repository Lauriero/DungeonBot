using System.Text.Json;

namespace VkNet.AudioApi.AudioBypassService.Utils;

public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public static SnakeCaseNamingPolicy Instance { get; } = new SnakeCaseNamingPolicy();

    public override string ConvertName(string name)
    {
        return name.ToSnakeCase();
    }
}