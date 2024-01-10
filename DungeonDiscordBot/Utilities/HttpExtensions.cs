using System.Net;

namespace DungeonDiscordBot.Utilities;

public class HttpExtensions
{
    /// <summary>
    /// Checks whether the file exists.
    /// </summary>
    public static async Task<bool> RemoteFileExists(string url, TimeSpan timeout = default)
    {
        try {
            using (HttpClient client = new HttpClient()) {
                if (timeout != default) {
                    client.Timeout = timeout;
                }
                
                HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url),
                    HttpCompletionOption.ResponseHeadersRead);

                return response.IsSuccessStatusCode;
            }
        } catch {
            return false;
        }
    }
}