using System;
using UnityEngine;

namespace VTS.MiniGameService
{
    /// <summary> SDK 初始化状态 </summary>
    public enum EPlatformInitState
    {
        /// <summary> 尚未初始化（或初始化中） </summary>
        NotInit = 0,
        /// <summary> 平台 SDK 初始化成功，服务为真实平台实现 </summary>
        Success,
        /// <summary> 初始化失败重试耗尽后降级为 Mock（游戏可玩，平台功能不可用） </summary>
        FallbackMock,
    }

    /// <summary>
    /// 平台服务入口（静态服务定位器 + 组装根）
    /// 职责：按编译宏创建各平台实现（CreateServices）、对外提供全局单例访问（AdService 等）、记录初始化状态。
    /// 当前接入平台：编辑器(Mock) / 微信小游戏(MINIGAME_SUBPLATFORM_WEIXIN) / 抖音小游戏(MINIGAME_SUBPLATFORM_DOUYIN)
    /// 抖音实现共用同一套 StarkSDK 公共实现（Implementations/Shared）；
    /// 接入新平台时：实现各服务子类（包对应平台宏）→ 在此处添加分支。
    /// 注意：每个分支引用的类都必须在该宏下可编译，切平台前先跑一次对应宏的编译。
    ///
    /// 健壮性约定（商业底线：SDK 失败不阻塞游戏）：
    /// - 初始化由游戏启动流程驱动：成功调 CreateServices()，重试耗尽调 FallbackToMock() 降级；
    /// - 服务属性 getter 兜底：未初始化时返回 Mock 实例，杜绝 NullReference 卡死启动流程。
    /// </summary>
    public static class PlatformServices
    {
        public static EPlatformInitState InitState { get; private set; } = EPlatformInitState.NotInit;

        /// <summary> 平台 SDK 是否以真实实现初始化成功（只读；降级状态为 false） </summary>
        public static bool IsInitSuccess => InitState == EPlatformInitState.Success;

        /// <summary> 当前是否处于降级模式（真实 SDK 不可用，平台功能走 Mock） </summary>
        public static bool IsFallbackMode => InitState == EPlatformInitState.FallbackMock;

        // 平台 SDK 初始化器：只负责拉起第三方 SDK，一般用 Initialize() 一步完成即可
        public static ISdkInitializer SdkInitializer { get; }

        // 业务服务：在SDK初始化成功后使用（getter 对未初始化情况兜底 Mock）
        public static AdServiceBase AdService         => m_adService         ??= new MockAdService();
        public static ISaveService SaveService        => m_saveService       ??= new MockSaveService();
        public static IAuthService AuthService        => m_authService       ??= new MockAuthService();
        public static ISocialService SocialService    => m_socialService     ??= new MockSocialService();
        public static IPaymentService PaymentService  => m_paymentService    ??= new MockPaymentService();
        public static IPlatformUtilityService UtilityService => m_utilityService ??= new MockUtilityService();
        public static LifecycleServiceBase LifecycleService => m_lifecycleService ??= new MockLifecycleService();
        public static IComplianceService ComplianceService => m_complianceService ??= new MockComplianceService();
        public static InterstitialAdService InterstitialAdService => m_interstitialAdService ??= new InterstitialAdService();

        private static AdServiceBase m_adService;
        private static ISaveService m_saveService;
        private static IAuthService m_authService;
        private static ISocialService m_socialService;
        private static IPaymentService m_paymentService;
        private static IPlatformUtilityService m_utilityService;
        private static LifecycleServiceBase m_lifecycleService;
        private static IComplianceService m_complianceService;
        private static InterstitialAdService m_interstitialAdService;

        static PlatformServices()
        {
#if UNITY_EDITOR
            SdkInitializer = new MockInitializer();
            Debug.Log("编辑器/模拟器平台初始化");
#elif MINIGAME_SUBPLATFORM_WEIXIN
            SdkInitializer = new WeChatInitializer();
            Debug.Log("微信小游戏平台初始化");
#elif MINIGAME_SUBPLATFORM_DOUYIN
            SdkInitializer = new DouYinInitializer();
            Debug.Log("抖音小游戏平台初始化");
#else
            SdkInitializer = new MockInitializer();
            Debug.Log("非小游戏平台，使用模拟初始化");
#endif
        }

        /// <summary>
        /// 一步完成平台接入：拉起平台 SDK → 创建并配置服务 → 失败按 maxAttempts 立即重试 → 耗尽则降级 Mock。
        /// 推荐用这个入口。需要「间隔重试」等自定义流程时，改用 SdkInitializer.Initialize + CreateServices 两步自行编排。
        /// </summary>
        /// <param name="maxAttempts">SDK 初始化总尝试次数（含首次），≤1 表示不重试</param>
        /// <param name="onReady">真实平台服务就绪（服务已创建、配置已注入）</param>
        /// <param name="onFallback">重试耗尽、已降级 Mock（存档与主流程可用，平台功能不可用）</param>
        public static void Initialize(AdConfigSO adConfig = null,
                                      PaymentConfigSO paymentConfig = null,
                                      ShareConfigSO shareConfig = null,
                                      PlatformEntryConfigSO entryConfig = null,
                                      InterstitialPolicySO interstitialPolicy = null,
                                      int maxAttempts = 1,
                                      Action onReady = null,
                                      Action<int> onFallback = null)
        {
            if (maxAttempts < 1)
            {
                maxAttempts = 1;
            }
            TryInitialize(1, maxAttempts, adConfig, paymentConfig, shareConfig, entryConfig, interstitialPolicy, onReady, onFallback);
        }

        private static void TryInitialize(int attempt, int maxAttempts,
                                          AdConfigSO adConfig, PaymentConfigSO paymentConfig,
                                          ShareConfigSO shareConfig, PlatformEntryConfigSO entryConfig,
                                          InterstitialPolicySO interstitialPolicy,
                                          Action onReady, Action<int> onFallback)
        {
            SdkInitializer.Initialize(
                onSuccess: () =>
                {
                    CreateServices(adConfig, paymentConfig, shareConfig, entryConfig, interstitialPolicy);
                    onReady?.Invoke();
                },
                onFailed: errCode =>
                {
                    if (attempt < maxAttempts)
                    {
                        Debug.LogWarning($"[PlatformServices] 平台 SDK 初始化失败（第 {attempt}/{maxAttempts} 次，code={errCode}），立即重试");
                        TryInitialize(attempt + 1, maxAttempts, adConfig, paymentConfig, shareConfig, entryConfig, interstitialPolicy, onReady, onFallback);
                        return;
                    }

                    Debug.LogError($"[PlatformServices] 平台 SDK 初始化失败（已尝试 {attempt} 次，code={errCode}），降级为 Mock");
                    FallbackToMock();
                    onFallback?.Invoke(errCode);
                });
        }

        /// <summary>
        /// 按当前平台创建全套业务服务并注入配置（SDK 初始化成功后由游戏启动流程调用一次）。
        /// 创建（new）与配置注入（Configure）在此一步完成；配置传 null 时跳过对应注入（广告/支付不可用，其余服务正常）。
        /// </summary>
        /// <param name="adConfig">广告位配置（来自游戏侧 AdConfigSO 资产）</param>
        /// <param name="paymentConfig">支付商品配置（来自游戏侧 PaymentConfigSO 资产）</param>
        /// <param name="shareConfig">分享素材配置（来自游戏侧 ShareConfigSO 资产），注入后自动设默认转发/朋友圈/微信客服参数</param>
        /// <param name="entryConfig">平台入口开关配置（来自游戏侧 PlatformEntryConfigSO 资产），注入后游戏圈/客服/关注等入口按开关生效</param>
        /// <param name="interstitialPolicy">插屏频控配置（来自游戏侧 InterstitialPolicySO 资产），注入后可用 InterstitialAdService.NotifyLevelSettled 自动弹插屏</param>
        public static void CreateServices(AdConfigSO adConfig = null,
                                       PaymentConfigSO paymentConfig = null,
                                       ShareConfigSO shareConfig = null,
                                       PlatformEntryConfigSO entryConfig = null,
                                       InterstitialPolicySO interstitialPolicy = null)
        {
#if UNITY_EDITOR
            //编辑器或PC端，使用模拟实现
            m_adService         = new MockAdService();
            m_saveService       = new MockSaveService();
            m_authService       = new MockAuthService();
            m_socialService     = new MockSocialService();
            m_paymentService    = new MockPaymentService();
            m_utilityService    = new MockUtilityService();
            m_lifecycleService  = new MockLifecycleService();
            m_complianceService = new MockComplianceService();
#elif MINIGAME_SUBPLATFORM_WEIXIN
            m_adService         = new WeChatAdService();
            m_saveService       = new WeChatSaveService();
            m_authService       = new WeChatAuthService();
            m_socialService     = new WeChatSocialService();
            m_paymentService    = new WeChatPaymentService();
            m_utilityService    = new WeChatUtilityService();
            m_lifecycleService  = new WeChatLifecycleService();
            m_complianceService = new WeChatComplianceService();
#elif MINIGAME_SUBPLATFORM_DOUYIN
            m_adService         = new DouYinAdService();
            m_saveService       = new DouYinSaveService();
            m_authService       = new DouYinAuthService();
            m_socialService     = new DouYinSocialService();
            m_paymentService    = new DouYinPaymentService();
            m_utilityService    = new DouYinUtilityService();
            m_lifecycleService  = new DouYinLifecycleService();
            m_complianceService = new DouYinComplianceService();
#else
            //非编辑器且非小游戏平台的其他构建，退化为模拟实现
            m_adService         = new MockAdService();
            m_saveService       = new MockSaveService();
            m_authService       = new MockAuthService();
            m_socialService     = new MockSocialService();
            m_paymentService    = new MockPaymentService();
            m_utilityService    = new MockUtilityService();
            m_lifecycleService  = new MockLifecycleService();
            m_complianceService = new MockComplianceService();
#endif
            // 配置注入（创建之后、对外可用之前）：由工厂统一完成，调用方无需逐服务 Configure
            if (adConfig != null)
            {
                AdService.Configure(adConfig);
            }
            if (paymentConfig != null && m_paymentService is PaymentServiceBase payService)
            {
                payService.Configure(paymentConfig);
            }
            if (shareConfig != null)
            {
                SocialService.Configure(shareConfig);
            }
            // 入口开关：传 null 表示不启用开关控制（服务照常响应，保持旧行为）
            PlatformEntry.Configure(entryConfig);
            if (interstitialPolicy != null)
            {
                InterstitialAdService.Configure(interstitialPolicy);
            }

            InitState = EPlatformInitState.Success;
        }

        /// <summary>
        /// 降级为 Mock 服务（SDK 初始化重试耗尽后由游戏启动流程调用）：
        /// 保证存档（PlayerPrefs）与游戏流程可用；广告/支付仅编辑器弹模拟面板，
        /// 真机上安静失败（不产生假成功），登录等其余服务走 Mock 行为
        /// </summary>
        public static void FallbackToMock()
        {
            m_adService         = new MockAdService();
            m_saveService       = new MockSaveService();
            m_authService       = new MockAuthService();
            m_socialService     = new MockSocialService();
            m_paymentService    = new MockPaymentService();
            m_utilityService    = new MockUtilityService();
            m_lifecycleService  = new MockLifecycleService();
            m_complianceService = new MockComplianceService();
            InitState = EPlatformInitState.FallbackMock;
            Debug.LogWarning("[PlatformService] 已降级为 Mock 平台服务（SDK 初始化失败），平台功能不可用但游戏可玩");
        }
    }
}
