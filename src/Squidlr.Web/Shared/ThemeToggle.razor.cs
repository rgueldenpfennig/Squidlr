using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.JSInterop;

namespace Squidlr.Web.Shared;

public sealed partial class ThemeToggle : IAsyncDisposable
{
    private bool _isDarkMode;
    private IJSObjectReference? _module;

    [Inject]
    public required IJSRuntime JSRuntime { get; set; }

    [Inject]
    public required IFileVersionProvider FileVersionProvider { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", FileVersionProvider.AddFileVersionToPath("/", "./js/theme.js"));
            _isDarkMode = await _module.InvokeAsync<bool>("isDarkMode");

            StateHasChanged();
        }
    }

    private async Task ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("setTheme", _isDarkMode);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // can be ignored when the circuit is closed
            }
        }
    }
}