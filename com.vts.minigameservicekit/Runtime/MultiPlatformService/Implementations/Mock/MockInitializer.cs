using System;
// 编辑器模拟平台SDK初始化
public class MockInitializer :ISdkInitializer
{
    public void Initialize(Action onSuccess, Action<int> onFailed)
    {
        onSuccess?.Invoke();
    }
}