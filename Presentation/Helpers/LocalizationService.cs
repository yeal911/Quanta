// ============================================================================
// 文件名: LocalizationService.cs
// 描述: 本地化静态门面（DI 统一第一步的兼容层）。
//       签名与原静态 LocalizationService 完全一致，现有调用点无需修改；
//       内部委托容器解析的 LocalizationManager 实例（AppServices.Configure
//       之后），容器可用前回退到 LocalizationManager.Default，两者为同一实例。
// ============================================================================

using System.Collections.Generic;
using Quanta.Core;
using Quanta.Core.DependencyInjection;

namespace Quanta.Presentation.Helpers;

/// <summary>
/// 本地化静态门面。保留原静态 LocalizationService 的全部签名以兼容既有调用点；
/// 实际翻译逻辑位于 DI 注册的 <see cref="LocalizationManager"/> 实例中，
/// 本类只负责把调用转发给容器解析的实例。新代码请改为构造函数注入
/// <see cref="Quanta.Core.Interfaces.ILocalizationService"/>。
/// </summary>
public static class LocalizationService
{
    /// <summary>
    /// 当前本地化实现：容器配置后从 DI 解析，早期启动阶段回退到共享默认实例。
    /// 两者始终是同一对象，静态路径与注入路径行为完全一致。
    /// </summary>
    private static LocalizationManager Current
        => AppServices.TryGet<LocalizationManager>() ?? LocalizationManager.Default;

    /// <summary>
    /// 重新加载所有语言（用于刷新外部语言包）
    /// </summary>
    public static void ReloadAllLanguages()
    {
        Current.ReloadAllLanguages();
    }

    /// <summary>
    /// 当前语言切换成功后触发的事件，事件参数为新语言代码。
    /// 订阅者（窗口、托盘菜单等）在任意入口切换语言时都会收到通知，实现即时刷新。
    /// </summary>
    public static event System.EventHandler<string>? LanguageChanged
    {
        add => Current.LanguageChanged += value;
        remove => Current.LanguageChanged -= value;
    }

    /// <summary>
    /// 当前语言是否为从右到左（RTL）布局语言（如阿拉伯语）。
    /// 窗口据此切换 FlowDirection。
    /// </summary>
    public static bool IsCurrentLanguageRightToLeft => Current.IsCurrentLanguageRightToLeft;

    /// <summary>
    /// 获取或设置当前语言代码。
    /// 设置时会验证语言是否受支持，并自动将选择持久化到配置文件中。
    /// </summary>
    public static string CurrentLanguage
    {
        get => Current.CurrentLanguage;
        set => Current.CurrentLanguage = value;
    }

    /// <summary>
    /// 从应用配置文件中加载语言设置。
    /// 在应用启动时调用，以恢复用户之前选择的语言。
    /// </summary>
    public static void LoadFromConfig()
    {
        Current.LoadFromConfig();
    }

    /// <summary>
    /// 根据翻译键名获取当前语言的翻译文本。
    /// 如果当前语言中找不到对应翻译，会回退到中文（zh-CN）。
    /// 如果中文中也找不到，则直接返回键名本身。
    /// </summary>
    /// <param name="key">翻译键名</param>
    /// <returns>对应的翻译文本</returns>
    public static string Get(string key)
    {
        return Current.Get(key);
    }

    /// <summary>
    /// 根据翻译键名获取当前语言的翻译文本，并使用参数进行格式化。
    /// 适用于包含占位符（如 {0}、{1}）的翻译模板。
    /// </summary>
    /// <param name="key">翻译键名</param>
    /// <param name="args">格式化参数</param>
    /// <returns>格式化后的翻译文本</returns>
    public static string Get(string key, params object[] args)
    {
        return Current.Get(key, args);
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // 动态语言支持 - 统一语言管理
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 获取所有支持的语言列表
    /// </summary>
    public static IReadOnlyList<Quanta.Core.LanguageInfo> GetSupportedLanguages()
    {
        return Current.GetSupportedLanguages();
    }

    /// <summary>
    /// 获取语言显示名称的翻译键名
    /// </summary>
    public static string GetLanguageDisplayKey(string languageCode)
    {
        return Current.GetLanguageDisplayKey(languageCode);
    }

    /// <summary>
    /// 设置语言（带验证）
    /// </summary>
    public static bool TrySetLanguage(string languageCode)
    {
        return Current.TrySetLanguage(languageCode);
    }
}
