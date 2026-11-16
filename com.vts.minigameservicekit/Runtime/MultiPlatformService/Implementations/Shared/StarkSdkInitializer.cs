#if MINIGAME_SUBPLATFORM_DOUYIN

using System;
using TTSDK;

/// <summary>
/// 抖音平台共用 SDK 初始化：使用 StarkSDK
/// </summary>
public abstract class StarkSdkInitializerBase : ISdkInitializer
{
    public void Initialize(Action onSuccess, Action<int> onFailed)
    {
        TT.InitSDK((code, env) =>
        {
            if (code == 0)
            {
                onSuccess?.Invoke();
            }
            else
            {
                // 错误码         说明
                // 1              TT Unity SDK 版本不支持
                // 2              Unity 引擎版本不被支持
                onFailed?.Invoke(code);
            }
        });
    }
}

#endif
