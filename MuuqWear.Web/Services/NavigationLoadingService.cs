using Microsoft.JSInterop;

namespace MuuqWear.Web.Services;

/// <summary>
/// Coordinates nav progress / link spinners with page data readiness (not just route swap).
/// </summary>
public sealed class NavigationLoadingService
{
    private readonly IJSRuntime _js;
    private bool _awaitingContent;

    public NavigationLoadingService(IJSRuntime js) => _js = js;

    public void BeginNavigation() => _awaitingContent = true;

    public async Task MarkContentReadyAsync()
    {
        if (!_awaitingContent)
            return;

        _awaitingContent = false;

        try
        {
            await _js.InvokeVoidAsync("mwNavProgress.pageReady");
        }
        catch (JSDisconnectedException) { }
        catch (InvalidOperationException) { }
    }
}
