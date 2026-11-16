#if MINIGAME_SUBPLATFORM_DOUYIN

using System;
using System.Collections.Generic;
using TTSDK;

/// <summary>
/// 抖音平台共用生命周期服务：TT.GetAppLifeCycle 的 OnShow/OnHide + ExitMiniProgram
/// </summary>
public abstract class StarkSdkLifecycleServiceBase : LifecycleServiceBase
{
    protected StarkSdkLifecycleServiceBase()
    {
        var lifecycle = TT.GetAppLifeCycle();
        lifecycle.OnHide += OnAppHide;
        lifecycle.OnShow += OnAppShow;
    }

    private void OnAppHide()
    {
        InvokeOnHide();
    }

    private void OnAppShow(Dictionary<string, object> param)
    {
        InvokeOnShow();
    }

    protected override void InternalQuit(Action onSuccess = null, Action<string> onFailed = null, Action onComplete = null)
    {
        TT.ExitMiniProgram(false, (success) =>
        {
            if (success)
            {
                onSuccess?.Invoke();
            }
            else
            {
                onFailed?.Invoke("退出小游戏被拒绝或取消");
            }
        });
        onComplete?.Invoke();
    }
}

#endif
