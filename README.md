# MiniGame Service Kit (多平台小游戏SDK接入工具)


统一接口 + 条件编译工厂 + Mock 兜底。覆盖微信小游戏 / 抖音小游戏两平台的广告、内购支付、登录、存档、合规、分享社交能力。

平台 SDK 未安装或初始化失败时自动降级 Mock：getter 永不返回 null；编辑器有模拟面板可完整联调，真机上广告/支付静默···失败（不产生假成功），游戏照常可玩。

---

## 一、安装

### 1.1 装包

Package Manager → Add package from git URL 粘贴：

```
https://github.com/VTheShow/MiniGameServiceKit.git?path=/com.vts.minigameservicekit#v0.1.0
```

固定 v0.1.0 版本，推荐生产工程使用。要跟随最新开发版可把末尾的 `#v0.1.0` 换成 `#main`，但后续提交可能影响兼容性，不推荐用于已上线工程。

包在仓库子目录 `com.vts.minigameservicekit/` 下，`?path=/com.vts.minigameservicekit` 不能省——省略后 UPM 会到仓库根目录找 `package.json` 并报错。

### 1.2 平台 SDK

按要发布的平台安装对应 SDK，不捆绑：

| 目标平台 | 需安装的 SDK | 平台宏 | 包内实现 |
|---|---|---|---|
| 微信小游戏 | [微信转换SDK](https://gitee.com/wechat-minigame/minigame-tuanjie-transform-sdk) | `MINIGAME_SUBPLATFORM_WEIXIN` | Implementations/WeChat |
| 抖音小游戏 | StarkSDK | `MINIGAME_SUBPLATFORM_DOUYIN` | Implementations/Shared |
| Unity 编辑器 | — | — | Mock 全套 |

平台宏由团结引擎在激活对应子平台时自动注入（需团结引擎 1.5 及以上）；宏未定义时对应平台代码不参与编译，所以只安装需要发布的那个平台的 SDK 即可。

---

## 二、接入流程与时序

三步：

1. 建 5 个配置 SO 并填内容（见第四节）
2. 启动时调一次 `PlatformServices.Initialize(...)`——包内自动完成「拉起平台 SDK → 创建服务 → 失败降级」
3. 业务代码通过 `PlatformServices.XxxService` 调能力（见第五节）

包内做的事（接入方不用手写）：

```
PlatformServices.Initialize(ad, pay, share, entry, policy, maxAttempts: 3)
 ├─ SdkInitializer.Initialize()：拉起平台 SDK（微信 WX.InitSDK / 字节 TT.InitSDK）
 ├─ 成功：CreateServices(...) 创建 9 个服务并注入各配置，然后回调 onReady
 └─ 失败：立即重试至 maxAttempts；仍失败则 FallbackToMock() 并回调 onFallback(code)
```

降级的含义：存档、生命周期、平台工具照常可用；广告/支付只在编辑器有模拟面板，真机上安静失败——不会白给奖励或商品。

完整启动示例：

```csharp
using VTS.MiniGameService;
using UnityEngine;

public class GameLauncher : MonoBehaviour
{
    [SerializeField] AdConfigSO            adConfig;
    [SerializeField] PaymentConfigSO       paymentConfig;
    [SerializeField] ShareConfigSO         shareConfig;
    [SerializeField] PlatformEntryConfigSO entryConfig;
    [SerializeField] InterstitialPolicySO  interstitialPolicy;

    void Start()
    {
        // 传入各服务配置项，无需配置可传 null
        PlatformServices.Initialize(
            adConfig, paymentConfig, shareConfig, entryConfig, interstitialPolicy,
            maxAttempts: 3,
            onReady: () =>
            {
                PlatformServices.AdService.Preload();   // 预拉激励视频/插屏素材
                // 平台服务就绪，继续游戏自己的启动流程
            },
            onFallback: errCode =>
            {
                // 已降级 Mock：存档与主流程照常；广告/支付仅编辑器有模拟面板，真机静默失败（不产生假成功）
                Debug.LogWarning($"平台 SDK 初始化失败 code={errCode}，已降级 Mock");
            });
    }
}
```

需要「间隔重试」「先做别的再初始化」这类自定义流程时，改用两步：`PlatformServices.SdkInitializer.Initialize(onSuccess, onFailed)` → 成功后自己调 `CreateServices(...)`。

会话编排（隐私授权 → 实名 → 静默登录 → 分享素材）、场景加载、入口 UI 都属游戏侧，包不提供，按各游戏自己的启动流程实现（参考写法见 5.6 与 5.5）。

---

## 三、服务清单

全部服务从 `PlatformServices` 取，未初始化时自动返回 Mock 实例，不会为 null。

| 服务 | 接口 / 类型 | 获取方式 | 主要能力 |
|---|---|---|---|
| 平台 SDK 初始化 | `ISdkInitializer` | `PlatformServices.SdkInitializer` | `Initialize(onSuccess, onFailed)`——一般用 `PlatformServices.Initialize(...)` 一步完成 |
| 广告 | `AdServiceBase` | `PlatformServices.AdService` | `ShowRewardedVideoAd` / `ShowBannerAd` / `HideBannerAd` / `ShowInterstitialAd` / `Preload` / `IsRewardedVideoReady` |
| 登录与用户信息 | `IAuthService` | `PlatformServices.AuthService` | `Login` / `GetUserInfo` |
| 存档 | `ISaveService` | `PlatformServices.SaveService` | `Save<T>` / `Load<T>` / `Delete` / `DeleteAll` 及对应 Async 版本 |
| 支付 | `IPaymentService` | `PlatformServices.PaymentService` | `IsPaymentSupported` / `QueryPaymentSupport` / `Pay` |
| 分享社交 | `ISocialService` | `PlatformServices.SocialService` | 分享 / 收藏 / 桌面快捷方式 / 游戏圈 / 客服 / 关注官方号 |
| 合规 | `IComplianceService` | `PlatformServices.ComplianceService` | `CheckPrivacyAuthorization` / `RequestPrivacyAuthorization` / `AuthenticateRealName` |
| 平台工具 | `IPlatformUtilityService` | `PlatformServices.UtilityService` | 安全区 / 屏幕尺寸 / 帧率 / 屏幕方向 / 键盘 / 系统字体 / 触摸 / 震动 / 剪贴板 |
| 生命周期 | `LifecycleServiceBase` | `PlatformServices.LifecycleService` | `OnShow` / `OnHide` / `OnBeforeQuit` 事件、`QuitGame` |
| 插屏频控 | `InterstitialAdService` | `PlatformServices.InterstitialAdService` | `SetAdFreePredicate` / `NotifyLevelSettled` / `AdsAllowed` / `ResetState` |

初始化状态：

```csharp
PlatformServices.InitState      // NotInit / Success / FallbackMock
PlatformServices.IsInitSuccess  // 真实平台实现初始化成功
PlatformServices.IsFallbackMode // 已降级 Mock
```

各平台能力支持情况：

| 能力 | 微信 | 抖音 |
|---|---|---|
| 广告 | ✅ | ✅ |
| 存档 | ✅ | ✅ |
| 登录 | ✅ | ✅ |
| 分享社交 | ✅ | ✅ |
| 内购支付 | ✅ 需云函数 | ✅ |
| 隐私授权 | ✅ | — |
| 实名认证 | 平台托管 | ✅ |

---

## 四、配置资产

包里**不含任何配置资产**，每个工程自己建。菜单 `Create → Scriptable Object →` 选类型，**存放目录自定**（如 `Assets/Configs/`）

| 资产 | 用途 |
|---|---|
| `AdConfigSO` | 各平台激励视频/横幅/插屏广告位 ID |
| `PaymentConfigSO` | 商品列表、下单签名云函数地址、支付环境 |
| `ShareConfigSO` | 转发标题/图片/query、朋友圈素材、微信客服参数 |
| `PlatformEntryConfigSO` | 游戏圈/客服/关注抖音号/快捷方式入口的显隐与位置 |
| `InterstitialPolicySO` | 自动插屏的频控参数 |

资产通过 `PlatformServices.Initialize` 一次注入交给包

```csharp
PlatformServices.Initialize(adConfig, paymentConfig, shareConfig, entryConfig, interstitialPolicy);
```

| 传入的资产 | 包内自动做的事 |
|---|---|
| `AdConfigSO` | 取当前平台的三个广告位 ID，注入 `AdService` |
| `PaymentConfigSO` | 注入商品列表、云函数地址、支付环境到 `PaymentService` |
| `ShareConfigSO` | 自动调 `SetupDefaultShare` 设默认转发素材、`ShareToTimeline` 设朋友圈素材；微信还会注入客服 corpId/url |
| `PlatformEntryConfigSO` | 注入 `PlatformEntry`，游戏圈/客服/关注/快捷方式入口按开关生效（详见 4.4） |
| `InterstitialPolicySO` | 注入 `InterstitialAdService`，关卡结算时按频控自动弹插屏（详见 4.5） |

参数都可传 null，传了哪个就配哪个。

### 4.1 AdConfigSO（广告位）

`adConfigList` 每个平台一条，`adPlatform` 选平台后填三个 ID：

| 字段 | 对应广告 |
|---|---|
| 激励视频广告ID | 激励视频 |
| 横幅广告ID | Banner / 原生模板 |
| 插屏广告ID | 插屏 |

只填要上线的平台即可。未填的平台对应广告不可用，Console 打「广告位ID未配置」警告；编辑器里始终走 Mock 广告面板。

### 4.2 PaymentConfigSO（商品）

`products` 每行一个商品：

| 字段 | 说明 |
|---|---|
| 商品ID(游戏内部) | 订单与发货用的内部 ID，如 `coin_pack_1` |
| 商品名 | 展示在平台支付面板 |
| 价格(元) | 下单金额自动换算为分（×100 取整） |
| 商品类型 | 例：星星礼包 / 去广告特权 / 自定义 |
| 到账星星数 | 仅「星星礼包」类型使用 |
| 微信道具ID | 微信后台「虚拟支付-道具直购」建的道具 ID |
| 抖音道具ID | 抖音后台「支付-道具/礼包」建的道具 ID |

其他字段：

- **下单签名云函数地址**：编辑器阶段留空（走本地调试签名，Mock 支付可完整走通）；部署云函数后填 URL。
- **支付环境**：0 = 线上，1 = 沙箱。

### 4.3 ShareConfigSO（分享）

| 字段 | 说明 |
|---|---|
| 转发标题 / 转发图片URL | 建议必填，否则右上角菜单转发是裸链接。图片放 CDN，https，5:4 比例效果最佳 |
| 转发携带query | 如 `from=friend`，用于分享回流统计 |
| 朋友圈标题 / 图片 | 仅微信，且仅 Android 支持被动分享 |
| 微信客服企业ID / 客服链接 | 开通微信客服后填；留空则客服入口不可用 |

传给 `PlatformServices.Initialize` 后包会自动设置，不需要手动调 `SetupDefaultShare`。标题和图片都非空才会设置；朋友圈素材同理。

### 4.4 PlatformEntryConfigSO（入口显隐）

这是一块**策划开关面板**：勾掉哪项，对应入口就不出现，不用改代码。

| 字段 | 关掉后的效果 |
|---|---|
| 显示游戏圈(微信) | `ShowGameClubButton` 直接跳过，不创建按钮 |
| 显示客服入口(微信) | `OpenCustomerService` 直接跳过 |
| 显示关注抖音号入口 | `FollowOfficialAccount` 直接回调 `false`，不拉起面板 |
| 新用户引导添加桌面快捷方式(抖音) | `PlatformEntry.ShouldGuideShortcut` 变 false，游戏侧据此决定要不要弹引导 |
| 游戏圈按钮样式 / 右边距 / 顶部边距 | 只影响样式与位置，不影响显隐 |

开关同时受平台能力约束：游戏圈只在微信生效，关注号只在抖音生效；编辑器下视为全平台可用，方便调试。

游戏 UI 摆按钮时直接读解析结果，不用自己判断平台：

```csharp
using VTS.MiniGameService;

bool showClub    = PlatformEntry.ShowGameClub;          // 配置开 && 当前平台支持
bool showService = PlatformEntry.ShowCustomerService;
bool showFollow  = PlatformEntry.ShowFollowAccount;
bool guideShortcut = PlatformEntry.ShouldGuideShortcut;

// 已把配置里的「右边距/顶部边距」换算成屏幕左上角原点的 left/top
social.ShowGameClubButton(PlatformEntry.GameClubStyle);
```

> 未把 `PlatformEntryConfigSO` 传给 `PlatformServices.Initialize` 时，`PlatformEntry` 不做任何拦截，服务保持旧行为。

### 4.5 InterstitialPolicySO（插屏频控）

| 字段 | 默认 | 说明 |
|---|---|---|
| 启用自动插屏 | 开 | 关闭后只能手动调 `ShowInterstitialAd` |
| 免打扰关卡数 | 3 | 前 N 关结算不弹 |
| 间隔关卡数 | 2 | 两次插屏之间至少间隔的通关数 |
| 间隔秒数 | 180 | 两次插屏之间至少间隔的秒数 |

传给 `PlatformServices.Initialize` 后，`InterstitialAdService` 会在你调用 `NotifyLevelSettled(levelId)` 时按上面四条判定要不要弹插屏；频控状态用包内 `SaveService` 持久化，跨会话有效。

「已买去广告」这类游戏侧状态通过谓词注入，包不碰你的存档结构：

```csharp
PlatformServices.InterstitialAdService.SetAdFreePredicate(
    () => saveData.removeAdPurchased);
```

---

## 五、能力接入示例

示例中的 `Toast.Show` / `GrantReward` / `ApplySave` / `StartGame` / `SendCodeToServer` 等为游戏侧自建方法，`rewardButton` / `nicknameText` / `avatarUrl` 等为游戏侧自有 UI 与变量，替换成真实项目实现即可；`paymentConfig` 指初始化时注入的那个 `PaymentConfigSO`。

### 5.1 广告

```csharp
var ads = PlatformServices.AdService;

// 激励视频：onClose(isEnded, viewedCount) —— 是否完整看完、本次流程已看完数量
ads.ShowRewardedVideoAd(
    onClose: (isEnded, viewedCount) =>
    {
        if (isEnded) GrantReward(viewedCount);
    },
    onError: err => Debug.LogWarning("激励视频失败: " + err));

// 按钮置灰用：激励视频是否已就绪（编辑器 Mock 恒为 true；真机降级时为 false）
rewardButton.interactable = ads.IsRewardedVideoReady;

// 横幅（原生模板），宽度 360 逻辑像素，底部居中
ads.ShowBannerAd(AdServiceBase.EBannerAdPosition.BottomCenter, 360);
ads.HideBannerAd();

// 插屏
ads.ShowInterstitialAd(err => Debug.LogWarning("插屏失败: " + err));
```

广告错误可全局订阅一次，统一做 Toast，业务调用点不必逐个处理：

```csharp
AdServiceBase.AdErrorOccurred += msg => Toast.Show(msg);
```

需要「按关卡自动弹插屏」时用频控服务，不用自己写判定：

```csharp
var interstitial = PlatformServices.InterstitialAdService;

// 关卡结算回主页时调用：按免打扰关卡 / 间隔关卡 / 间隔秒数判定，满足才真正展示
interstitial.NotifyLevelSettled(levelId);

// 商店里判断是否已拥有去广告特权
bool removeAdOwned = !interstitial.AdsAllowed;
```

### 5.2 登录与用户信息

```csharp
var auth = PlatformServices.AuthService;

auth.Login(result =>
{
    if (!result.success)
    {
        Debug.LogError("登录失败: " + result.errMsg);
        return;
    }
    // code 有效期约 5 分钟，发往服务端换 openid / session_key
    SendCodeToServer(result.code);
});

auth.GetUserInfo(profile =>
{
    if (!profile.success) return;
    nicknameText.text = profile.nickname;
    avatarUrl = profile.avatarUrl;
});
```

### 5.3 存档

```csharp
[System.Serializable]
public class PlayerSave
{
    public int level;
    public int coins;
}

var save = PlatformServices.SaveService;

// 同步：内部用 JsonUtility 序列化，T 必须是 [Serializable] 类
save.Save("player", new PlayerSave { level = 3, coins = 120 });
PlayerSave data = save.Load<PlayerSave>("player");   // 不存在时返回 null

// 异步
save.SaveAsync("player", data, r =>
{
    if (!r.isSuccess) Debug.LogError("存档失败: " + r.message);
});
save.LoadAsync<PlayerSave>("player", (d, r) =>
{
    if (r.isSuccess) ApplySave(d);
});

// 删除
save.Delete("player");
save.DeleteAll();
```

### 5.4 支付

```csharp
var pay = PlatformServices.PaymentService;

// 先确认当前环境可支付（微信需查询虚拟支付开通情况）
pay.QueryPaymentSupport(supported =>
{
    if (!supported)
    {
        Toast.Show("当前环境不支持支付");
        return;
    }

    // 按 PaymentConfigSO 中的商品生成订单，金额取配置价，orderId 自动生成
    PayProduct product = paymentConfig.GetProduct("star_pack_small");
    PayOrder   order   = PayOrder.Create(product, paymentConfig.payEnv);

    pay.Pay(order, result =>
    {
        if (result.success)
            Debug.Log("支付完成，等待服务端发货: " + result.orderId);
        else
            Debug.LogWarning($"支付失败 {result.errCode}: {result.errMsg}");
    });
});
```

支付成功只代表玩家完成支付动作，**发货与订单校验以服务端回调为准**。

### 5.5 分享与社交

```csharp
var social = PlatformServices.SocialService;

// 被动分享素材：把 ShareConfigSO 传给 `PlatformServices.Initialize` 时会自动设置；这里演示手动调用
social.SetupDefaultShare("一起来玩！", "https://cdn.example.com/share.png", "from=menu");

// 主动拉起分享面板
social.Share("我通关了第 10 关！", "https://cdn.example.com/share.png", ok =>
{
    Debug.Log("分享结果: " + ok);
});

// 微信游戏圈悬浮按钮（需后台开通，仅真机生效）
social.ShowGameClubButton(new GameClubButtonOptions { text = "游戏圈", left = 20, top = 170 });
social.HideGameClubButton();
social.DestroyGameClubButton();

// 微信客服（需在 ShareConfigSO 配置 corpId 与链接）
social.OpenCustomerService();

// 抖音关注官方号
social.FollowOfficialAccount(ok => Debug.Log("关注: " + ok));

// 桌面快捷方式（抖音）
social.CheckShortcut(has => { if (!has) social.AddShortcut("游戏名", "https://cdn.example.com/icon.png", ok => { }); });
```

### 5.6 合规

```csharp
var compliance = PlatformServices.ComplianceService;

// 微信：隐私协议授权为上线必接
compliance.CheckPrivacyAuthorization(needAuth =>
{
    if (!needAuth)
    {
        StartGame();
        return;
    }

    compliance.RequestPrivacyAuthorization(agreed =>
    {
        if (agreed) StartGame();
        else ShowPrivacyRejectTip();
    });
});

// 抖音：需主动拉起实名认证，未实名用户支付等功能受限
compliance.AuthenticateRealName((ok, msg) =>
{
    Debug.Log($"实名认证: {ok} {msg}");
});
```

### 5.7 平台工具

```csharp
var util = PlatformServices.UtilityService;

Rect    safeArea = util.GetLogicSafeArea();     // 逻辑像素安全区，适配刘海/Home Indicator
Vector2 screen   = util.GetLogicScreenSize();
util.SetPreferredFrames(60);
util.VibrateShort();
util.SetClipboardText("邀请码 1234");
util.GetClipboardText(text => Debug.Log("剪贴板: " + text));
```

### 5.8 生命周期

```csharp
var life = PlatformServices.LifecycleService;

life.OnHide       += SaveGame;      // 切到后台
life.OnShow       += RefreshUI;     // 回到前台
life.OnBeforeQuit += SaveGame;      // 退出前

life.QuitGame();
```

---

## 六、平台后台配置


### 6.1 微信小游戏

**广告**（公众平台 → 流量主）
- 开通流量主（需累计独立访客达标）
- 建三个广告位：激励视频 / 原生模板（横幅）/ 插屏，抄下 ID
- 填到 `AdConfigSO` 的「微信小游戏」一行

**内购**（虚拟支付）
- 公众平台 → 功能 → 虚拟支付：完成开户（需资质）
- 道具管理：按 `PaymentConfigSO` 里的商品逐个建道具，道具 ID 与配置一致
- 部署下单云函数（见第七节）
- `PaymentConfigSO` 填云函数地址、微信道具 ID；正式环境 `payEnv` 改 0
- 配置消息推送（发货/退款回调）

**合规**
- 公众平台 → 设置 → 服务内容声明 → 用户隐私保护指引：声明收集项（需与实际调用匹配）
- 游戏侧调 `CheckPrivacyAuthorization` / `RequestPrivacyAuthorization`（上线必接）
- 实名/防沉迷由平台托管，无需接入

**增长**
- 游戏圈：公众平台 → 功能 → 游戏圈开通，游戏侧调 `ShowGameClubButton`
- 微信客服：开通并绑定，corpId/url 填 `ShareConfigSO`，`PlatformEntryConfigSO` 勾选客服入口
- 分享图建议 5:4、放 CDN（https）

**构建**
- MiniGameConfig（WX-WASM-SDK-V2/Editor）：填 AppID、CDN 地址

### 6.2 抖音小游戏

**广告**（抖音开发者平台 → 流量变现）
- 开通巨量引擎变现，建激励视频 / Banner / 插屏广告位
- 填到 `AdConfigSO` 的「抖音小游戏」一行

**内购**
- 开发者平台 → 变现 → 支付能力：申请开通（需资质与版号，以平台当前政策为准）
- 配置道具/礼包，道具 ID 填 `PaymentConfigSO.douyinGoodsId`，价格一致
- 服务端配支付回调地址（下单无需签名，但发货回调要自己接）

**合规**
- 游戏侧调 `AuthenticateRealName` 拉起实名认证（未实名仅限支付等功能）
- 防沉迷由平台托管

**增长**
- 绑定官方抖音号（关注入口由 `PlatformEntryConfigSO.showFollowAccount` 控制）
- 桌面快捷方式引导（`shortcutGuideEnabled`，默认开）

**构建**
- 确认 `project.config.json` 里的 appid 
- 在 Build Settings → MiniGame → Sub Platforms 激活「抖音小游戏」：引擎会自动注入 `MINIGAME_SUBPLATFORM_DOUYIN` 并重新编译，无需手动加宏（见 1.2）

---

## 七、云函数（微信虚拟支付下单签名）

微信虚拟支付要求 `signData` / `paySig` / `signature` 由服务端生成，客户端支付前请求。

- 示例代码与部署教程：Package Manager → 本包详情页 → **Samples** → Import（导入到 `Assets/Samples/`）；或直接看仓库里的 [`com.vts.minigameservicekit/Samples~/Docs/cloudfunctions/`](com.vts.minigameservicekit/Samples~/Docs/cloudfunctions)
- 部署完把 HTTP 触发地址填进 `PaymentConfigSO.cloudFunctionUrl`，包会自动从本地调试签名切到云函数签名

`cloudFunctionUrl` 留空时走本地调试签名：编辑器/沙箱能走通流程，**真机正式支付会被微信拒绝**。

---

## 八、常见问题

**没装平台 SDK 能跑吗？**
能。编辑器下 `MockInitializer` 直接回调成功，8 个服务全是 Mock 实现，广告/支付有模拟面板可完整联调；真机上 SDK 初始化失败会降级 Mock，存档与主流程照常，但**广告和支付安静失败**——不弹任何模拟 UI、不产生假成功（不会白给奖励或商品）。

**广告位 ID 没填会怎样？**
对应广告不可用，Console 打「广告位ID未配置」警告；编辑器里 Mock 广告面板仍可点，不影响调试。

**改了 ShareConfigSO 没反应？**
确认它传给了 `PlatformServices.Initialize`。传了才会自动设置；没传或标题/图片有空值时会跳过，Console 有对应提示。

**关掉 PlatformEntryConfigSO 的开关，按钮还在？**
同样确认它传给了 `PlatformServices.Initialize`。未传时 `PlatformEntry` 不拦截，服务保持旧行为。

**支付一直失败或签名报错？**
先看 `PaymentConfigSO.cloudFunctionUrl` 是否为空——留空时用本地调试签名，真机必失败。再确认云函数里的 AppID / AppSecret / offerId / AppKey 是否填对。

**服务会不会为 null？**
不会。`PlatformServices` 所有服务 getter 都有 Mock 兜底，未初始化也能安全调用。
