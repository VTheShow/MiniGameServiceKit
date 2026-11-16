using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 广告服务：视频激励广告、Banner(横幅)广告、Interstitial(插屏)广告、原生模板广告
/// </summary>
public abstract class AdServiceBase
{
    // 原生模板广告/横幅的高度由平台模板决定且不可设置，实测宽高比约 3:1，按比例估算用于垂直方向定位
    private const float BannerEstimatedHeightRatio = 1f / 3f;

    protected AdConfig m_adConfig;
    protected abstract AdConfig.EAdPlatform AdPlatform { get; }

    /// <summary>
    /// 注入广告位配置（服务创建后由启动流程调用一次）：按平台取出对应广告位 ID 并校验非空。
    /// 与 CreateServices（创建服务实例）是两个阶段：先创建、后 Configure。
    /// </summary>
    public void Configure(AdConfigSO adConfig)
    {
        m_adConfig = adConfig.GetAdConfig(AdPlatform);

        if (string.IsNullOrEmpty(m_adConfig.videoAdUnitId))
        {
            Debug.LogWarning($"[{AdPlatform}] 激励视频广告位ID未配置，激励视频将不可用（请在 AdConfigSO 中填写）");
        }
        if (string.IsNullOrEmpty(m_adConfig.bannerAdUnitId))
        {
            Debug.LogWarning($"[{AdPlatform}] 横幅(原生模板)广告位ID未配置，Banner将不可用（请在 AdConfigSO 中填写）");
        }
        if (string.IsNullOrEmpty(m_adConfig.interstitialAdUnitId))
        {
            Debug.LogWarning($"[{AdPlatform}] 插屏广告位ID未配置，插屏广告将不可用（请在 AdConfigSO 中填写）");
        }
    }

    /// <summary>
    /// 预加载广告（Configure 之后调用一次，提前拉取激励视频/插屏素材，缩短首次展示等待）
    /// </summary>
    public virtual void Preload() { }

    /// <summary>
    /// 激励视频是否已加载就绪（用于「看广告」按钮置灰/提示判断；Mock 恒为 true）
    /// </summary>
    public virtual bool IsRewardedVideoReady => false;

    /// <summary>
    /// 广告错误全局广播（随 Warning 日志）：由启动流程统一订阅做 Toast 提示，业务调用点无需重复处理
    /// </summary>
    public static event Action<string> AdErrorOccurred;

    protected static void RaiseAdError(string errMsg)
    {
        Debug.LogWarning("[AdService] " + errMsg);
        AdErrorOccurred?.Invoke(errMsg);
    }
    /// <summary>
    /// 展示视频激励广告
    /// </summary>
    /// <param name="onClose">关闭广告回调</param>
    /// <param name="onError">广告加载错误回调</param>
    /// <param name="multitonRewardTimes">额外观看广告的次数</param>
    /// <param name="multitonRewardMsg">再得广告的奖励文案，玩家每看完一个广告都会展示</param>
    public abstract void ShowRewardedVideoAd(Action<bool, int> onClose, Action<string> onError = null, int multitonRewardTimes = 0, List<string> multitonRewardMsg = null);
    // 展示Banner广告
    public abstract void ShowBannerAd(EBannerAdPosition pos=EBannerAdPosition.BottomCenter,int width = 360, Action<string> onError = null);
    // 隐藏Banner广告
    public abstract void HideBannerAd();
    // 显示Interstitial广告
    public abstract void ShowInterstitialAd(Action<string> onError);

    // Banner广告显示位置枚举
    public enum EBannerAdPosition
    {
        LeftCenter,
        RightCenter,
        TopCenter,
        BottomCenter,
    }

    /// <summary>
    /// 在自定义位置展示Banner。坐标为屏幕左上角原点的逻辑像素，Y 轴向下。
    /// 平台不支持自定义位置时触发 onError；默认实现即为不支持。
    /// </summary>
    public virtual void ShowBannerAdAt(int left, int top, int width = 360, Action<string> onError = null)
    {
        onError?.Invoke("当前平台不支持自定义Banner位置");
    }

    /// <summary>
    /// 计算预设位置下 Banner 左上角的逻辑像素坐标（原点：屏幕左上角，Y 轴向下）。
    /// 居中与贴边都在安全区内进行，避免被刘海/Home Indicator 遮挡；
    /// 平台无法提供安全区时各内边距传 0（即按全屏计算）。
    /// </summary>
    /// <param name="bannerAdWidth">Banner 宽度（逻辑像素）；高度由平台模板决定，按比例估算</param>
    /// <param name="screenLogicWidth">屏幕逻辑宽度</param>
    /// <param name="screenLogicHeight">屏幕逻辑高度</param>
    /// <param name="pos">预设位置</param>
    /// <param name="safeLeft">安全区左边距（逻辑像素）</param>
    /// <param name="safeTop">安全区顶部边距（刘海/状态栏）</param>
    /// <param name="safeRight">安全区右边距</param>
    /// <param name="safeBottom">安全区底部边距（Home Indicator）</param>
    protected (int left, int top) CalculateBannerAdLogicalPos(int bannerAdWidth, float screenLogicWidth, float screenLogicHeight,
        EBannerAdPosition pos, int safeLeft = 0, int safeTop = 0, int safeRight = 0, int safeBottom = 0)
    {
        int screenW   = (int)screenLogicWidth;
        int screenH   = (int)screenLogicHeight;
        int bannerHeight = Mathf.CeilToInt(bannerAdWidth * BannerEstimatedHeightRatio);

        int left, top;
        switch (pos)
        {
            case EBannerAdPosition.LeftCenter:
                left = safeLeft;
                top  = CenteredCoordinate(screenH, safeTop, safeBottom, bannerHeight);
                break;
            case EBannerAdPosition.RightCenter:
                left = screenW - bannerAdWidth - safeRight;
                top  = CenteredCoordinate(screenH, safeTop, safeBottom, bannerHeight);
                break;
            case EBannerAdPosition.TopCenter:
                left = CenteredCoordinate(screenW, safeLeft, safeRight, bannerAdWidth);
                top  = safeTop;
                break;
            case EBannerAdPosition.BottomCenter:
            default:
                left = CenteredCoordinate(screenW, safeLeft, safeRight, bannerAdWidth);
                top  = screenH - bannerHeight - safeBottom;
                break;
        }
        return (Mathf.Max(0, left), Mathf.Max(0, top));
    }

    /// 在 [start, end-size] 区间内取居中坐标：start + (区间长 - size) / 2，区间放不下时贴 start
    private static int CenteredCoordinate(int totalLength, int startInset, int endInset, int size)
    {
        return startInset + Mathf.Max(0, (totalLength - startInset - endInset - size) / 2);
    }
}