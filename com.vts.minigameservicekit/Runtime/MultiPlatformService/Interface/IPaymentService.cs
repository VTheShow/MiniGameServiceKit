using System;

/// <summary>
/// 支付服务：统一各小游戏平台的内购支付（微信虚拟支付道具直购 / 抖音游戏币支付等）。
/// 流程：构建订单 →（微信）向签名服务请求下单签名 → 拉起平台支付面板 → 回调支付结果。
/// 支付成功仅代表玩家完成支付动作，发货与订单校验以服务端回调为准（见 Samples~/Docs/cloudfunctions）。
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// 当前平台是否具备内购支付能力（静态能力：SDK/平台是否提供支付接口，
    /// 环境是否开通支付需再通过 QueryPaymentSupport 异步确认）
    /// </summary>
    bool IsPaymentSupported { get; }

    /// <summary> 查询当前环境是否可支付（微信需查询虚拟支付开通情况；不支持的平台直接回调 false） </summary>
    void QueryPaymentSupport(Action<bool> callback);

    /// <summary>
    /// 发起支付
    /// </summary>
    /// <param name="order">订单信息（商品ID/唯一订单号/金额[分]等）</param>
    /// <param name="callback">支付结果回调</param>
    void Pay(PayOrder order, Action<PayResult> callback);
}

/// <summary>
/// 支付订单（客户端构建；服务端凭 orderId 完成下单与对账发货）
/// </summary>
[Serializable]
public class PayOrder
{
    /// <summary> 唯一订单号：客户端本地生成，支付回调/对账的凭据 </summary>
    public string orderId;
    /// <summary> 商品ID（对应 PaymentConfigSO 中 PayProduct.productId） </summary>
    public string productId;
    /// <summary> 商品名（展示在平台支付面板） </summary>
    public string productName;
    /// <summary> 支付金额，单位：分 </summary>
    public int amount;
    /// <summary> 购买数量（道具直购通常为 1） </summary>
    public int buyQuantity = 1;
    /// <summary> 区服ID（无分区概念传 "1"） </summary>
    public string zoneId = "1";
    /// <summary> 支付环境：0=线上 1=沙箱（微信虚拟支付） </summary>
    public int env;
    /// <summary> 透传给服务端支付回调的附加数据 </summary>
    public string attach;

    /// <summary> 按商品配置生成订单（金额取配置价，orderId 自动生成） </summary>
    public static PayOrder Create(PayProduct product, int env)
    {
        return new PayOrder
        {
            orderId     = NewOrderId(),
            productId   = product.productId,
            productName = product.displayName,
            amount      = product.AmountInCents,
            buyQuantity = 1,
            zoneId      = "1",
            env         = env,
        };
    }

    /// <summary> 生成平台要求格式的订单号：32字符内、数字/大小写字母/_-|*、不以下划线开头 </summary>
    public static string NewOrderId()
    {
        return "O" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
                    + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
    }
}

/// <summary> 支付结果 </summary>
[Serializable]
public class PayResult
{
    public bool   success;
    public string orderId;
    public int    errCode;
    public string errMsg;

    public static PayResult Ok(string orderId) => new() { success = true, orderId = orderId };
    public static PayResult Fail(string orderId, int errCode, string errMsg)
        => new() { success = false, orderId = orderId, errCode = errCode, errMsg = errMsg };
}
