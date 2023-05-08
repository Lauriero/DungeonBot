namespace DungeonDiscordBot.Utilities;

public static class AsyncExtensions
{
    /// <summary>
    /// Waits until cancellation will be requested from a token.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static Task WaitForCancellationAsync(this CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<bool>();
        ct.Register(s => ((TaskCompletionSource<bool>)s!).SetResult(true), tcs);
        return tcs.Task;
    }
}