using System;
using UnityEngine;

public class MockSocialService : ISocialService
{
    public void Configure(ShareConfigSO config)
    {
        if (config == null)
        {
            return;
        }
        if (!string.IsNullOrEmpty(config.shareTitle) && !string.IsNullOrEmpty(config.shareImageUrl))
        {
            SetupDefaultShare(config.shareTitle, config.shareImageUrl, config.shareQuery);
        }
        if (!string.IsNullOrEmpty(config.timelineTitle) && !string.IsNullOrEmpty(config.timelineImageUrl))
        {
            ShareToTimeline(config.timelineTitle, config.timelineImageUrl, null);
        }
        Debug.Log("[Mock] 已注入 ShareConfigSO");
    }

    public void SetupDefaultShare(string title, string imageUrl, string query = null)
    {
        Debug.Log($"[Mock] 配置默认被动分享素材: {title}, {imageUrl}, query={query}");
    }

    public void Share(string title, string imageUrl, Action<bool> callback)
    {
        Debug.Log($"[Mock] 模拟主动分享: {title}, {imageUrl}");
        callback?.Invoke(true);
    }

    public void ShareToTimeline(string title, string imageUrl, Action<bool> callback)
    {
        Debug.Log($"[Mock] 模拟配置朋友圈分享内容: {title}, {imageUrl}");
        callback?.Invoke(true);
    }

    public bool IsFavorites => false;

    public void AddToFavorites(string title, string imageUrl, Action<bool> callback)
    {
        Debug.Log($"[Mock] 模拟配置收藏内容: {title}, {imageUrl}");
        callback?.Invoke(true);
    }

    public void Unfavorites(Action<bool> callback)
    {
        Debug.Log("[Mock] 模拟取消收藏");
        callback?.Invoke(true);
    }

    public void ShowFavoriteGuide(string content)
    {
        Debug.Log("[Mock] 模拟显示收藏引导: " + content);
    }

    public void CheckShortcut(Action<bool> callback)
    {
        Debug.Log("[Mock] 模拟检查快捷方式: 未创建");
        callback?.Invoke(false);
    }

    public void AddShortcut(string title, string imageUrl, Action<bool> callback)
    {
        Debug.Log($"[Mock] 模拟添加快捷方式: {title}");
        callback?.Invoke(true);
    }

    public void ShowGameClubButton(GameClubButtonOptions options)
    {
        if (!PlatformEntry.ShowGameClub)
        {
            Debug.Log("[Mock] 游戏圈入口已在 PlatformEntryConfigSO 中关闭，跳过");
            return;
        }
        Debug.Log($"[Mock] 模拟显示游戏圈按钮: type={options.type}, pos=({options.left},{options.top}), size={options.width}x{options.height}");
    }

    public void HideGameClubButton()
    {
        Debug.Log("[Mock] 模拟隐藏游戏圈按钮");
    }

    public void DestroyGameClubButton()
    {
        Debug.Log("[Mock] 模拟销毁游戏圈按钮");
    }

    #region 平台触达入口

    public void OpenCustomerService()
    {
        if (!PlatformEntry.ShowCustomerService)
        {
            Debug.Log("[Mock] 客服入口已在 PlatformEntryConfigSO 中关闭，跳过");
            return;
        }
        Debug.Log("[Mock] 模拟拉起客服会话");
    }

    public void FollowOfficialAccount(Action<bool> callback)
    {
        if (!PlatformEntry.ShowFollowAccount)
        {
            Debug.Log("[Mock] 关注官方号入口已在 PlatformEntryConfigSO 中关闭，跳过");
            callback?.Invoke(false);
            return;
        }
        Debug.Log("[Mock] 模拟拉起关注官方账号");
        callback?.Invoke(true);
    }

    public void IsFollowingOfficialAccount(Action<bool> callback)
    {
        Debug.Log("[Mock] 模拟查询关注状态：未关注");
        callback?.Invoke(false);
    }

    #endregion
}
