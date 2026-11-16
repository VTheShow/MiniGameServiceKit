#if MINIGAME_SUBPLATFORM_WEIXIN

using System;
using UnityEngine;
using WeChatWASM;

public class WeChatAuthService : IAuthService
{
    public void Login(Action<LoginResult> callback)
    {
        var op = new LoginOption
        {
            success = (res) =>
            {
                Debug.Log("微信登录成功，code: " + res.code);
                callback?.Invoke(new LoginResult { success = true, code = res.code });
            },
            fail = (res) =>
            {
                Debug.LogError("微信登录失败: " + res.errMsg);
                callback?.Invoke(new LoginResult { success = false, errMsg = res.errMsg });
            },
        };
        WX.Login(op);
    }

    public void GetUserInfo(Action<UserProfile> callback)
    {
        var op = new GetUserInfoOption
        {
            success = (res) =>
            {
                callback?.Invoke(new UserProfile
                {
                    success   = true,
                    nickname  = res.userInfo.nickName,
                    avatarUrl = res.userInfo.avatarUrl,
                });
            },
            fail = (res) =>
            {
                // 授权被拒绝/失败时也必须回调，否则调用方永久挂起
                Debug.LogWarning("获取微信用户信息失败: " + res.errMsg);
                callback?.Invoke(new UserProfile { success = false });
            },
        };
        WX.GetUserInfo(op);
    }
}

#endif
