using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using System;

namespace SignalRDemo.Client.Services;

/// <summary>
/// 主题类型
/// </summary>
public enum AppTheme
{
    Dark,
    Light
}

/// <summary>
/// 主题管理服务
/// </summary>
public class ThemeService
{
    private static ThemeService? _instance;
    public static ThemeService Instance => _instance ??= new ThemeService();

    private readonly StyleInclude _darkTheme;
    private readonly StyleInclude _lightTheme;

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;

    public event Action<AppTheme>? ThemeChanged;

    private ThemeService()
    {
        // 加载主题样式
        _darkTheme = new StyleInclude(new Uri("avares://SignalRDemo.Client/"))
        {
            Source = new Uri("avares://SignalRDemo.Client/Styles/DarkTheme.axaml")
        };

        _lightTheme = new StyleInclude(new Uri("avares://SignalRDemo.Client/"))
        {
            Source = new Uri("avares://SignalRDemo.Client/Styles/LightTheme.axaml")
        };
    }

    /// <summary>
    /// 设置主题
    /// </summary>
    public void SetTheme(AppTheme theme)
    {
        if (CurrentTheme == theme) return;

        var app = Application.Current;
        if (app == null) return;

        // 移除当前主题
        var currentStyle = CurrentTheme == AppTheme.Dark ? _darkTheme : _lightTheme;
        if (app.Styles.Contains(currentStyle))
        {
            app.Styles.Remove(currentStyle);
        }

        // 应用新主题
        var newStyle = theme == AppTheme.Dark ? _darkTheme : _lightTheme;
        app.Styles.Add(newStyle);

        CurrentTheme = theme;
        ThemeChanged?.Invoke(theme);
    }

    /// <summary>
    /// 切换主题
    /// </summary>
    public void ToggleTheme()
    {
        SetTheme(CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
    }

    /// <summary>
    /// 初始化主题（在 App 启动时调用）
    /// </summary>
    public void Initialize(AppTheme defaultTheme = AppTheme.Dark)
    {
        var app = Application.Current;
        if (app == null) return;

        var style = defaultTheme == AppTheme.Dark ? _darkTheme : _lightTheme;
        app.Styles.Add(style);
        CurrentTheme = defaultTheme;
    }
}
