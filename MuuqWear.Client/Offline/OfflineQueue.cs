using Microsoft.JSInterop;

namespace MuuqWear.Client.Offline;

public sealed class OfflineQueue
{
    private readonly IJSRuntime _js;

    public OfflineQueue(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<bool> IsOnlineAsync()
    {
        try
        {
            return await _js.InvokeAsync<bool>("mwIsOnline");
        }
        catch
        {
            return true;
        }
    }

    public async Task EnqueueAsync(string kind, object payload)
    {
        if (!OfflineAction.Queues(kind))
            throw new InvalidOperationException($"{kind} cannot queue.");

        await _js.InvokeVoidAsync("mwOutbox.enqueue", kind, payload);
    }
}
