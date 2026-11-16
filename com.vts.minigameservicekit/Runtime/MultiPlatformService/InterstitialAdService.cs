using System;
using UnityEngine;
using VTS.MiniGameService;

/// <summary>
/// 插屏广告频控服务：按 InterstitialPolicySO 的规则决定「关卡结算时要不要弹插屏」。
/// 状态（上次展示的关卡与时间）用包内 SaveService 持久化，跨会话有效。
/// 去广告等特权由游戏侧通过 SetAdFreePredicate 注入，包不关心游戏的存档结构。
/// </summary>
public class InterstitialAdService
{
    private const string SAVE_KEY = "__kit_interstitial_policy_state";

    private InterstitialPolicySO m_config;
    private Func<bool>           m_isAdFree;
    private InterstitialPolicyState m_state;
    private bool                 m_stateLoaded;

    /// <summary> 注入频控配置（CreateServices 内调用；传 null 表示关闭自动插屏） </summary>
    public void Configure(InterstitialPolicySO config)
    {
        m_config      = config;
        m_stateLoaded = false;
    }

    /// <summary>
    /// 注入「是否处于免广告状态」的判断（如已购买去广告特权）。
    /// 每次判定都会调用，游戏侧可在这里读自己的存档；不注入则视为从未购买。
    /// </summary>
    public void SetAdFreePredicate(Func<bool> isAdFree)
    {
        m_isAdFree = isAdFree;
    }

    /// <summary> 当前是否允许展示插屏（免广告状态下为 false） </summary>
    public bool AdsAllowed => !(m_isAdFree != null && m_isAdFree());

    /// <summary>
    /// 关卡结算（回主页）时调用：满足频控则自动展示插屏。
    /// 判定顺序：自动插屏开关 → 免广告 → 免打扰关卡 → 关卡间隔 → 时间间隔。
    /// </summary>
    /// <param name="levelId">刚完成结算的关卡 ID</param>
    /// <returns>本次是否展示了插屏</returns>
    public bool NotifyLevelSettled(int levelId)
    {
        if (m_config == null || !m_config.enable)
        {
            return false;
        }
        if (!AdsAllowed)
        {
            return false;
        }
        if (levelId <= m_config.firstNoAdLevels)
        {
            return false;
        }

        var state = EnsureState();

        if (state.lastLevelId > 0 && levelId - state.lastLevelId < m_config.intervalLevels)
        {
            return false;
        }
        if (state.lastUtcTicks > 0)
        {
            var lastTime = new DateTime(state.lastUtcTicks, DateTimeKind.Utc);
            if ((DateTime.UtcNow - lastTime).TotalSeconds < m_config.intervalSeconds)
            {
                return false;
            }
        }

        state.lastLevelId  = levelId;
        state.lastUtcTicks = DateTime.UtcNow.Ticks;
        SaveState(state);

        PlatformServices.AdService.ShowInterstitialAd(null);
        Debug.Log($"[InterstitialAd] 自动展示插屏（第 {levelId} 关结算）");
        return true;
    }

    /// <summary> 清空频控状态（调试用，清空后下一次结算即可再次展示） </summary>
    public void ResetState()
    {
        m_state       = new InterstitialPolicyState();
        m_stateLoaded = true;
        SaveState(m_state);
    }

    private InterstitialPolicyState EnsureState()
    {
        if (!m_stateLoaded)
        {
            m_state       = PlatformServices.SaveService.Load<InterstitialPolicyState>(SAVE_KEY)
                            ?? new InterstitialPolicyState();
            m_stateLoaded = true;
        }
        return m_state;
    }

    private void SaveState(InterstitialPolicyState state)
    {
        PlatformServices.SaveService.Save(SAVE_KEY, state);
    }
}

/// <summary> 插屏频控的持久化状态（包内私有用途，勿在游戏侧直接使用） </summary>
[Serializable]
public class InterstitialPolicyState
{
    /// <summary> 上次展示插屏的关卡 ID，0 表示从未展示 </summary>
    public int lastLevelId;
    /// <summary> 上次展示插屏的 UTC ticks，0 表示从未展示 </summary>
    public long lastUtcTicks;
}
