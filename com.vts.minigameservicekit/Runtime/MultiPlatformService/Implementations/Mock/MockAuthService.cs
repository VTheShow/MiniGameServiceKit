using System;
using UnityEngine;

public class MockAuthService : IAuthService
{
    public void Login(Action<LoginResult> callback)
    {
        Debug.Log("[Mock] 模拟登录成功");
        callback?.Invoke(new LoginResult { success = true, code = "mock_code" });
    }

    public void GetUserInfo(Action<UserProfile> callback)
    {
        Debug.Log("[Mock] 返回模拟用户信息");
        callback?.Invoke(new UserProfile { success = true, nickname = "模拟玩家", avatarUrl = string.Empty });
    }
}
