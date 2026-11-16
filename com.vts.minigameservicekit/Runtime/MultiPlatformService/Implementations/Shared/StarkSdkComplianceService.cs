#if MINIGAME_SUBPLATFORM_DOUYIN

using System;
using UnityEngine;
using TTSDK;

/// <summary>
/// 抖音平台合规服务：
/// - 实名认证：抖音要求客户端主动拉起（未实名用户支付等功能受限）；
///   AuthenticateRealName 的成功回调表示认证窗口拉起成功，
///   认证完成的最终结果建议后续接 TTAccount.SetRealNameAuthenticationCallback 统一处理
/// - 隐私授权：字节平台无微信式隐私授权弹窗接口，恒为已授权
/// </summary>
public abstract class StarkSdkComplianceServiceBase : IComplianceService
{
    public void CheckPrivacyAuthorization(Action<bool> needAuth)
    {
        needAuth?.Invoke(false);
    }

    public void RequestPrivacyAuthorization(Action<bool> agreed)
    {
        agreed?.Invoke(true);
    }

    public void AuthenticateRealName(Action<bool, string> callback)
    {
        // StarkSDK 6.9.5：实名入口收敛到 TT 静态门面（TTAccount.AuthenticateRealName 为实例成员已不可直接调）
        TT.AuthenticateRealName(
            (errMsg) =>
            {
                Debug.Log("[StarkCompliance] 实名认证窗口已拉起");
                callback?.Invoke(true, errMsg);
            },
            (errMsg) =>
            {
                Debug.LogWarning("[StarkCompliance] 拉起实名认证失败: " + errMsg);
                callback?.Invoke(false, errMsg);
            });
    }
}

#endif
