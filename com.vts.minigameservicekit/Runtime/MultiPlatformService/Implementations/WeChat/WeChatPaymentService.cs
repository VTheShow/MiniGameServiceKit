#if MINIGAME_SUBPLATFORM_WEIXIN

using System;
using UnityEngine;
using VTS.MiniGameService;
using WeChatWASM;

/// <summary>
/// 微信虚拟支付（虚拟支付 2.0 道具直购模式）：
/// - QueryPaymentSupport：WX.CheckIsSupportMidasPayment 查询当前环境是否可支付
/// - Pay：向签名提供者请求 signData/paySig/signature → WX.RequestMidasPaymentGameItem 拉起支付面板
/// 上线前提：微信后台开通「虚拟支付」并配置道具（见包 README 第六节「平台后台配置」）；
/// 支付成功后的发货由服务端支付回调完成（下单/签名云函数见 Samples~/Docs/cloudfunctions）。
/// </summary>
public class WeChatPaymentService : PaymentServiceBase
{
    public override bool IsPaymentSupported => true;

    public override void QueryPaymentSupport(Action<bool> callback)
    {
        WX.CheckIsSupportMidasPayment(new CheckIsSupportMidasPaymentOption
        {
            success = (res) =>
            {
                bool allowPay = res.data != null && res.data.allow_pay;
                Debug.Log($"[WeChatPay] 虚拟支付环境查询: allow_pay={allowPay}");
                callback?.Invoke(allowPay);
            },
            fail = (err) =>
            {
                Debug.LogWarning("[WeChatPay] 虚拟支付环境查询失败: " + err.errMsg);
                callback?.Invoke(false);
            },
        });
    }

    public override void Pay(PayOrder order, Action<PayResult> callback)
    {
        var product = GetProduct(order, callback);
        if (product == null)
        {
            return;
        }
        if (string.IsNullOrEmpty(product.wechatProductId))
        {
            Debug.LogError($"[WeChatPay] 商品 {order.productId} 未配置微信道具ID（PaymentConfigSO.wechatProductId）");
            callback?.Invoke(PayResult.Fail(order.orderId, -2, "商品未配置微信道具ID"));
            return;
        }

        // 用户态签名依赖 session_key：须用一次性 code 在服务端现换，
        // 因此支付前重新静默登录取新 code（code 有效期 5 分钟且 code2session 仅可用一次）
        PlatformServices.AuthService.Login((login) =>
        {
            if (!login.success)
            {
                Debug.LogWarning("[WeChatPay] 下单前登录失败: " + login.errMsg);
                callback?.Invoke(PayResult.Fail(order.orderId, -4, "登录失败，无法下单"));
                return;
            }

            m_signProvider.RequestGoodsSign(order, product, login.code, (sign) =>
            {
                if (!sign.success)
                {
                    callback?.Invoke(PayResult.Fail(order.orderId, -3, sign.errMsg));
                    return;
                }

                WX.RequestMidasPaymentGameItem(new RequestMidasPaymentGameItemOption
                {
                    signData  = sign.signData,
                    paySig    = sign.paySig,
                    signature = sign.signature,
                    success   = (res) =>
                    {
                        // 支付动作完成；发货以服务端支付回调为准
                        Debug.Log($"[WeChatPay] 支付完成 orderId={order.orderId} errCode={res.errCode}");
                        callback?.Invoke(PayResult.Ok(order.orderId));
                    },
                    fail = (err) =>
                    {
                        Debug.LogWarning($"[WeChatPay] 支付失败 orderId={order.orderId} errCode={err.errCode} errMsg={err.errMsg}");
                        callback?.Invoke(PayResult.Fail(order.orderId, Convert.ToInt32(err.errCode), err.errMsg));
                    },
                });
            });
        });
    }
}

#endif
