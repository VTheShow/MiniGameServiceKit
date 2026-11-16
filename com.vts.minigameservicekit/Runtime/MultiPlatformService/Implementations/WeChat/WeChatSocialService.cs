#if MINIGAME_SUBPLATFORM_WEIXIN

using System;
using UnityEngine;
using WeChatWASM;

/// <summary>
/// 微信社交服务：
/// - 被动分享：SetupDefaultShare 通过 WX.OnShareAppMessage 设置右上角菜单转发的默认素材（启动时调用一次）
/// - 主动分享：WX.ShareAppMessage（微信不提供"分享是否成功"的结果，callback 恒为 true）
/// - 朋友圈：仅支持被动分享（右上角菜单，且目前仅 Android），主动拉起不支持
/// - 游戏圈：WX.CreateGameClubButton（需在微信后台开通游戏圈；开发者工具不支持，仅真机生效）
/// - 收藏/快捷方式：收藏内容可配置；快捷方式微信小游戏不支持
/// </summary>
public class WeChatSocialService : ISocialService
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
        if (!string.IsNullOrEmpty(config.customerServiceCorpId))
        {
            SetCustomerServiceConfig(config.customerServiceCorpId, config.customerServiceUrl);
        }
    }

    #region 分享

    public void SetupDefaultShare(string title, string imageUrl, string query = null)
    {
        if (m_defaultShareSetup)
        {
            // SDK 的 OnShareAppMessage 为覆盖式注册，重复调用无意义
            return;
        }
        var param = new WXShareAppMessageParam
        {
            title = title,
            imageUrl = imageUrl,
            query = query ?? string.Empty,
        };
        WX.OnShareAppMessage(param);
        m_defaultShareSetup = true;
        Debug.Log("[WeChatSocial] 已配置默认被动分享素材: " + title);
    }

    public void Share(string title, string imageUrl, Action<bool> callback)
    {
        var option = new ShareAppMessageOption
        {
            title = title,
            imageUrl = imageUrl,
        };
        WX.ShareAppMessage(option);
        // 微信分享面板拉起后不回调结果，视为已触发
        callback?.Invoke(true);
    }

    public void ShareToTimeline(string title, string imageUrl, Action<bool> callback)
    {
        // 朋友圈无法主动拉起，这里注册/更新玩家通过右上角菜单分享到朋友圈时的内容
        WX.OnShareTimeline((setParam) =>
        {
            setParam(new OnShareTimelineListenerResult
            {
                title = title,
                imageUrl = imageUrl,
            });
        });
        Debug.Log("[WeChatSocial] 已配置朋友圈分享内容（仅支持右上角菜单触发，目前仅 Android 生效）");
        callback?.Invoke(true);
    }

    #endregion

    #region 收藏 / 快捷方式

    public bool IsFavorites => false; // 微信小游戏无法查询收藏状态

    public void AddToFavorites(string title, string imageUrl, Action<bool> callback)
    {
        // 配置玩家通过右上角菜单「收藏」时的内容
        WX.OnAddToFavorites((setParam) =>
        {
            setParam(new OnAddToFavoritesListenerResult
            {
                title = title,
                imageUrl = imageUrl,
            });
        });
        callback?.Invoke(true);
    }

    public void Unfavorites(Action<bool> callback)
    {
        Debug.LogWarning("[WeChatSocial] 微信小游戏不支持取消收藏");
        callback?.Invoke(false);
    }

    public void ShowFavoriteGuide(string content)
    {
        // 收藏引导（showFavoriteGuide）为抖音平台能力，微信不支持
        Debug.LogWarning("[WeChatSocial] 微信小游戏不支持收藏引导: " + content);
    }

    public void CheckShortcut(Action<bool> callback)
    {
        Debug.LogWarning("[WeChatSocial] 微信小游戏不支持桌面快捷方式");
        callback?.Invoke(false);
    }

    public void AddShortcut(string title, string imageUrl, Action<bool> callback)
    {
        Debug.LogWarning("[WeChatSocial] 微信小游戏不支持桌面快捷方式");
        callback?.Invoke(false);
    }

    #endregion

    #region 游戏圈

    private WXGameClubButton m_gameClubButton;

    public void ShowGameClubButton(GameClubButtonOptions options)
    {
        if (!PlatformEntry.ShowGameClub)
        {
            Debug.Log("[WeChatSocial] 游戏圈入口已在 PlatformEntryConfigSO 中关闭，跳过");
            return;
        }

        // 每次按最新配置重建，避免维护多套样式更新逻辑
        DestroyGameClubButton();

        try
        {
            var style = new GameClubButtonStyle
            {
                left = options.left,
                top = options.top,
                width = options.width,
                height = options.height,
                backgroundColor = options.backgroundColor,
                borderColor = options.borderColor,
                borderWidth = options.borderWidth,
                borderRadius = options.borderRadius,
                color = options.color,
                textAlign = (GameClubButtonTextAlign)(int)options.textAlign,
                fontSize = options.fontSize,
                lineHeight = options.lineHeight,
            };
            var param = new WXCreateGameClubButtonParam
            {
                type = options.type == EGameClubButtonType.Image ? GameClubButtonType.image : GameClubButtonType.text,
                text = options.text,
                icon = (GameClubButtonIcon)(int)options.icon,
                style = style,
                hasRedDot = options.showRedDot ? true : (bool?)null,
            };

            m_gameClubButton = WX.CreateGameClubButton(param);
            m_gameClubButton.Show();
            Debug.Log("[WeChatSocial] 游戏圈按钮已显示");
        }
        catch (Exception e)
        {
            // 开发者工具中 createGameClubButton 必然失败；未开通游戏圈功能时真机也会失败
            Debug.LogWarning("[WeChatSocial] 创建游戏圈按钮失败（开发者工具不支持该接口，请真机测试；并确认微信后台已开通游戏圈）: " + e.Message);
        }
    }

    public void HideGameClubButton()
    {
        m_gameClubButton?.Hide();
    }

    public void DestroyGameClubButton()
    {
        if (m_gameClubButton == null)
        {
            return;
        }
        m_gameClubButton.Destroy();
        m_gameClubButton = null;
    }

    #endregion

    #region 客服（微信客服，需后台开通并绑定）

    // 微信客服参数：corpId/url 由游戏侧启动流程从 ShareConfigSO 读出后注入
    private static string s_csCorpId;
    private static string s_csUrl;

    /// <summary> 注入微信客服参数（企业ID与客服链接，微信后台「微信客服」页面获取） </summary>
    public static void SetCustomerServiceConfig(string corpId, string url)
    {
        s_csCorpId = corpId;
        s_csUrl    = url;
    }

    public void OpenCustomerService()
    {
        if (!PlatformEntry.ShowCustomerService)
        {
            Debug.Log("[WeChatSocial] 客服入口已在 PlatformEntryConfigSO 中关闭，跳过");
            return;
        }
        if (string.IsNullOrEmpty(s_csCorpId) || string.IsNullOrEmpty(s_csUrl))
        {
            Debug.LogWarning("[WeChatSocial] 微信客服参数未配置（WeChatSocialService.SetCustomerServiceConfig），无法拉起客服");
            return;
        }
        WX.OpenCustomerServiceChat(new OpenCustomerServiceChatOption
        {
            corpId  = s_csCorpId,
            extInfo = new ExtInfoOption { url = s_csUrl },
            success = (res) => Debug.Log("[WeChatSocial] 已拉起微信客服"),
            fail = (err) => Debug.LogWarning("[WeChatSocial] 拉起微信客服失败: " + err.errMsg),
        });
    }

    public void FollowOfficialAccount(Action<bool> callback)
    {
        Debug.LogWarning("[WeChatSocial] 微信小游戏不支持拉起关注公众号");
        callback?.Invoke(false);
    }

    public void IsFollowingOfficialAccount(Action<bool> callback)
    {
        Debug.LogWarning("[WeChatSocial] 微信小游戏不支持查询公众号关注状态");
        callback?.Invoke(false);
    }

    #endregion
}

#endif
