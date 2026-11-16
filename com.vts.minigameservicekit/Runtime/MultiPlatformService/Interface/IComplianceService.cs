using System;

/// <summary>
/// 平台合规服务：隐私授权与实名认证（各平台要求见各实现注释）。
/// 微信：隐私协议授权为上线必接（基础库 2023-09 起强制）；实名/防沉迷由平台侧托管，无需客户端接入。
/// 抖音：需客户端主动拉起实名认证（未实名用户支付等功能受限）。
/// </summary>
public interface IComplianceService
{
    /// <summary> 查询是否需要隐私授权（微信：getPrivacySetting；其余平台恒回调 false） </summary>
    void CheckPrivacyAuthorization(Action<bool> needAuth);

    /// <summary>
    /// 拉起隐私授权（微信：requirePrivacyAuthorize，未注册自定义监听时弹平台默认弹窗，
    /// 同意/拒绝均会回调；其余平台恒回调 true）
    /// </summary>
    void RequestPrivacyAuthorization(Action<bool> agreed);

    /// <summary> 拉起实名认证（抖音：AuthenticateRealName；微信平台托管恒成功；不支持的平台恒成功） </summary>
    void AuthenticateRealName(Action<bool, string> callback);
}
