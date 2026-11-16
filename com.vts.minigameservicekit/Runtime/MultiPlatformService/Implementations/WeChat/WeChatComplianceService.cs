#if MINIGAME_SUBPLATFORM_WEIXIN

using System;
using UnityEngine;
using WeChatWASM;

/// <summary>
/// 微信合规服务：
/// - 隐私授权：getPrivacySetting 查询是否需要授权 → requirePrivacyAuthorize 拉起授权
///   （未注册 onNeedPrivacyAuthorization 时微信弹平台默认弹窗；用户拒绝不阻塞进入游戏，
///    但后续调用涉及隐私的接口会被平台拦截）
/// - 实名/防沉迷：微信平台侧托管，无需客户端接入
/// </summary>
public class WeChatComplianceService : IComplianceService
{
    public void CheckPrivacyAuthorization(Action<bool> needAuth)
    {
        WX.GetPrivacySetting(new GetPrivacySettingOption
        {
            success = (res) => needAuth?.Invoke(res.needAuthorization),
            fail = (err) =>
            {
                // 低版本基础库无此接口：视为无需授权，交由平台在调用隐私接口时兜底
                Debug.LogWarning("[WeChatCompliance] 查询隐私授权状态失败: " + err.errMsg);
                needAuth?.Invoke(false);
            },
        });
    }

    public void RequestPrivacyAuthorization(Action<bool> agreed)
    {
        WX.RequirePrivacyAuthorize(new RequirePrivacyAuthorizeOption
        {
            success = (res) => agreed?.Invoke(true),
            fail = (err) =>
            {
                Debug.Log("[WeChatCompliance] 用户拒绝隐私授权或拉起失败: " + err.errMsg);
                agreed?.Invoke(false);
            },
        });
    }

    public void AuthenticateRealName(Action<bool, string> callback)
    {
        // 实名认证与防沉迷由微信平台托管，无需客户端拉起
        callback?.Invoke(true, null);
    }
}

#endif
