#if MINIGAME_SUBPLATFORM_WEIXIN

using System;
using WeChatWASM;

public class WeChatLifecycleService : LifecycleServiceBase
{
    public WeChatLifecycleService()
    {
        WX.OnHide((res) => InvokeOnHide());
        WX.OnShow((res) => InvokeOnShow());
    }

    protected override void InternalQuit(Action onSuccess = null, Action<string> onFailed = null, Action onComplete = null)
    {
        var op = new ExitMiniProgramOption()
        {
            success = (res) => onSuccess?.Invoke(),
            fail = (res) => onFailed?.Invoke(res.errMsg),
            complete = (res) => onComplete?.Invoke(),
        };
        WX.ExitMiniProgram(op);
    }
}

#endif
