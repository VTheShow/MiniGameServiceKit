using System;
using UnityEngine;

/// <summary> 编辑器模拟合规服务：全部直接通过 </summary>
public class MockComplianceService : IComplianceService
{
    public void CheckPrivacyAuthorization(Action<bool> needAuth)
    {
        Debug.Log("[Mock] 模拟查询隐私授权：无需授权");
        needAuth?.Invoke(false);
    }

    public void RequestPrivacyAuthorization(Action<bool> agreed)
    {
        Debug.Log("[Mock] 模拟隐私授权：同意");
        agreed?.Invoke(true);
    }

    public void AuthenticateRealName(Action<bool, string> callback)
    {
        Debug.Log("[Mock] 模拟实名认证：通过");
        callback?.Invoke(true, null);
    }
}
