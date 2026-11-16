# 云函数部署教程：10 分钟接通微信虚拟支付下单签名

> 口诀：**开通虚拟支付拿三钥 → 建云函数填配置 → 开 HTTP 访问拿地址 → 地址填回 Unity → 真机试买一单**
>
> 为什么需要云函数：微信虚拟支付的两个签名（paySig 用支付密钥、signature 用玩家 session_key）**绝不能放在客户端**（放客户端 = 密钥泄露 = 被刷单）。云函数就是替你保管密钥、代你签名的地方。

---

## 第 0 步：确认前置条件

1. 你的 CDN 已经跑在腾讯云开发（TCB）环境上（`https://xxx.tcb.qcloud.la`）——说明环境已开通，直接用这个环境即可。
2. 微信公众平台（mp.weixin.qq.com）已注册小游戏，且完成了**虚拟支付开户**（虚拟支付需要主体资质，在公众平台左侧「功能 - 虚拟支付」按引导开通）。

## 第 1 步：拿到三个关键参数

打开 **微信公众平台 → 功能 → 虚拟支付 → 基本配置**，抄下三样东西：

| 参数 | 在哪 | 填到哪 |
|---|---|---|
| `offerId` | 基本配置页 | 云函数 `OFFER_ID` |
| `AppKey`（支付密钥，现网） | 基本配置页 | 云函数 `APP_KEY` |
| `AppSecret` | 公众平台 → 开发 → 开发设置 | 云函数 `APP_SECRET` |

另外记下你的 **AppID**（开发设置页顶部），云函数 `APPID` 用。

## 第 2 步：配置道具

公众平台 → 功能 → 虚拟支付 → **道具管理** → 新建道具：

- 道具 ID 自定（如 `star_pack_small`、`remove_ad`），**抄下来**——后面要填到 Unity 的 PaymentConfigSO 里
- 道具价格（单位分）必须与 Unity 里配置的价格一致（如 ¥6 = 600 分）
- 每个礼包档位建一个道具

> 坑位提示：新建/修改道具约 **10 分钟**才生效，期间下单会报 `COIN_OR_PRODUCT_ID_CREATED_IN_RECENTLY`，等着就好。

## 第 3 步：部署 createOrder 云函数

1. 打开微信开发者工具 → 顶部「云开发」→ 进入你的环境。
2. 左侧「云函数」→ 新建 → 函数名 `createOrder`，运行环境 Node.js 16+。
3. 把本目录 `createOrder/index.js` 和 `createOrder/package.json` 的内容，覆盖到云端函数代码编辑器里（或在本地函数目录放这两个文件后右键「上传并部署：云端安装依赖」）。
4. **填配置**：打开 `index.js` 顶部，把 `APPID` / `APP_SECRET` / `OFFER_ID` / `APP_KEY` **四个占位全部**换成第 1 步抄的值——四项都是占位符，没有一项是预填的。
5. 部署（上传并部署：云端安装依赖）。

## 第 4 步：开启 HTTP 访问服务（拿 URL）

云函数默认只能从小程序内部调用，我们的 Unity 游戏是走 HTTP 的，所以要开 HTTP 访问：

1. 云开发控制台 → 设置 → **全局配置** → 「添加」HTTP 触发路径，填 `createOrder`（与函数同名）。
2. 保存后得到形如 `https://<环境ID>.service.tcloudbase.com/createOrder` 的地址——**抄下来**。
3. 回到 Unity：`PaymentConfigSO` 资产的 `下单签名云函数地址` 填入这个 URL。
4. 运行游戏 → 主界面左上角「商店」→ 买一单 → Console 里 `[Payment]` 日志显示签名请求成功即链路打通。

## 第 5 步：配置支付回调（发货通知，正式上线前必须做）

玩家付完钱，微信会把「发货通知」推给你。云开发做法：

1. 云开发控制台 → 「云函数」→ 新建 `paymentCallback` 函数（收到通知记录订单即可起步）。
2. 公众平台 → 开发 → 开发设置 → **消息推送**：启用，方式选「云函数」，环境选你的 TCB 环境，函数填 `paymentCallback`。
3. 勾选事件：`xpay_goods_deliver_notify`（发货）、`xpay_refund_notify`（退款）。
4. 在 `paymentCallback` 里按 `OutTradeNo`（= Unity 端 PayOrder.orderId）对账发货。本游戏是单机档，起步阶段「支付成功客户端即时到账 + 回调仅记流水」即可；将来做服务端账务时以回调为准。

> 单机游戏起步期的务实策略：客户端支付成功即发星星（当前实现），回调用来对账与排查纠纷——这就是休闲小游戏最常见的做法；要做强校验时再把发货逻辑挪到回调里。

## 第 6 步：真机验证清单

- [ ] 现网测试：`PaymentConfigSO.payEnv = 0`，用真实微信支付 1 分/1 元档道具试买（可自己在后台退款）
- [ ] 沙箱测试（可选）：`payEnv = 1` + 沙箱密钥，用沙箱测试账号支付
- [ ] 看 Console：`[WeChatPay] 支付完成 orderId=...` → [游戏道具]到账 / 去广告生效
- [ ] 公众平台 → 虚拟支付 → 订单中心，能查到这笔订单

## 常见报错速查

| 报错 | 原因 | 处理 |
|---|---|---|
| `使用本地调试签名` 警告 | PaymentConfigSO 没填云函数地址 | 第 4 步 |
| 签名请求失败/超时 | HTTP 访问服务没开或 URL 错 | 第 4 步 |
| `jscode2session 失败 40125` | AppSecret 错误/已重置 | 核对第 1 步 |
| `COIN_OR_PRODUCT_ID_CREATED_IN_RECENTLY` | 道具刚建未生效 | 等 10 分钟 |
| 支付面板报签名错误 | AppKey 与 env 不匹配（现网密钥配了沙箱）或道具价格与后台不一致 | 核对第 1/2 步 |

## 后续：升级新版支付接口（暂不需要）

当前转换 SDK（v0.1.34）用的是 `requestMidasPaymentGameItem`（旧版，仍在服务）。将来升级转换 SDK 后可切到新版 `wx.requestVirtualPayment`：客户端换调新接口，云函数只需把 `SIGN_URI` 改成 `'requestVirtualPayment'`，其余不变。
