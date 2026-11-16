using UnityEngine;
using VTS.MiniGameKit;

/// <summary>
/// 平台悬浮入口配置：控制各平台原生入口（游戏圈）与平台差异入口（客服/关注）的显隐。
/// 商店等全平台通用入口由各游戏自己放在菜单 prefab 里，不在本配置范围。
/// 仅在对应平台生效（如游戏圈仅微信显示），配置驱动、无需改代码。
/// </summary>
[CreateAssetMenu(fileName = "PlatformEntryConfigSO", menuName = "Scriptable Object/PlatformEntryConfigSO")]
public class PlatformEntryConfigSO : ScriptableObject
{
    [KitInspectorLabel("显示游戏圈(微信)")]
    [Tooltip("需要微信后台开通「游戏圈」；仅真机生效")]
    public bool showGameClub = true;

    [KitInspectorLabel("游戏圈按钮样式")]
    public GameClubButtonOptions gameClubStyle = new();

    [KitInspectorLabel("游戏圈按钮右边距(逻辑像素)")]
    [Tooltip("从屏幕右上角向左的偏移；需避开微信右上角胶囊按钮（建议 ≥ 96）")]
    public int gameClubRightOffset = 100;

    [KitInspectorLabel("游戏圈按钮顶部边距(逻辑像素)")]
    [Tooltip("从屏幕顶部向下的偏移；需避开微信右上角胶囊按钮（建议 ≥ 150）")]
    public int gameClubTopOffset = 170;

    [KitInspectorLabel("显示客服入口(微信)")]
    [Tooltip("需在 ShareConfigSO 配置微信客服参数")]
    public bool showCustomerService = false;

    [KitInspectorLabel("显示关注抖音号入口(抖音)")]
    [Tooltip("需在抖音后台绑定官方抖音号")]
    public bool showFollowAccount = true;

    [KitInspectorLabel("新用户引导添加桌面快捷方式(抖音)")]
    public bool shortcutGuideEnabled = true;
}
