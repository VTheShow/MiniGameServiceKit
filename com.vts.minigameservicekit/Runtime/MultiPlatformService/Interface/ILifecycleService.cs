using System;

public interface ILifecycleService
{
    void QuitGame(Action onSuccess = null, Action<string> onFailed = null, Action onComplete = null);
}