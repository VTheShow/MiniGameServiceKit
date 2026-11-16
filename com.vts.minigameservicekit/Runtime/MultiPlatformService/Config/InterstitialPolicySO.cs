using UnityEngine;
using VTS.MiniGameKit;

/// <summary>
/// 插屏广告自动展示策略：关卡结算回主页时按频控自动展示插屏。
/// 纯数据类，包不读取；频控判定由游戏侧按这些参数实现（去广告特权等豁免逻辑也在游戏侧）。
/// </summary>
[CreateAssetMenu(fileName = "InterstitialPolicySO", menuName = "Scriptable Object/InterstitialPolicySO")]
public class InterstitialPolicySO : ScriptableObject
{
    [KitInspectorLabel("启用自动插屏")]
    public bool enable = true;

    [KitInspectorLabel("免打扰关卡数")]
    [Tooltip("新手保护：前 N 关结算不弹插屏")]
    public int firstNoAdLevels = 3;

    [KitInspectorLabel("间隔关卡数")]
    [Tooltip("两次插屏之间至少间隔的通关数")]
    public int intervalLevels = 2;

    [KitInspectorLabel("间隔秒数")]
    [Tooltip("两次插屏之间至少间隔的秒数")]
    public float intervalSeconds = 180f;
}
