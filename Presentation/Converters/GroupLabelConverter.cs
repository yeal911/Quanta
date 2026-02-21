// ============================================================================
// 文件名: GroupLabelConverter.cs
// 文件用途: 将搜索结果分组标签（内部键名）转换为本地化的显示文字。
//          例如将 "App" 转为 "应用"（中文）或 "Apps"（英文）。
// ============================================================================

using System.Globalization;
using System.Windows.Data;
using Quanta.Services;

namespace Quanta.Helpers;

/// <summary>
/// 搜索结果分组标签转换器，将内部分组键名转换为当前语言的显示文本。
/// 绑定时 CollectionViewGroup.Name 传入此 Converter。
/// </summary>
[ValueConversion(typeof(string), typeof(string))]
public class GroupLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value?.ToString() ?? "") switch
        {
            "Command" => LocalizationService.Get("GroupCommand"),
            "App" => LocalizationService.Get("GroupApp"),
            "File" => LocalizationService.Get("GroupFile"),
            "Window" => LocalizationService.Get("GroupWindow"),
            "Calc" => LocalizationService.Get("GroupCalc"),
            "Web" => LocalizationService.Get("GroupWeb"),
            "Text" => LocalizationService.Get("GroupText"),
            "Clip" => LocalizationService.Get("GroupClip"),
            _ => value?.ToString() ?? ""
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => System.Windows.Data.Binding.DoNothing;
}
