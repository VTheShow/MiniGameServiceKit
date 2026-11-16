using System;

/// <summary>
/// 用户信息服务
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 平台登录：拿到 code 后应由服务端换取 openid/session_key（微信 code2session），
    /// code 有效期约 5 分钟，客户端只需缓存并等待后端接入
    /// </summary>
    void Login(Action<LoginResult> callback);

    /// <summary> 获取用户信息（昵称/头像） </summary>
    void GetUserInfo(Action<UserProfile> callback);
}

/// <summary> 登录结果 </summary>
[Serializable]
public class LoginResult
{
    public bool success;
    /// <summary> 平台登录凭证（发往服务端换取 openid/session_key） </summary>
    public string code;
    /// <summary> 匿名登录凭证（抖音提供，同样交由服务端处理） </summary>
    public string anonymousCode;
    public string errMsg;
}

/// <summary> 用户信息 </summary>
[Serializable]
public class UserProfile
{
    public bool   success;
    public string nickname;
    public string avatarUrl;
}
