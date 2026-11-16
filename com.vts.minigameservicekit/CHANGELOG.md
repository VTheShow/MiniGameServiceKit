# Changelog

## 0.1.0

- 一步式接入：`PlatformServices.Initialize(...)`——包内完成 SDK 初始化、创建服务、失败自动降级 Mock
- 8 个平台服务：广告（激励视频 / 横幅 / 插屏）、登录与用户信息、存档、内购支付、分享社交、合规（隐私 / 实名）、平台工具、生命周期
- 插屏频控服务 `InterstitialAdService`（免打扰关卡 / 关卡间隔 / 时间间隔，状态自动持久化）
- 平台入口开关 `PlatformEntry`（游戏圈 / 客服 / 关注抖音号 / 桌面快捷方式，配置驱动）
- 5 个配置 SO（AdConfig / PaymentConfig / ShareConfig / PlatformEntryConfig / InterstitialPolicy）全部由包消费
- Mock 双行为：编辑器弹模拟面板便于联调；真机降级时安静失败，不产生假成功（不白给奖励 / 商品）
- 微信虚拟支付下单签名云函数示例（Package Manager → Samples 导入）
- 支持平台：微信小游戏、抖音小游戏
