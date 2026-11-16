using System;
using System.Collections.Generic;
using UnityEngine;
using VTS.MiniGameKit;

/// <summary>
/// 内购商品与支付环境配置。资产放在游戏工程的任意目录即可（包不限制路径），
/// 各平台后台需要配置的道具/商品 ID 见包 README 第六节「平台后台配置」。
/// </summary>
[CreateAssetMenu(fileName = "PaymentConfigSO", menuName = "Scriptable Object/PaymentConfigSO")]
public class PaymentConfigSO : ScriptableObject
{
    [KitInspectorLabel("商品列表")]
    public List<PayProduct> products = new();

    [KitInspectorLabel("下单签名云函数地址")]
    [Tooltip("微信虚拟支付的下单/签名云函数 HTTP 触发地址；留空时使用本地调试签名（真机支付会失败，仅用于编辑器联调）。部署见 Samples~/Docs/cloudfunctions")]
    public string cloudFunctionUrl = string.Empty;

    [KitInspectorLabel("支付环境(0=线上, 1=沙箱)")]
    [Tooltip("微信虚拟支付环境；沙箱环境仅特定测试账号可用")]
    public int payEnv = 0;

    public PayProduct GetProduct(string productId)
    {
        return products.Find(p => p.productId == productId);
    }
}

/// <summary> 内购商品定义（游戏内部商品ID + 各平台后台的道具ID映射） </summary>
[Serializable]
public class PayProduct
{
    [KitInspectorLabel("商品ID(游戏内部)")]
    [Tooltip("订单与发货使用的内部商品ID，如 star_pack_small")]
    public string productId;

    [KitInspectorLabel("商品名")]
    public string displayName;

    [KitInspectorLabel("价格(元)")]
    public float priceYuan = 6f;

    [KitInspectorLabel("商品类型")]
    public EPayProductType type = EPayProductType.StarPack;

    [KitInspectorLabel("到账星星数(星星礼包生效)")]
    public int starAmount;

    [KitInspectorLabel("微信道具ID")]
    [Tooltip("微信小游戏后台「虚拟支付-道具直购」中配置的道具ID")]
    public string wechatProductId;

    [KitInspectorLabel("抖音道具ID")]
    [Tooltip("抖音小游戏后台「支付-道具/礼包」中配置的道具ID")]
    public string douyinGoodsId;

    /// <summary> 金额换算为分 </summary>
    public int AmountInCents => Mathf.RoundToInt(priceYuan * 100f);
}

public enum EPayProductType
{
    [InspectorName("星星礼包")] StarPack = 0,
    [InspectorName("去广告特权")] RemoveAd,
    [InspectorName("自定义")] Custom,
}
