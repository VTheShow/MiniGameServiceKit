#if MINIGAME_SUBPLATFORM_DOUYIN

using System;
using UnityEngine;
using TTSDK;

/// <summary>
/// 抖音平台共用账号服务：TT.Login / TT.GetUserInfo
/// </summary>
public abstract class StarkSdkAuthServiceBase : IAuthService
{
    public void Login(Action<LoginResult> callback)
    {
        TT.Login(
            (code, anonymousCode, isLogin) =>
            {
                Debug.Log("[StarkAuth] 登录成功 code: " + code);
                callback?.Invoke(new LoginResult { success = true, code = code, anonymousCode = anonymousCode });
            },
            (errMsg) =>
            {
                Debug.LogError("[StarkAuth] 登录失败: " + errMsg);
                callback?.Invoke(new LoginResult { success = false, errMsg = errMsg });
            });
    }

    public void GetUserInfo(Action<UserProfile> callback)
    {
        TT.GetUserInfo(
            (ref TTUserInfo userInfo) =>
            {
                callback?.Invoke(new UserProfile
                {
                    success   = true,
                    nickname  = userInfo.nickName,
                    avatarUrl = userInfo.avatarUrl,
                });
            },
            (errMsg) =>
            {
                // 授权被拒绝/失败时也必须回调，否则调用方永久挂起
                Debug.LogWarning("[StarkAuth] 获取用户信息失败: " + errMsg);
                callback?.Invoke(new UserProfile { success = false });
            });
    }
}

#endif
