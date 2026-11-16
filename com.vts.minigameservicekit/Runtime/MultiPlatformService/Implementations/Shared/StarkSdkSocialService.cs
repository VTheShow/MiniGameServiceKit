#if MINIGAME_SUBPLATFORM_DOUYIN

using System;
using UnityEngine;
using TTSDK.UNBridgeLib.LitJson;
using TTSDK;

/// <summary>
/// 抖音平台共用社交服务：
/// - 分享：TT.ShareAppMessage（主动）/ TT.OnShareAppMessage（被动素材配置）
/// - 收藏/快捷方式/复访引导：TT 原生支持
/// - 游戏圈：微信特有能力，字节平台不支持（no-op）
/// </summary>
public abstract class StarkSdkSocialServiceBase : ISocialService
{
    private bool m_defaultShareSetup;

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
    }

    #region 分享

    public void SetupDefaultShare(string title, string imageUrl, string query = null)
    {
        if (m_defaultShareSetup)
        {
            return;
        }
        TT.OnShareAppMessage((shareOption) =>
        {
            var shareJson = new JsonData();
            shareJson["title"] = title;
            shareJson["imageUrl"] = imageUrl;
            return new TTShare.ShareParam(shareJson, null, null, null);
        });
        m_defaultShareSetup = true;
        Debug.Log("[StarkSocial] 已配置默认被动分享素材: " + title);
    }

    public void Share(string title, string imageUrl, Action<bool> callback)
    {
        var shareJson = new JsonData();
        shareJson["title"] = title;
        shareJson["imageUrl"] = imageUrl;
        TT.ShareAppMessage(
            shareJson,
            successCallback: (data) => callback?.Invoke(true),
            failedCallback: (errMsg) =>
            {
                Debug.LogWarning("[StarkSocial] 分享失败: " + errMsg);
                callback?.Invoke(false);
            },
            cancelledCallback: () => callback?.Invoke(false));
    }

    public void ShareToTimeline(string title, string imageUrl, Action<bool> callback)
    {
        // 字节平台没有「朋友圈」体裁（有「日常」分享，尚未接入）
        Debug.LogWarning("[StarkSocial] 抖音平台不支持分享到朋友圈");
        callback?.Invoke(false);
    }

    #endregion

    #region 收藏 / 快捷方式

    public bool IsFavorites => TT.InContainerEnv && TT.IsCollected();

    public void AddToFavorites(string title, string imageUrl, Action<bool> callback)
    {
        TT.Collect(callback);
    }

    public void Unfavorites(Action<bool> callback)
    {
        TT.CancelCollection(callback);
    }

    public void ShowFavoriteGuide(string content)
    {
        TT.ShowFavoriteGuide(TTFavorite.Style.BottomBar, content);
    }

    /// <summary>
    /// 显示复访引导（收藏相关，但独立）
    /// </summary>
    public void ShowRevisitGuide(Action<bool> onComplete = null)
    {
        TT.ShowRevisitGuide((isSuccess) => onComplete?.Invoke(isSuccess));
    }

    public void CheckShortcut(Action<bool> callback)
    {
        TT.CheckShortcut(callback);
    }

    public void AddShortcut(string title, string imageUrl, Action<bool> callback)
    {
        TT.AddShortcut(callback);
    }

    #endregion

    #region 游戏圈（微信特有，字节平台不支持）

    public void ShowGameClubButton(GameClubButtonOptions options)
    {
        Debug.LogWarning("[StarkSocial] 游戏圈为微信小游戏特有能力，抖音平台不支持");
    }

    public void HideGameClubButton() { }

    public void DestroyGameClubButton() { }

    #endregion

    #region 客服 / 关注官方账号（抖音号）

    public void OpenCustomerService()
    {
        // 字节平台无通用客服会话接口。6.9.5 的 TT.OpenAwemeCustomerService 语义是
        // "通过客服页面发起钻石支付"（iOS 虚拟支付受限时的替代下单通道），属支付场景而非通用客服，
        // 故不接入本接口；iOS 支付受限引导将来在支付流程中按需接入
        Debug.LogWarning("[StarkSocial] 抖音平台无通用客服接口，建议引导关注官方抖音号（FollowOfficialAccount）");
    }

    public void FollowOfficialAccount(Action<bool> callback)
    {
        if (!PlatformEntry.ShowFollowAccount)
        {
            Debug.Log("[StarkSocial] 关注官方号入口已在 PlatformEntryConfigSO 中关闭，跳过");
            callback?.Invoke(false);
            return;
        }

        // StarkSDK 6.9.5：TT.FollowDouYinUserProfile 已移除，改为 TT.OpenAwemeUserProfile
        // 需在抖音开发者平台-设置中绑定游戏官方抖音号
        TT.OpenAwemeUserProfile(
            () =>
            {
                Debug.Log("[StarkSocial] 已完成打开抖音号流程（是否关注成功由用户操作决定，无法查询本次结果）");
                callback?.Invoke(true);
            },
            (code, errMsg) =>
            {
                Debug.LogWarning($"[StarkSocial] 打开抖音号失败 code={code} msg={errMsg}");
                callback?.Invoke(false);
            },
            true);
    }

    public void IsFollowingOfficialAccount(Action<bool> callback)
    {
        // StarkSDK 6.9.5：TT.IsFollowDouyin 已移除，改为 TT.CheckFollowAwemeState
        TT.CheckFollowAwemeState(callback);
    }

    #endregion
}

#endif
