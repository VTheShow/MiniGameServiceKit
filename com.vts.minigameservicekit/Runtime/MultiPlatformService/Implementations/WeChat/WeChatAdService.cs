#if MINIGAME_SUBPLATFORM_WEIXIN

using System;
using System.Collections.Generic;
using UnityEngine;
using WeChatWASM;

/// <summary>
/// 微信广告服务（按官方 API 语义实现，参考 developers.weixin.qq.com 文档）：
///
/// - 激励视频 RewardedVideoAd：全局单例组件，show()/load() 均可能失败（Promise）。
///   官方推荐时序：直接 show()，失败（常见为未加载完成）时 load() 后再 show() 一次；
///   而不是每次展示前都先 load()（无谓等待，且 onLoad 与 load 回调会重复触发 show）。
/// - 插屏 InterstitialAd：广告展示一次即被消耗，必须重新 load() 才能再次 show()。
///   因此每次展示前 load()；onClose 后主动 load() 预热下一条。
/// - 横幅：基础库 3.5.5 起 BannerAd 停止维护，使用原生模板广告 CustomAd。
///   官方未承诺创建后修改 style 会生效，因此位置/尺寸变化时销毁重建，同参数时复用。
///
/// 错误统一 Debug.LogWarning 并回调 onError；广告对象缓存复用。
/// </summary>
public class WeChatAdService : AdServiceBase
{
    // —— 激励视频 ——
    private WXRewardedVideoAd   m_videoAd;
    private bool                m_videoAdMultiton;
    private Action<bool, int>   m_rewardOnClose;
    private Action<string>      m_rewardOnError;
    private bool                m_rewardPending;    // 一次展示流程（show→close）进行中
    private int                 m_rewardViewedCount;// multiton 模式下累计完整观看次数
    private bool                m_videoAdLoaded;    // OnLoad/OnError 维护的就绪状态

    // —— 原生模板广告（横幅位）——
    private WXCustomAd          m_customAd;
    private int                 m_customAdLeft = -1;
    private int                 m_customAdTop  = -1;
    private int                 m_customAdWidth = -1;
    private Action<string>      m_bannerOnError;
    private bool                m_bannerVisible;    // OnLoad 自动刷新时仅在可见状态下重新 Show

    // —— 插屏 ——
    private WXInterstitialAd    m_interstitialAd;
    private Action<string>      m_interstitialOnError;

    protected override AdConfig.EAdPlatform AdPlatform => AdConfig.EAdPlatform.WechatMiniGame;

    public override bool IsRewardedVideoReady => m_videoAdLoaded && !m_rewardPending;

    public override void Preload()
    {
        if (!string.IsNullOrEmpty(m_adConfig.videoAdUnitId))
        {
            EnsureVideoAd(false);
            m_videoAd.Load();
        }
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
            Debug.LogWarning("[WeChatAd] 激励视频广告位ID未配置");
            onError?.Invoke("激励视频广告位ID未配置");
            return;
        }
        if (m_rewardPending && !m_videoAdMultiton)
        {
            // 单例模式：一次只允许一个展示流程，防连点双发奖
            Debug.LogWarning("[WeChatAd] 激励视频正在展示中，忽略本次请求");
            return;
        }

        // multiton（多例广告，基础库 2.8.0+）：一次拉起可连续观看多个广告，每个广告关闭都会回调 onClose；
        // "再看一个得 XX"的奖励文案在微信后台广告位配置，SDK 不提供该参数
        bool multiton = multitonRewardTimes > 0;
        EnsureVideoAd(multiton);

        m_rewardOnClose     = onClose;
        m_rewardOnError     = onError;
        m_rewardPending     = true;
        m_rewardViewedCount = 0;

        // 官方推荐：先直接 show；未加载完成等失败时再 load + show（见 RetryShowVideoAfterLoad）
        ShowVideo();
    }

    private void EnsureVideoAd(bool multiton)
    {
        if (m_videoAd != null && m_videoAdMultiton == multiton)
        {
            return;
        }
        if (m_videoAd != null)
        {
            m_videoAd.OffError(OnVideoAdError);
            m_videoAd.OffClose(OnVideoAdClosed);
            m_videoAd.OffLoad(OnVideoAdLoaded);
            m_videoAd.Destroy();
        }

        var param = new WXCreateRewardedVideoAdParam
        {
            adUnitId = m_adConfig.videoAdUnitId,
            multiton = multiton,
        };
        m_videoAd         = WX.CreateRewardedVideoAd(param);
        m_videoAdMultiton = multiton;
        m_videoAdLoaded   = false;

        // 事件监听只在创建时注册一次（on* 为追加式注册，重复注册会导致回调多次触发），
        // 通过成员状态路由到当次请求
        m_videoAd.OnError(OnVideoAdError);
        m_videoAd.OnClose(OnVideoAdClosed);
        m_videoAd.OnLoad(OnVideoAdLoaded);
    }

    private void OnVideoAdLoaded(WXADLoadResponse res)
    {
        m_videoAdLoaded = true;
    }

    private void ShowVideo()
    {
        m_videoAd.Show(OnVideoShowSuccess, OnVideoShowFailed);
    }

    private void OnVideoShowSuccess(WXTextResponse res) { }

    private void OnVideoShowFailed(WXTextResponse res)
    {
        // 官方推荐模式：show 失败（常见原因是广告未加载完成）→ load 后重试一次
        m_videoAd.Load(OnVideoRetryLoadSuccess, OnVideoLoadFailed);
    }

    private void OnVideoRetryLoadSuccess(WXTextResponse res)
    {
        m_videoAd.Show(OnVideoShowSuccess, OnVideoRetryShowFailed);
    }

    private void OnVideoRetryShowFailed(WXTextResponse res)
    {
        FailReward(BuildErrMsg("展示激励视频失败(重试)", res));
    }

    private void OnVideoLoadFailed(WXADErrorResponse res)
    {
        m_videoAdLoaded = false;
        FailReward(BuildErrMsg("拉取激励视频失败", res));
    }

    private void OnVideoAdError(WXADErrorResponse res)
    {
        // 播放过程中的错误（素材失效、网络中断等）
        m_videoAdLoaded = false;
        FailReward(BuildErrMsg("激励视频播放出错", res));
    }

    private void OnVideoAdClosed(WXRewardedVideoAdOnCloseResponse res)
    {
        if (res.isEnded)
        {
            m_rewardViewedCount++;
        }

        if (m_videoAdMultiton)
        {
            // 多例模式：每个广告关闭都回调一次；玩家中途退出（isEnded=false）时结束整次流程。
            // 注意：连续观看的中途结算不会清除 pending，后续自动播放的广告关闭仍会走到这里；
            // 下一次 ShowRewardedVideoAd 请求会覆盖回调，不会永久卡死。
            if (res.isEnded)
            {
                m_rewardOnClose?.Invoke(true, m_rewardViewedCount);
                return;
            }
        }

        var onClose = m_rewardOnClose;
        int viewed  = m_rewardViewedCount;
        ClearRewardPending();

        // 官方行为：onClose 触发后广告被消耗，主动 load 预热下一次展示
        m_videoAd.Load();

        onClose?.Invoke(viewed > 0, viewed);
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
        m_rewardPending     = false;
        m_rewardOnClose     = null;
        m_rewardOnError     = null;
        m_rewardViewedCount = 0;
    }

    #endregion

    #region 横幅（原生模板广告）

    /// <summary>
    /// CustomAd 的宽高由模板决定（约 width/3），只需给出宽度和位置；
    /// 预设位置的定位在安全区内进行（避开刘海/Home Indicator）
    /// </summary>
    public override void ShowBannerAd(EBannerAdPosition pos = EBannerAdPosition.BottomCenter, int width = 360, Action<string> onError = null)
    {
        var info = WX.GetSystemInfoSync();
        var sa = WX.GetWindowInfo().safeArea;

        // 微信安全区为左上角原点（Y 向下）的边缘坐标，换算为四边内边距
        int safeLeft   = (int)sa.left;
        int safeTop    = (int)sa.top;
        int safeRight  = (int)(info.windowWidth - sa.right);
        int safeBottom = (int)(info.windowHeight - sa.bottom);

        var (left, top) = CalculateBannerAdLogicalPos(width, (float)info.windowWidth, (float)info.windowHeight, pos,
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
            Debug.LogWarning("[WeChatAd] 横幅(原生模板)广告位ID未配置");
            onError?.Invoke("横幅广告位ID未配置");
            return;
        }

        m_bannerOnError = onError;
        m_bannerVisible = true;

        // 官方未承诺 CustomAd 创建后修改 style 生效：参数变化时销毁重建，参数不变时复用
        bool needRecreate = m_customAd == null
                         || m_customAdLeft != left
                         || m_customAdTop != top
                         || m_customAdWidth != width;
        if (needRecreate)
        {
            DestroyBannerAd();
            var param = new WXCreateCustomAdParam
            {
                adUnitId = m_adConfig.bannerAdUnitId,
                adIntervals = 30,
                style = new CustomStyle
                {
                    left = left,
                    top = top,
                    width = width,
                },
            };
            m_customAd       = WX.CreateCustomAd(param);
            m_customAdLeft   = left;
            m_customAdTop    = top;
            m_customAdWidth  = width;
            m_customAd.OnLoad(OnBannerLoaded);
            m_customAd.OnError(OnBannerError);
        }
        m_customAd.Show();
    }

    public override void HideBannerAd()
    {
        m_bannerVisible = false;
        m_customAd?.Hide();
    }

    private void OnBannerLoaded(WXADLoadResponse res)
    {
        // 自动刷新会反复触发 OnLoad：只在横幅应可见时重新 Show，避免 Hide 后被自动拉起
        if (m_bannerVisible)
        {
            m_customAd.Show();
        }
    }

    private void OnBannerError(WXADErrorResponse res)
    {
        string msg = BuildErrMsg("横幅(原生模板)广告出错", res);
        RaiseAdError(msg);
        var onError = m_bannerOnError;
        m_bannerOnError = null;
        onError?.Invoke(msg);
    }

    private void DestroyBannerAd()
    {
        if (m_customAd == null)
        {
            return;
        }
        m_customAd.OffLoad(OnBannerLoaded);
        m_customAd.OffError(OnBannerError);
        m_customAd.Destroy();
        m_customAd = null;
    }

    #endregion

    #region 插屏

    public override void ShowInterstitialAd(Action<string> onError)
    {
        if (string.IsNullOrEmpty(m_adConfig.interstitialAdUnitId))
        {
            Debug.LogWarning("[WeChatAd] 插屏广告位ID未配置");
            onError?.Invoke("插屏广告位ID未配置");
            return;
        }

        EnsureInterstitial();
        m_interstitialOnError = onError;

        // 插屏展示一次即被消耗：每次展示前重新 load
        m_interstitialAd.Load(OnInterstitialLoadSuccess, OnInterstitialLoadFailed);
    }

    private void EnsureInterstitial()
    {
        if (m_interstitialAd != null)
        {
            return;
        }
        var param = new WXCreateInterstitialAdParam
        {
            adUnitId = m_adConfig.interstitialAdUnitId,
        };
        m_interstitialAd = WX.CreateInterstitialAd(param);
        m_interstitialAd.OnError(OnInterstitialAdError);
        m_interstitialAd.OnClose(OnInterstitialClosed);
    }

    private void OnInterstitialLoadSuccess(WXTextResponse res)
    {
        m_interstitialAd.Show(OnInterstitialShowSuccess, OnInterstitialShowFailed);
    }

    private void OnInterstitialLoadFailed(WXADErrorResponse res)
    {
        FailInterstitial(BuildErrMsg("拉取插屏广告失败", res));
    }

    private void OnInterstitialShowSuccess(WXTextResponse res) { }

    private void OnInterstitialShowFailed(WXTextResponse res)
    {
        FailInterstitial(BuildErrMsg("展示插屏广告失败", res));
    }

    private void OnInterstitialAdError(WXADErrorResponse res)
    {
        FailInterstitial(BuildErrMsg("插屏广告出错", res));
    }

    private void OnInterstitialClosed()
    {
        // 官方语义：插屏关闭后不能直接再次展示，主动 load 预热下一条
        m_interstitialAd?.Load();
    }

    private void FailInterstitial(string errMsg)
    {
        RaiseAdError(errMsg);
        var onError = m_interstitialOnError;
        m_interstitialOnError = null;
        onError?.Invoke(errMsg);
    }

    #endregion

    private static string BuildErrMsg(string prefix, WXADErrorResponse res)
    {
        return $"{prefix} errCode={res.errCode} errMsg={res.errMsg}";
    }

    private static string BuildErrMsg(string prefix, WXTextResponse res)
    {
        return $"{prefix} errCode={res.errCode} errMsg={res.errMsg}";
    }
}

#endif
