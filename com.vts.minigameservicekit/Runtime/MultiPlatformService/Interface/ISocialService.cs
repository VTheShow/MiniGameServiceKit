using System;
using UnityEngine;
using VTS.MiniGameKit;

/// <summary>
/// 社交互动服务：分享、收藏、游戏圈
/// </summary>
public interface ISocialService
{
    /// <summary>
    /// 注入分享配置（由 CreateServices 调用）：自动设置默认转发素材、朋友圈素材与微信客服参数。
    /// 传入 null 时不做任何设置。
    /// </summary>
    void Configure(ShareConfigSO config);

    #region 分享

    /// <summary>
    /// 配置被动分享素材：设置后玩家通过右上角菜单转发/收藏时使用该内容（建议启动时调用一次）
    /// </summary>
    void SetupDefaultShare(string title, string imageUrl, string query = null);

    // 主动拉起分享面板（给好友/群）
    void Share(string title, string imageUrl, Action<bool> callback);

    // 配置「分享到朋友圈」的内容（微信仅支持被动分享：玩家通过右上角菜单触发，仅 Android）
    void ShareToTimeline(string title, string imageUrl, Action<bool> callback);

    #endregion

    #region 收藏 / 快捷方式（部分平台不支持，见各实现注释）

    // 检查是否已收藏
    bool IsFavorites { get; }
    // 配置「收藏」的内容（玩家通过右上角菜单收藏时使用）
    void AddToFavorites(string title, string imageUrl, Action<bool> callback);
    void Unfavorites(Action<bool> callback);
    // 显示收藏引导页
    void ShowFavoriteGuide(string content);

    // 检查快捷方式是否已经创建
    void CheckShortcut(Action<bool> callback);
    // 添加桌面快捷方式
    void AddShortcut(string title, string imageUrl, Action<bool> callback);

    #endregion

    #region 游戏圈

    /// <summary>
    /// 显示游戏圈悬浮按钮（需要微信后台开通「游戏圈」功能；开发者工具中该接口不可用，仅真机生效）
    /// </summary>
    void ShowGameClubButton(GameClubButtonOptions options);
    /// <summary> 隐藏游戏圈按钮（保留实例，可再次 Show） </summary>
    void HideGameClubButton();
    /// <summary> 销毁游戏圈按钮（下次 Show 会按传入配置重建） </summary>
    void DestroyGameClubButton();

    #endregion

    #region 平台触达入口（客服 / 关注官方账号）

    /// <summary>
    /// 拉起客服会话（微信：需在微信后台开通并绑定微信客服；抖音不支持）
    /// </summary>
    void OpenCustomerService();
    /// <summary>
    /// 拉起关注官方账号面板（抖音：关注抖音号，需在抖音后台绑定游戏官方抖音号；微信不支持直接回调 false）
    /// </summary>
    void FollowOfficialAccount(Action<bool> callback);
    /// <summary> 查询是否已关注官方账号（抖音支持；微信不支持恒回调 false） </summary>
    void IsFollowingOfficialAccount(Action<bool> callback);

    #endregion
}

/// <summary>
/// 游戏圈按钮配置。坐标为微信逻辑像素，原点在屏幕左上角，Y 轴向下。
/// </summary>
[Serializable]
public class GameClubButtonOptions
{
    public EGameClubButtonType  type = EGameClubButtonType.Image;
    [KitInspectorLabel("文字内容（type=Text 时生效）")]
    public string               text = "游戏圈";
    [KitInspectorLabel("图标配色")]
    public EGameClubIconColor   icon = EGameClubIconColor.Green;

    [KitInspectorLabel("左边距（逻辑像素）")]
    public int left = 20;
    [KitInspectorLabel("上边距（逻辑像素）")]
    public int top = 20;
    public int width = 40;
    public int height = 40;

    [KitInspectorLabel("背景色（type=Text 时生效）")]
    public string backgroundColor = "#FFFFFF";
    public string borderColor = "#000000";
    public int borderWidth;
    public int borderRadius = 8;
    [KitInspectorLabel("文字颜色（type=Text 时生效）")]
    public string color = "#000000";
    public EGameClubTextAlign textAlign = EGameClubTextAlign.Center;
    public int fontSize = 14;
    public int lineHeight = 16;

    [KitInspectorLabel("是否显示红点")]
    public bool showRedDot;

    /// <summary> 复制一份，避免修改配置资产本体 </summary>
    public GameClubButtonOptions Clone()
    {
        return new GameClubButtonOptions
        {
            type            = type,
            text            = text,
            icon            = icon,
            left            = left,
            top             = top,
            width           = width,
            height          = height,
            backgroundColor = backgroundColor,
            borderColor     = borderColor,
            borderWidth     = borderWidth,
            borderRadius    = borderRadius,
            color           = color,
            textAlign       = textAlign,
            fontSize        = fontSize,
            lineHeight      = lineHeight,
            showRedDot      = showRedDot,
        };
    }
}

public enum EGameClubButtonType
{
    [InspectorName("图片")] Image = 0,
    [InspectorName("文字")] Text = 1,
}

public enum EGameClubIconColor
{
    [InspectorName("绿色")] Green = 0,
    [InspectorName("白色")] White = 1,
    [InspectorName("深色")] Dark = 2,
    [InspectorName("浅色")] Light = 3,
}

public enum EGameClubTextAlign
{
    Left = 0,
    Center = 1,
    Right = 2,
}
