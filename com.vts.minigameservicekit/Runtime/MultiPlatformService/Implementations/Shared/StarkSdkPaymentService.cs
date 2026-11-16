#if MINIGAME_SUBPLATFORM_DOUYIN

using System;
using System.Collections.Generic;
using UnityEngine;
using TTSDK;

/// <summary>
/// 抖音平台支付服务：道具直购模式（goodType=2）。
/// - 抖音：TTPayService.RequestGamePayment，CustomId 携带订单号，支付结果经服务端支付回调回传发货
///   （下单无需客户端签名，签名密钥在服务端回调验签时使用）
/// 上线前提：抖音后台「支付」中配置道具/礼包并接入支付回调（见包 README 第六节）。
/// </summary>
public abstract class StarkSdkPaymentServiceBase : PaymentServiceBase
{
    public override bool IsPaymentSupported => true;

    public override void Pay(PayOrder order, Action<PayResult> callback)
    {
        var product = GetProduct(order, callback);
        if (product == null)
        {
            return;
        }
        if (!IsPaymentSupported)
        {
            callback?.Invoke(PayResult.Fail(order.orderId, -1, "当前平台/SDK 版本不支持支付"));
            return;
        }
        if (string.IsNullOrEmpty(product.douyinGoodsId))
        {
            Debug.LogError($"[StarkPay] 商品 {order.productId} 未配置抖音道具ID（PaymentConfigSO.douyinGoodsId）");
            callback?.Invoke(PayResult.Fail(order.orderId, -2, "商品未配置抖音道具ID"));
            return;
        }

        // 道具直购参数语义见 tt.requestGamePayment 文档：
        // mode/env/currencyType/platform 为固定值；goodType=2 道具直购；
        // orderAmount 单位分；customId 为自定义订单号（服务端回调回传，发货对账凭据）
        var param = new Dictionary<string, object>
        {
            ["mode"]         = "game",
            ["env"]          = "0",
            ["currencyType"] = "CNY",
            ["platform"]     = "android",
            ["goodType"]     = 2,
            ["goodsId"]      = product.douyinGoodsId,
            ["goodName"]     = order.productName,
            ["orderAmount"]  = order.amount,
            ["buyQuantity"]  = order.buyQuantity,
            ["customId"]     = order.orderId,
            ["zoneId"]       = order.zoneId,
            ["extraInfo"]    = order.attach ?? string.Empty,
        };

        // StarkSDK 6.9.5：支付入口收敛到 TT 静态门面（TTPayService.RequestGamePayment 为实例成员已不可直接调）；
        // Dictionary 参数重载与 6.7.5 文档一致
        TT.RequestGamePayment(
            param,
            () =>
            {
                // 支付动作完成；发货以服务端支付回调为准
                Debug.Log($"[StarkPay] 支付完成 orderId={order.orderId}");
                callback?.Invoke(PayResult.Ok(order.orderId));
            },
            (code, errMsg) =>
            {
                Debug.LogWarning($"[StarkPay] 支付失败 orderId={order.orderId} code={code} msg={errMsg}");
                callback?.Invoke(PayResult.Fail(order.orderId, code, errMsg));
            });
    }
}

#endif
