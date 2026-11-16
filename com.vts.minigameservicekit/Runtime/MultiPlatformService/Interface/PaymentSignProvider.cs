using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 支付签名提供者：微信虚拟支付要求 signData / paySig / signature 由服务端生成
/// （paySig = HMAC-SHA256(米大师AppKey, "requestMidasPaymentGameItem&" + signData)，
///  signature 以 session_key 为密钥，两者都必须在服务端计算），客户端支付前通过本接口请求。
/// 抖音道具直购不需要签名，实现可直接跳过本环节。
/// 云函数示例见 Samples~/Docs/cloudfunctions。
/// </summary>
public interface IPaymentSignProvider
{
    /// <summary>
    /// 请求道具直购下单签名
    /// </summary>
    /// <param name="order">订单信息</param>
    /// <param name="product">商品配置</param>
    /// <param name="loginCode">支付前现取的登录 code（服务端用它换 session_key 计算用户态签名；无用时传空）</param>
    /// <param name="callback">签名结果回调</param>
    void RequestGoodsSign(PayOrder order, PayProduct product, string loginCode, Action<PaymentSignResult> callback);
}

/// <summary> 下单签名结果（与云函数返回字段一一对应） </summary>
[Serializable]
public class PaymentSignResult
{
    public bool   success;
    /// <summary> 支付原串（后端生成，原样透传给平台支付 API） </summary>
    public string signData;
    /// <summary> 支付签名 </summary>
    public string paySig;
    /// <summary> 用户态签名 </summary>
    public string signature;
    public string errMsg;
}

/// <summary>
/// 云函数实现：POST 订单信息到云函数 HTTP 触发地址，
/// 约定返回 JSON：{"errCode":0, "signData":"...", "paySig":"...", "signature":"..."}
/// </summary>
public class CloudFunctionSignProvider : IPaymentSignProvider
{
    private readonly string m_url;

    public CloudFunctionSignProvider(string url)
    {
        m_url = url;
    }

    public void RequestGoodsSign(PayOrder order, PayProduct product, string loginCode, Action<PaymentSignResult> callback)
    {
        var body = new JObject
        {
            ["action"]      = "createOrder",
            ["productId"]   = order.productId,
            ["platformSku"] = product.wechatProductId,
            ["productName"] = order.productName,
            ["orderId"]     = order.orderId,
            ["amount"]      = order.amount,
            ["buyQuantity"] = order.buyQuantity,
            ["zoneId"]      = order.zoneId,
            ["env"]         = order.env,
            ["attach"]      = order.attach ?? string.Empty,
            ["loginCode"]   = loginCode ?? string.Empty,
        };
        SignRequestRunner.PostJson(m_url, body.ToString(), (response, error) =>
        {
            if (error != null)
            {
                callback?.Invoke(new PaymentSignResult { success = false, errMsg = "请求下单签名失败: " + error });
                return;
            }
            try
            {
                var json = JObject.Parse(response);
                int errCode = json.Value<int?>("errCode") ?? 0;
                if (errCode != 0)
                {
                    callback?.Invoke(new PaymentSignResult
                    {
                        success = false,
                        errMsg  = "下单失败: " + (json.Value<string>("errMsg") ?? errCode.ToString()),
                    });
                    return;
                }
                callback?.Invoke(new PaymentSignResult
                {
                    success   = true,
                    signData  = json.Value<string>("signData"),
                    paySig    = json.Value<string>("paySig"),
                    signature = json.Value<string>("signature"),
                });
            }
            catch (Exception e)
            {
                callback?.Invoke(new PaymentSignResult { success = false, errMsg = "解析下单签名响应失败: " + e.Message });
            }
        });
    }
}

/// <summary>
/// 本地调试实现：客户端自拼 signData、签名为假值。
/// 仅用于编辑器/Mock 流程联调；真机正式支付会被微信拒绝（签名校验不过）。
/// </summary>
public class LocalDebugSignProvider : IPaymentSignProvider
{
    public void RequestGoodsSign(PayOrder order, PayProduct product, string loginCode, Action<PaymentSignResult> callback)
    {
        Debug.LogWarning("[Payment] 使用本地调试签名（真机正式支付会失败）。部署下单云函数并在 PaymentConfigSO.cloudFunctionUrl 填入地址后自动切换");
        var signData = new JObject
        {
            ["mode"]         = "goods",
            ["offerId"]      = product.wechatProductId,
            ["buyQuantity"]  = order.buyQuantity,
            ["env"]          = order.env,
            ["currencyType"] = "CNY",
            ["platform"]     = "android",
            ["zoneId"]       = order.zoneId,
            ["productId"]    = product.wechatProductId,
            ["goodsPrice"]   = order.amount,
            ["outTradeNo"]   = order.orderId,
            ["attach"]       = order.attach ?? string.Empty,
        }.ToString(Newtonsoft.Json.Formatting.None);
        callback?.Invoke(new PaymentSignResult
        {
            success   = true,
            signData  = signData,
            paySig    = "local_debug_sign",
            signature = "local_debug_signature",
        });
    }
}

/// <summary> UnityWebRequest 协程宿主（服务为纯 C# 类，无法自行发起协程） </summary>
internal class SignRequestRunner : MonoBehaviour
{
    private static SignRequestRunner m_instance;

    public static void PostJson(string url, string json, Action<string, string> callback)
    {
        if (m_instance == null)
        {
            var go = new GameObject("PaymentSignRequestRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            m_instance = go.AddComponent<SignRequestRunner>();
        }
        m_instance.StartCoroutine(m_instance.PostJsonRoutine(url, json, callback));
    }

    private IEnumerator PostJsonRoutine(string url, string json, Action<string, string> callback)
    {
        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 15;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                callback?.Invoke(null, req.error);
            }
            else
            {
                callback?.Invoke(req.downloadHandler.text, null);
            }
        }
    }
}
