using UnityEngine;
using VTS.MiniGameService;

/// <summary>
/// 平台入口开关解析：把 PlatformEntryConfigSO 里的开关与当前平台能力合并成一个结果。
/// 由 CreateServices 注入配置；**未注入时不做任何拦截**（保持旧行为，服务照常响应）。
/// 编辑器下视为全平台可用，方便策划调样式。
/// </summary>
public static class PlatformEntry
{
    private static PlatformEntryConfigSO s_config;

    /// <summary> 注入入口配置（CreateServices 内调用；传 null 表示不启用开关控制） </summary>
    public static void Configure(PlatformEntryConfigSO config)
    {
        s_config = config;
    }

    /// <summary> 是否启用了开关控制 </summary>
    public static bool IsConfigured => s_config != null;

    /// <summary> 当前是否为编辑器/PC 等非小游戏平台（Mock 实现） </summary>
    public static bool IsMockPlatform
    {
        get
        {
#if UNITY_EDITOR
            return true;
#elif MINIGAME_SUBPLATFORM_WEIXIN || MINIGAME_SUBPLATFORM_DOUYIN
            return false;
#else
            return true;
#endif
        }
    }

    public static bool IsWeChatPlatform
    {
        get
        {
#if MINIGAME_SUBPLATFORM_WEIXIN
            return true;
#else
            return false;
#endif
        }
    }

    public static bool IsByteDancePlatform
    {
        get
        {
#if MINIGAME_SUBPLATFORM_DOUYIN
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary> 是否显示游戏圈入口（微信原生能力） </summary>
    public static bool ShowGameClub =>
        s_config == null || (s_config.showGameClub && (IsMockPlatform || IsWeChatPlatform));

    /// <summary> 是否显示客服入口（微信客服，需在 ShareConfigSO 填 corpId/url） </summary>
    public static bool ShowCustomerService =>
        s_config == null || (s_config.showCustomerService && (IsMockPlatform || IsWeChatPlatform));

    /// <summary> 是否显示关注官方号入口（抖音） </summary>
    public static bool ShowFollowAccount =>
        s_config == null || (s_config.showFollowAccount && (IsMockPlatform || IsByteDancePlatform));

    /// <summary> 新用户首次进入时是否引导添加桌面快捷方式（抖音） </summary>
    public static bool ShouldGuideShortcut =>
        s_config == null || (s_config.shortcutGuideEnabled && (IsMockPlatform || IsByteDancePlatform));

    /// <summary>
    /// 游戏圈按钮配置：把配置里的右边距/顶部边距换算成「屏幕左上角原点、Y 轴向下」的 left/top。
    /// 返回副本，修改它不会影响配置资产。
    /// </summary>
    public static GameClubButtonOptions GameClubStyle
    {
        get
        {
            if (s_config == null)
            {
                return new GameClubButtonOptions();
            }

            var style  = s_config.gameClubStyle.Clone();
            var screen = PlatformServices.UtilityService.GetLogicScreenSize();
            style.left = Mathf.Max(0, (int)screen.x - s_config.gameClubRightOffset - style.width);
            style.top  = s_config.gameClubTopOffset;
            return style;
        }
    }
}
