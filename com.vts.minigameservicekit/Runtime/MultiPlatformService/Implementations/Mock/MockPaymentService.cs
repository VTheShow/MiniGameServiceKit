using System;
using UnityEngine;

/// <summary>
/// Mock 支付服务：
/// - 编辑器：弹出模拟支付面板，完整走通下单 / 回调流程（真机看不到这些 UI）
/// - 真机降级：一律不支持支付——杜绝玩家「模拟支付成功」白拿商品
/// </summary>
public class MockPaymentService : PaymentServiceBase
{
#if UNITY_EDITOR
    private static MockPayController s_controller;
    private MockPayController Controller
    {
        get
        {
            if (s_controller == null)
            {
                s_controller = MockPayController.CreateObject();
                s_controller.Init();
            }
            return s_controller;
        }
    }

    public override bool IsPaymentSupported => true;

    public override void Pay(PayOrder order, Action<PayResult> callback)
    {
        var product = GetProduct(order, callback);
        if (product == null)
        {
            return;
        }

        Controller.Show(order.productName, order.amount, (paySuccess) =>
        {
            if (paySuccess)
            {
                Debug.Log($"[Mock] 模拟支付成功: {order.productId} ¥{order.amount / 100f:0.00} orderId={order.orderId}");
                callback?.Invoke(PayResult.Ok(order.orderId));
            }
            else
            {
                Debug.Log("[Mock] 模拟支付取消");
                callback?.Invoke(PayResult.Fail(order.orderId, -100, "用户取消"));
            }
        });
    }
#else
    // 真机降级：SDK 初始化失败后游戏仍可玩，但支付彻底不可用
    public override bool IsPaymentSupported => false;

    public override void Pay(PayOrder order, Action<PayResult> callback)
    {
        Debug.LogWarning("[MockPay] 真机降级模式：支付不可用");
        callback?.Invoke(PayResult.Fail(order.orderId, -1000, "平台支付服务不可用（已降级 Mock）"));
    }
#endif
}
