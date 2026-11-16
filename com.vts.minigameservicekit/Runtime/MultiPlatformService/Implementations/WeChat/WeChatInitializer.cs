#if MINIGAME_SUBPLATFORM_WEIXIN

using System;
using WeChatWASM;

public class WeChatInitializer :ISdkInitializer // 微信SDK初始化
{
    public void Initialize(Action onSuccess, Action<int> onFailed)
    {
        // 注：微信小游戏官方文档未找到WX.InitSDK 的 code 返回值具体含义，从网络开发者社区获取信息
        //      code        含义
        //      200         初始化成功(标准)
        //      1           成功(部分旧版、编辑器环境)
        //      其他        不知道，官网文档没有，问小游戏客服，客服答“https://developers.weixin.qq.com/minigame/introduction/文档中仅说明了该函数是异步的，未提供 code 值的详细定义”
        WX.InitSDK((code) =>
        {
            if (code == 0 || code == 200)
            {
                onSuccess?.Invoke();
            }
            else
            {
                onFailed?.Invoke(code);
            }
        });
    }
}
#endif