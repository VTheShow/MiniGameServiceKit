using System;

/// <summary>
/// 平台 SDK 初始化接口：只负责拉起第三方平台 SDK（微信 WX.InitSDK / 字节 TT.InitSDK），
/// **不负责创建包内服务**（那是 PlatformServices.CreateServices 的事）。
/// 一般情况下直接用 PlatformServices.Initialize 一步完成，无需自己调本接口。
/// </summary>
public interface ISdkInitializer
{
    /// <summary> 拉起平台 SDK </summary>
    /// <param name="onSuccess">初始化成功</param>
    /// <param name="onFailed">初始化失败，参数为平台原始错误码（含义随平台不同，仅用于日志）</param>
    void Initialize(Action onSuccess, Action<int> onFailed);
}
