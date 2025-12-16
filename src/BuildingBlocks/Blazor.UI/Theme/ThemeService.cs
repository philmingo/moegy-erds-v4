using Microsoft.JSInterop;

namespace FSH.Framework.Blazor.UI.Theme;

public interface IThemeService
{
    bool IsDarkMode { get; }
    event Action? OnThemeChanged;
    Task ToggleThemeAsync();
    Task SetThemeAsync(bool isDark);
    Task InitializeAsync();
}

public class ThemeService : IThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isDarkMode;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool IsDarkMode => _isDarkMode;

    public event Action? OnThemeChanged;

    public async Task InitializeAsync()
    {
        try
        {
            var theme = await _jsRuntime.InvokeAsync<string>("getTheme");
            _isDarkMode = theme == "dark";
        }
        catch
        {
            _isDarkMode = false;
        }
    }

    public async Task ToggleThemeAsync()
    {
        _isDarkMode = !_isDarkMode;
        await UpdateThemeAsync();
    }

    public async Task SetThemeAsync(bool isDark)
    {
        _isDarkMode = isDark;
        await UpdateThemeAsync();
    }

    private async Task UpdateThemeAsync()
    {
        try
        {
            var theme = _isDarkMode ? "dark" : "light";
            await _jsRuntime.InvokeVoidAsync("setTheme", theme);
            OnThemeChanged?.Invoke();
        }
        catch
        {
            // Handle JS interop errors gracefully
        }
    }
}
