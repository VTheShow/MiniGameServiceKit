#if MINIGAME_SUBPLATFORM_DOUYIN

using System;
using System.Collections.Generic;
using UnityEngine;
using TTSDK;

/// <summary>
/// 抖音平台共用广告服务，按 StarkSDK 官方 API 实现：
///
/// - 激励视频 TTRewardedVideoAd：创建时自动加载，OnLoad 后可 Show；Show 失败走 OnError，
///   官方推荐失败后 Load 重试；OnClose(isEnded, count) 直接给出完整观看标志与 multiton 连看次数。
///   多例广告参数（MultitonRewardTimes/MultitonRewardMsg/ProgressTip）在创建时固化，
///   因此 multiton 配置变化时需要重建实例。
/// - 横幅 TTBannerAd：横幅有时效性会失效（IsInvalid），展示前检查失效并重建；
///   位置/尺寸变化时重建（TTBannerStyle 在创建时固化）。
/// - 插屏 TTInterstitialAd：必须先 Load 成功（IsLoaded）才能 Show；关闭后主动 Load 预热下一条。
///
/// 广告位 ID 统一来自 AdConfigSO（基类 Configure 注入），不再使用空字符串。
/// </summary>
public abstract class StarkSdkAdServiceBase : AdServiceBase
{
    // —— 激励视频 ——
    private TTRewardedVideoAd   m_videoAd;
    private bool                m_videoAdMultiton;
    private Action<bool, int>   m_rewardOnClose;
    private Action<string>      m_rewardOnError;
    private bool                m_rewardPending;
    private bool                m_autoShowOnLoad;   // Load 重试完成后自动 Show
    private int                 m_retryCount;
    private bool                m_videoAdLoaded;

    // —— 横幅 ——
    private TTBannerAd          m_bannerAd;
    private int                 m_bannerLeft = -1;
    private int                 m_bannerTop  = -1;
    private int                 m_bannerWidth = -1;
    private Action<string>      m_bannerOnError;
    private bool                m_bannerVisible;

    // —— 插屏 ——
    private TTInterstitialAd    m_interstitialAd;
    private Action<string>      m_interstitialOnError;
    private bool                m_interstitialPending;

    // 抖音使用 AdConfigSO 中的广告位配置
    protected override AdConfig.EAdPlatform AdPlatform => AdConfig.EAdPlatform.DouyinMiniGame;

    public override bool IsRewardedVideoReady => m_videoAdLoaded && !m_rewardPending;

    public override void Preload()
    {
        // 激励视频创建时即自动加载；插屏需手动 Load
        EnsureVideoAd(false);
        if (!string.IsNullOrEmpty(m_adConfig.interstitialAdUnitId))
        {
            EnsureInterstitial();
            m_interstitialAd.Load();
        }
    }

    #region 激励视频

    public override void ShowRewardedVideoAd(Action<bool, int> onClose, Action<string> onError = null, int multitonRewardTimes = 0, List<string> multitonRewardMsg = null)
    {
        if (string.IsNullOrEmpty(m_adConfig.videoAdUnitId))
        {
            Debug.LogWarning("[StarkAd] 激励视频广告位ID未配置");
            onError?.Invoke("激励视频广告位ID未配置");
            return;
        }
        if (m_rewardPending)
        {
            Debug.LogWarning("[StarkAd] 激励视频正在展示中，忽略本次请求");
            return;
        }

        bool multiton = multitonRewardTimes > 0;
        EnsureVideoAd(multiton, multitonRewardTimes, multitonRewardMsg);

        m_rewardOnClose    = onClose;
        m_rewardOnError    = onError;
        m_rewardPending    = true;
        m_retryCount       = 0;
        m_autoShowOnLoad   = true;

        m_videoAd.Show();
    }

    private void EnsureVideoAd(bool multiton, int multitonRewardTimes = 0, List<string> multitonRewardMsg = null)
    {
        if (m_videoAd != null && m_videoAdMultiton == multiton)
        {
            return;
        }
        m_videoAd?.Destroy();
        if (m_videoAd != null)
        {
            m_videoAd.OnLoad -= OnVideoLoad;
        }

        // 官方全参重载：关闭/错误回调在创建时注册，multiton 参数一并固化
        m_videoAd = TT.CreateRewardedVideoAd(
            m_adConfig.videoAdUnitId,
            OnVideoClosed,
            OnVideoError,
            multiton,
            multitonRewardMsg != null ? multitonRewardMsg.ToArray() : null,
            multitonRewardTimes,
            multiton && multitonRewardTimes > 0);
        m_videoAdMultiton = multiton;
        m_videoAdLoaded   = false;

        // OnLoad 事件驱动就绪状态与重试自动展示（多例/单例重建后都需重新订阅）
        m_videoAd.OnLoad += OnVideoLoad;
    }

    private void OnVideoLoad()
    {
        m_videoAdLoaded = true;
        // 创建后自动加载与 Load() 重试都会走到这里：仅在有待展示请求时自动 Show
        if (m_rewardPending && m_autoShowOnLoad)
        {
            m_autoShowOnLoad = false;
            m_videoAd.Show();
        }
    }

    private void OnVideoError(int code, string message)
    {
        m_videoAdLoaded = false;
        if (m_rewardPending && m_retryCount < 1)
        {
            // 官方推荐：展示失败（常见为未加载完成）→ Load 后由 OnLoad 重新 Show，最多重试一次
            m_retryCount++;
            m_autoShowOnLoad = true;
            m_videoAd.Load();
            return;
        }
        FailReward($"激励视频出错 code={code} msg={message}");
    }

    private void OnVideoClosed(bool isEnded, int count)
    {
        var onClose = m_rewardOnClose;
        ClearRewardPending();
        // 抖音的 OnClose 直接提供 isEnded 与 multiton 连看次数
        onClose?.Invoke(isEnded, count);
    }

    private void FailReward(string errMsg)
    {
        RaiseAdError(errMsg);
        var onError = m_rewardOnError;
        ClearRewardPending();
        onError?.Invoke(errMsg);
    }

    private void ClearRewardPending()
    {
        m_rewardPending   = false;
        m_rewardOnClose   = null;
        m_rewardOnError   = null;
        m_retryCount      = 0;
        m_autoShowOnLoad  = false;
    }

    #endregion

    #region 横幅

    /// <summary>
    /// TTBannerStyle 的高度由模板决定（约 width/3），只需给出宽度和位置；
    /// 预设位置的定位在安全区内进行（避开刘海/Home Indicator）
    /// </summary>
    public override void ShowBannerAd(EBannerAdPosition pos = EBannerAdPosition.BottomCenter, int width = 360, Action<string> onError = null)
    {
        var info = TT.GetSystemInfo();
        var sa = info.safeArea;

        // 抖音安全区为左上角原点（Y 向下）的边缘坐标，换算为四边内边距
        int safeLeft   = sa.left;
        int safeTop    = sa.top;
        int safeRight  = info.screenWidth - sa.right;
        int safeBottom = info.screenHeight - sa.bottom;

        var (left, top) = CalculateBannerAdLogicalPos(width, info.screenWidth, info.screenHeight, pos,
            safeLeft, safeTop, safeRight, safeBottom);
        ShowBannerAdInternal(left, top, width, onError);
    }

    public override void ShowBannerAdAt(int left, int top, int width = 360, Action<string> onError = null)
    {
        ShowBannerAdInternal(left, top, width, onError);
    }

    private void ShowBannerAdInternal(int left, int top, int width, Action<string> onError)
    {
        if (string.IsNullOrEmpty(m_adConfig.bannerAdUnitId))
        {
            Debug.LogWarning("[StarkAd] 横幅广告位ID未配置");
            onError?.Invoke("横幅广告位ID未配置");
            return;
        }

        m_bannerOnError = onError;
        m_bannerVisible = true;

        // 样式在创建时固化：位置/尺寸变化需重建；另外横幅有时效性，失效后也要重建
        bool needRecreate = m_bannerAd == null
                         || m_bannerAd.IsInvalid()
                         || m_bannerLeft != left
                         || m_bannerTop != top
                         || m_bannerWidth != width;
        if (needRecreate)
        {
            DestroyBannerAd();
            m_bannerLeft  = left;
            m_bannerTop   = top;
            m_bannerWidth = width;
            m_bannerAd = TT.CreateBannerAd(
                m_adConfig.bannerAdUnitId,
                new TTBannerStyle { left = left, top = top, width = width },
                30,
                OnBannerError,
                OnBannerLoaded,
                null,
                null);
        }
        m_bannerAd.Show();
    }

    public override void HideBannerAd()
    {
        m_bannerVisible = false;
        m_bannerAd?.Hide();
    }

    private void OnBannerLoaded()
    {
        if (m_bannerVisible)
        {
            m_bannerAd.Show();
        }
    }

    private void OnBannerError(int code, string message)
    {
        string msg = $"横幅广告出错 code={code} msg={message}";
        RaiseAdError(msg);
        var onError = m_bannerOnError;
        m_bannerOnError = null;
        onError?.Invoke(msg);
    }

    private void DestroyBannerAd()
    {
        if (m_bannerAd == null)
        {
            return;
        }
        m_bannerAd.OnLoad -= OnBannerLoaded;
        m_bannerAd.OnError -= OnBannerError;
        m_bannerAd.Destroy();
        m_bannerAd = null;
    }

    #endregion

    #region 插屏

    public override void ShowInterstitialAd(Action<string> onError)
    {
        if (string.IsNullOrEmpty(m_adConfig.interstitialAdUnitId))
        {
            Debug.LogWarning("[StarkAd] 插屏广告位ID未配置");
            onError?.Invoke("插屏广告位ID未配置");
            return;
        }

        EnsureInterstitial();
        m_interstitialOnError = onError;
        m_interstitialPending = true;

        // 官方语义：插屏必须 Load 成功后才能 Show
        if (m_interstitialAd.IsLoaded())
        {
            m_interstitialAd.Show();
        }
        else
        {
            m_interstitialAd.Load();
        }
    }

    private void EnsureInterstitial()
    {
        if (m_interstitialAd != null)
        {
            return;
        }
        m_interstitialAd = TT.CreateInterstitialAd(
            m_adConfig.interstitialAdUnitId,
            OnInterstitialError,
            OnInterstitialClosed,
            OnInterstitialLoaded);
    }

    private void OnInterstitialLoaded()
    {
        if (m_interstitialPending)
        {
            m_interstitialAd.Show();
        }
    }

    private void OnInterstitialClosed()
    {
        m_interstitialPending = false;
        // 插屏展示一次即被消耗，预热下一条
        m_interstitialAd?.Load();
    }

    private void OnInterstitialError(int code, string message)
    {
        string msg = $"插屏广告出错 code={code} msg={message}";
        RaiseAdError(msg);
        m_interstitialPending = false;
        var onError = m_interstitialOnError;
        m_interstitialOnError = null;
        onError?.Invoke(msg);
    }

    #endregion
}

#endif
