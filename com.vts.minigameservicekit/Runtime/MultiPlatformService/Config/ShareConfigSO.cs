using UnityEngine;
using VTS.MiniGameKit;

/// <summary>
/// 分享素材与客服配置：纯数据类，包不读取；游戏侧启动时读出参数调用
/// SetupDefaultShare / 客服相关接口注入平台。
/// 图片建议使用平台 CDN 可访问的 https 地址（微信要求 5:4 比例的分享图效果最佳）。
/// </summary>
[CreateAssetMenu(fileName = "ShareConfigSO", menuName = "Scriptable Object/ShareConfigSO")]
public class ShareConfigSO : ScriptableObject
{
    [KitInspectorLabel("转发标题")]
    public string shareTitle = string.Empty;

    [KitInspectorLabel("转发图片URL")]
    public string shareImageUrl = string.Empty;

    [KitInspectorLabel("转发携带query")]
    [Tooltip("如 from=friend，用于分享回流统计")]
    public string shareQuery = string.Empty;

    [KitInspectorLabel("朋友圈标题(仅微信)")]
    public string timelineTitle = string.Empty;

    [KitInspectorLabel("朋友圈图片URL(仅微信)")]
    public string timelineImageUrl = string.Empty;

    [KitInspectorLabel("微信客服企业ID")]
    [Tooltip("微信客服开通后获得（corpId），留空则不显示客服入口")]
    public string customerServiceCorpId = string.Empty;

    [KitInspectorLabel("微信客服链接")]
    [Tooltip("微信客服链接（extInfo.url），与 corpId 配套")]
    public string customerServiceUrl = string.Empty;
}
