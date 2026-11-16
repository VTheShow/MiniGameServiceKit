using System;

public abstract class LifecycleServiceBase : ILifecycleService
{
    /// <summary> 游戏进入后台（切出游戏、按Home键等） </summary>
    public event Action OnHide;

    /// <summary> 游戏回到前台 </summary>
    public event Action OnShow;

    /// <summary> 请求退出游戏（会先触发 OnBeforeQuit 让你保存） </summary>
    public event Action OnBeforeQuit;

    protected void InvokeOnShow() => OnShow?.Invoke();
    protected void InvokeOnHide() => OnHide?.Invoke();
    public void QuitGame(Action onSuccess = null, Action<string> onFailed = null, Action onComplete = null)
    {
        OnBeforeQuit?.Invoke();
        InternalQuit(onSuccess, onFailed, onComplete);
    }
    protected abstract void InternalQuit(Action onSuccess = null, Action<string> onFailed = null, Action onComplete = null);
}
