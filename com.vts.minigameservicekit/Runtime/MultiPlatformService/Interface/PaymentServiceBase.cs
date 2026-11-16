using System;
using UnityEngine;

/// <summary>
/// 支付服务基类：注入商品配置并创建签名提供者（与 AdServiceBase.Configure 同模式）。
/// 微信虚拟支付要求 signData/paySig/signature 由服务端生成，签名来源由 PaymentConfigSO 决定
/// （配置了云函数地址走 CloudFunctionSignProvider，否则退化为本地调试签名）。
/// </summary>
public abstract class PaymentServiceBase : IPaymentService
{
    protected PaymentConfigSO    m_config;
    protected IPaymentSignProvider m_signProvider;

    public abstract bool IsPaymentSupported { get; }

    public virtual void QueryPaymentSupport(Action<bool> callback) => callback?.Invoke(IsPaymentSupported);

    public abstract void Pay(PayOrder order, Action<PayResult> callback);

    /// <summary> 注入支付配置（服务创建后由启动流程调用一次；与 CreateServices 创建实例是两个阶段：先创建、后 Configure） </summary>
    public void Configure(PaymentConfigSO config)
    {
        m_config      = config;
        m_signProvider = CreateSignProvider(config);

        if (m_config == null || m_config.products.Count == 0)
        {
            Debug.LogWarning("[Payment] PaymentConfigSO 未配置或商品列表为空，支付将不可用");
            return;
        }
        if (string.IsNullOrEmpty(m_config.cloudFunctionUrl))
        {
            Debug.LogWarning("[Payment] 未配置下单签名云函数地址，将使用本地调试签名：" +
                             "编辑器/沙箱可走通流程，真机正式支付会失败（部署见 Samples~/Docs/cloudfunctions）");
        }
    }

    /// <summary> 平台可重写签名来源（如后续接入自有后端 HTTP 签名服务） </summary>
    protected virtual IPaymentSignProvider CreateSignProvider(PaymentConfigSO config)
    {
        return string.IsNullOrEmpty(config?.cloudFunctionUrl)
            ? new LocalDebugSignProvider()
            : new CloudFunctionSignProvider(config.cloudFunctionUrl);
    }

    /// <summary> 取商品配置；未找到时回调失败并返回 null </summary>
    protected PayProduct GetProduct(PayOrder order, Action<PayResult> callback)
    {
        var product = m_config?.GetProduct(order.productId);
        if (product == null)
        {
            Debug.LogError($"[Payment] 商品未配置: {order.productId}（检查 PaymentConfigSO）");
            callback?.Invoke(PayResult.Fail(order.orderId, -1, "商品未配置"));
        }
        return product;
    }
}
