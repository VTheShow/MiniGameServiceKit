// 云函数：createOrder —— 微信小游戏虚拟支付（道具直购）下单签名
// ----------------------------------------------------------------------------
// 作用：接收客户端支付请求 → 用一次性 code 换 openid/session_key →
//       构造 signData 并计算 paySig / signature → 返回客户端拉起支付面板。
// 客户端对应：PaymentConfigSO.cloudFunctionUrl 指向本函数的 HTTP 访问地址。
// 部署教程：Samples~/Docs/cloudfunctions/README-云函数部署教程.md
// ----------------------------------------------------------------------------

const crypto = require('crypto');
const https = require('https');

// ══════════════════ 【必填】支付配置（见部署教程第 2 步） ══════════════════
const APPID       = '在这里填入你的AppID';   // 小游戏 AppID（微信公众平台-开发-开发设置）
const APP_SECRET  = '在这里填入你的AppSecret';  // 小游戏 AppSecret（同页面）
const OFFER_ID    = '在这里填入虚拟支付offerId'; // 微信公众平台-功能-虚拟支付-基本配置
const APP_KEY     = '在这里填入支付密钥AppKey'; // 同上页面（现网密钥；沙箱用沙箱密钥）
// ═══════════════════════════════════════════════════════════════════════════

// 签名 URI：客户端拉起接口名。当前转换 SDK 使用旧版接口 requestMidasPaymentGameItem；
// 将来升级转换 SDK 改用 wx.requestVirtualPayment 时，这里同步改为 'requestVirtualPayment'
const SIGN_URI = 'requestMidasPaymentGameItem';

function hmacSha256Hex(key, data) {
  return crypto.createHmac('sha256', key).update(data, 'utf8').digest('hex');
}

// 用一次性 code 换 openid / session_key（code2session）
function jscode2session(jsCode) {
  const url = `https://api.weixin.qq.com/sns/jscode2session` +
              `?appid=${APPID}&secret=${APP_SECRET}&js_code=${jsCode}&grant_type=authorization_code`;
  return new Promise((resolve, reject) => {
    https.get(url, (res) => {
      let body = '';
      res.on('data', (chunk) => { body += chunk; });
      res.on('end', () => {
        try {
          const data = JSON.parse(body);
          if (data.errcode) {
            reject(new Error(`jscode2session 失败: ${data.errcode} ${data.errmsg}`));
          } else {
            resolve(data); // { openid, session_key, unionid? }
          }
        } catch (e) {
          reject(new Error('jscode2session 响应解析失败: ' + e.message));
        }
      });
    }).on('error', reject);
  });
}

exports.main = async (event) => {
  // 客户端 POST body：{ action, productId, platformSku, productName, orderId,
  //                    amount, buyQuantity, zoneId, env, attach, loginCode }
  if (!event || event.action !== 'createOrder') {
    return { errCode: -1, errMsg: 'unknown action' };
  }

  try {
    // 1. 换取用户态（session_key 用于用户态签名）
    const session = await jscode2session(event.loginCode);

    // 2. 构造 signData（原样返回客户端透传，客户端/服务端都不得重新序列化）
    const signData = JSON.stringify({
      mode: 'goods',
      offerId: OFFER_ID,
      buyQuantity: event.buyQuantity || 1,
      env: event.env || 0,
      currencyType: 'CNY',
      platform: 'android',
      zoneId: event.zoneId || '1',
      productId: event.platformSku,   // 微信后台配置的道具ID
      goodsPrice: event.amount,       // 单位：分（须与后台道具定价一致）
      outTradeNo: event.orderId,      // 业务订单号（回调对账凭据）
      attach: event.attach || '',
    });

    // 3. 双签名
    const paySig = hmacSha256Hex(APP_KEY, `${SIGN_URI}&${signData}`);
    const signature = hmacSha256Hex(session.session_key, signData);

    // 4. 返回（客户端 CloudFunctionSignProvider 按这些字段名解析）
    return { errCode: 0, signData, paySig, signature };
  } catch (e) {
    console.error('[createOrder] 失败:', e.message);
    return { errCode: -2, errMsg: e.message };
  }
};
