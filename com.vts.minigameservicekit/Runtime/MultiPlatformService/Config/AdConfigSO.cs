using System;
using System.Collections.Generic;
using UnityEngine;
using VTS.MiniGameKit;

[CreateAssetMenu(fileName = "AdConfigSO", menuName = "Scriptable Object/AdConfigSO")]
public class AdConfigSO : ScriptableObject
{
    public List<AdConfig> adConfigList = new();
    public AdConfig GetAdConfig(AdConfig.EAdPlatform adPlatform)
    {
        var res = adConfigList.Find(config => config.adPlatform == adPlatform);
        res ??= new();
        return res;
    }
}

[Serializable]
public class AdConfig
{
    [KitInspectorLabel("广告平台")]
    public EAdPlatform  adPlatform;
    [KitInspectorLabel("激励视频广告ID")]
    public string       videoAdUnitId;
    [KitInspectorLabel("横幅广告ID")]
    public string       bannerAdUnitId;
    [KitInspectorLabel("插屏广告ID")]
    public string       interstitialAdUnitId;

    public AdConfig()
    {
        adPlatform = EAdPlatform.Mock;
        videoAdUnitId = string.Empty;
        bannerAdUnitId = string.Empty;
        interstitialAdUnitId = string.Empty;
    }

    public enum EAdPlatform
    {
        [InspectorName("模拟器/编辑器")] Mock = 0,
        [InspectorName("微信小游戏")] WechatMiniGame,
        [InspectorName("抖音小游戏")] DouyinMiniGame,
    }
}
