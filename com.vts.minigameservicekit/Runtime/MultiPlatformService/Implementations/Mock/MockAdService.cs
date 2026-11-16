using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mock 广告服务：
/// - 编辑器：弹出模拟广告面板，完整模拟激励视频 / 横幅 / 插屏流程（真机看不到这些 UI）
/// - 真机降级：不出任何 UI，接口安静失败——杜绝玩家「点假面板白拿奖励」
/// </summary>
public class MockAdService : AdServiceBase
{
    protected override AdConfig.EAdPlatform AdPlatform => AdConfig.EAdPlatform.Mock;

#if UNITY_EDITOR
    private readonly MockAdController mockAdController;

    public MockAdService()
    {
        mockAdController = MockAdController.CreateObject();
        mockAdController.Init();
    }

    public override bool IsRewardedVideoReady => true;

    public override void ShowRewardedVideoAd(Action<bool, int> onClose, Action<string> onError, int multitonRewardTimes = 0, List<string> multitonRewardMsg = null)
    {
        mockAdController.ShowRewardedVideoAD(onClose);
    }

    public override void ShowBannerAd(EBannerAdPosition pos = EBannerAdPosition.BottomCenter, int width = 360, Action<string> onError = null)
    {
        // Unity 安全区为左下角原点（Y 向上），换算为左上角原点的四边内边距
        var sa = Screen.safeArea;
        int safeLeft   = (int)sa.x;
        int safeBottom = (int)sa.y;
        int safeTop    = Screen.height - (int)(sa.y + sa.height);
        int safeRight  = Screen.width - (int)(sa.x + sa.width);

        var (left, top) = CalculateBannerAdLogicalPos(width, Screen.width, Screen.height, pos,
            safeLeft, safeTop, safeRight, safeBottom);
        ShowBannerAdAt(left, top, width, onError);
    }

    public override void ShowBannerAdAt(int left, int top, int width = 360, Action<string> onError = null)
    {
        // MockAdController 的横幅锚定在画布左上角（pivot 左上），Y 取负表示从顶部向下偏移
        mockAdController.ShowBannerAD(width, left, -top);
        Debug.Log($"编辑器模拟显示Banner广告 pos=({left},{top}) size={width}");
    }

    public override void HideBannerAd()
    {
        mockAdController.HideBannerAD();
    }

    public override void ShowInterstitialAd(Action<string> onError)
    {
        mockAdController.ShowInterstitialAD();
    }
#else
    // 真机降级：SDK 初始化失败后游戏仍可玩，但广告全部不可用
    public override bool IsRewardedVideoReady => false;

    public override void ShowRewardedVideoAd(Action<bool, int> onClose, Action<string> onError, int multitonRewardTimes = 0, List<string> multitonRewardMsg = null)
    {
        Debug.LogWarning("[MockAd] 真机降级模式：激励视频不可用，按「未看完」回调");
        onClose?.Invoke(false, 0);
    }

    public override void ShowBannerAd(EBannerAdPosition pos = EBannerAdPosition.BottomCenter, int width = 360, Action<string> onError = null) { }

    public override void ShowBannerAdAt(int left, int top, int width = 360, Action<string> onError = null) { }

    public override void HideBannerAd() { }

    public override void ShowInterstitialAd(Action<string> onError) { }
#endif
}
