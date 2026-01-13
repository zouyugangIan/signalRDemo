using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace SignalRDemo.Client.ViewModels;

/// <summary>
/// 布尔值转换为颜色 (用于连接状态指示)
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected ? Colors.LimeGreen : Colors.Gray;
        }
        return Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 用户名转换为首字母 (用于头像)
/// </summary>
public class NameToInitialConverter : IValueConverter
{
    public static readonly NameToInitialConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string name && !string.IsNullOrEmpty(name))
        {
            return name[0].ToString().ToUpper();
        }
        return "?";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
