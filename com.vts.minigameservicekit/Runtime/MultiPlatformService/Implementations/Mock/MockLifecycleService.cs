using System;
using UnityEngine;

// 模拟生命周期服务：编辑器内退出 = 停止播放；真机 = 退出应用
public class MockLifecycleService : LifecycleServiceBase
{
    protected override void InternalQuit(Action onSuccess = null, Action<string> onFailed = null, Action onComplete = null)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        onSuccess?.Invoke();
        onComplete?.Invoke();
    }
}
