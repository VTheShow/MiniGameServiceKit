using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 模拟广告面板只存在于编辑器；真机降级时绝不能给玩家看到假广告 UI
#if UNITY_EDITOR
public class MockAdController : MonoBehaviour
{
    private GameObject      m_bannerAdGO;
    private GameObject      m_interstitialAdGO;
    private GameObject      m_videoAdGO;

    private Button          m_bannerCloseBtn;
    private Button          m_videoAdCloseBtn;
    private Button          m_interstitialCloseBtn;
    
    private Button          m_videoAdRewardBtn;

    private RectTransform   m_bannerRT;


    Action<bool, int>   onVideoAdClose;
    Action              onInterstitialAdClose;
    Action              onBannerAdClose;

    public void Init()
    {
        DontDestroyOnLoad(gameObject);
        m_interstitialCloseBtn.onClick.AddListener(InterstitialCloseBtnClicked);
        m_videoAdCloseBtn.onClick.AddListener(VideoAdCloseBtnClicked);
        m_videoAdRewardBtn.onClick.AddListener(VideoAdRewardBtnClicked);
        m_bannerCloseBtn.onClick.AddListener(HideBannerAD);
    }

    public static MockAdController CreateObject()
    {
        Color bgColor = new(0.15f, 0.1f, 0.1f, 1f);
        Color mainColor = new(0.3f, 0.4f, 0.85f, 1f);
        Color btnClor = new(1f,0.6f, 0.125f, 1f);

        GameObject adCanvasGO = new ("MOCK AD CANVAS");

        Canvas canvas = adCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler canvasScaler = adCanvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1080, 1920);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        GraphicRaycaster raycaster = adCanvasGO.AddComponent<GraphicRaycaster>();
        raycaster.ignoreReversedGraphics = true;
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

        // Banner AD
        GameObject bannerAdGO = new("BannerGO");
        bannerAdGO.SetActive(false);

        RectTransform bannerRT = bannerAdGO.AddComponent<RectTransform>();
        bannerRT.SetParent(adCanvasGO.transform);
        bannerRT.anchorMin = new(0, 1);
        bannerRT.anchorMax = new(0, 1);
        bannerRT.pivot = new(0, 1);
        bannerRT.sizeDelta = new(360, 120);
        bannerRT.anchoredPosition = Vector2.zero;

        Image bannerImg = bannerAdGO.AddComponent<Image>();
        bannerImg.color = mainColor;

        GameObject bannerTextGO = new("Text");

        RectTransform bannerTextRT = bannerTextGO.AddComponent<RectTransform>();
        bannerTextRT.SetParent(bannerRT);
        bannerTextRT.anchorMin = Vector2.zero;
        bannerTextRT.anchorMax = Vector2.one;
        bannerTextRT.pivot = new(0.5f, 0.5f);
        bannerTextRT.sizeDelta = Vector2.zero;
        bannerTextRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI bannerText = bannerTextGO.AddComponent<TextMeshProUGUI>();
        bannerText.text = "MOCK BANNER ADS";
        bannerText.alignment = TextAlignmentOptions.Converted;
        bannerText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        bannerText.verticalAlignment = VerticalAlignmentOptions.Geometry;
        bannerText.fontSize = 30f;
        bannerText.raycastTarget = false;
        bannerText.fontWeight = FontWeight.Bold;

        GameObject bannerCloseBtnGO = new("CloseBtn");
        RectTransform bannerCloseBtnRT = bannerCloseBtnGO.AddComponent<RectTransform>();
        bannerCloseBtnRT.SetParent(bannerRT);
        bannerCloseBtnRT.anchorMin = Vector2.one;
        bannerCloseBtnRT.anchorMax = Vector2.one;
        bannerCloseBtnRT.pivot = Vector2.one;
        bannerCloseBtnRT.sizeDelta = new(50, 50);
        bannerCloseBtnRT.anchoredPosition = Vector2.zero;

        Image bannerCloseBtnImg = bannerCloseBtnGO.AddComponent<Image>();
        bannerCloseBtnImg.color = btnClor;

        Button bannerCloseBtn = bannerCloseBtnGO.AddComponent<Button>();
        bannerCloseBtn.targetGraphic = bannerCloseBtnImg;

        GameObject bannerCloseBtnTextGO = new("Text");

        RectTransform bannerCloseBtnTextRT = bannerCloseBtnTextGO.AddComponent<RectTransform>();
        bannerCloseBtnTextRT.SetParent(bannerCloseBtnRT);
        bannerCloseBtnTextRT.anchorMin = Vector2.zero;
        bannerCloseBtnTextRT.anchorMax = Vector2.one;
        bannerCloseBtnTextRT.pivot = new(0.5f, 0.5f);
        bannerCloseBtnTextRT.sizeDelta = Vector2.zero;
        bannerCloseBtnTextRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI bannerCloseBtnText = bannerCloseBtnTextGO.AddComponent<TextMeshProUGUI>();
        bannerCloseBtnText.text = "X";
        bannerCloseBtnText.alignment = TextAlignmentOptions.Converted;
        bannerCloseBtnText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        bannerCloseBtnText.verticalAlignment = VerticalAlignmentOptions.Geometry;
        bannerCloseBtnText.fontSize = 36f;
        bannerCloseBtnText.raycastTarget = false;
        bannerCloseBtnText.fontWeight = FontWeight.Bold;

        // Interstitial AD

        GameObject interstitialAdGO = new("InstertitialGO");
        interstitialAdGO.SetActive(false);

        RectTransform interstitialRT = interstitialAdGO.AddComponent<RectTransform>();
        interstitialRT.SetParent(adCanvasGO.transform);
        interstitialRT.anchorMin = Vector2.zero;
        interstitialRT.anchorMax = Vector2.one;
        interstitialRT.pivot = new(0.5f, 0.5f);
        interstitialRT.sizeDelta = Vector2.zero;
        interstitialRT.anchoredPosition = Vector2.zero;

        Image interstitialImg = interstitialAdGO.AddComponent<Image>();
        interstitialImg.color = mainColor;

        GameObject intersTitleTextGO = new("Title");
        RectTransform interTitleTextRT = intersTitleTextGO.AddComponent<RectTransform>();
        interTitleTextRT.SetParent(interstitialRT);
        interTitleTextRT.anchorMin = new(0, 0.5f);
        interTitleTextRT.anchorMax = new(1, 0.5f);
        interTitleTextRT.pivot = new(0.5f, 0.5f);
        interTitleTextRT.sizeDelta = new(0, 100f);
        interTitleTextRT.anchoredPosition = new(0, 200f);

        TextMeshProUGUI interstitialText = intersTitleTextGO.AddComponent<TextMeshProUGUI>();
        interstitialText.text = "MOCK INTERSTITIAL ADS";
        interstitialText.alignment = TextAlignmentOptions.Converted;
        interstitialText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        interstitialText.verticalAlignment = VerticalAlignmentOptions.Geometry;
        interstitialText.fontSize = 62f;
        interstitialText.raycastTarget = false;
        interstitialText.fontWeight = FontWeight.Bold;

        GameObject intersCloseBtnGO = new ("CloseBtn");
        RectTransform intersCloseBtnRT = intersCloseBtnGO.AddComponent<RectTransform>();
        intersCloseBtnRT.SetParent(interstitialRT);
        intersCloseBtnRT.anchorMin = new(0.5f, 0);
        intersCloseBtnRT.anchorMax = new(0.5f, 0);
        intersCloseBtnRT.pivot = new(0.5f, 0);
        intersCloseBtnRT.sizeDelta = new(600f, 200f);
        intersCloseBtnRT.anchoredPosition = new(0, 225f);

        Image intersCloseBtnImg = intersCloseBtnGO.AddComponent<Image>();
        intersCloseBtnImg.color = btnClor;

        Button intersCloseBtn = intersCloseBtnGO.AddComponent<Button>();
        intersCloseBtn.targetGraphic = intersCloseBtnImg;

        GameObject intersCloseBtnTextGO = new("Text");

        RectTransform intersCloseBtnTextRT = intersCloseBtnTextGO.AddComponent<RectTransform>();
        intersCloseBtnTextRT.SetParent(intersCloseBtnRT);
        intersCloseBtnTextRT.anchorMin = Vector2.zero;
        intersCloseBtnTextRT.anchorMax = Vector2.one;
        intersCloseBtnTextRT.pivot = new Vector2(0.5f, 0.5f);
        intersCloseBtnTextRT.sizeDelta = Vector2.zero;
        intersCloseBtnTextRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI intersCloseBtnText = intersCloseBtnTextGO.AddComponent<TextMeshProUGUI>();
        intersCloseBtnText.text = "CLOSE";
        intersCloseBtnText.alignment = TextAlignmentOptions.Converted;
        intersCloseBtnText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        intersCloseBtnText.verticalAlignment = VerticalAlignmentOptions.Geometry;
        intersCloseBtnText.fontSize = 60;
        intersCloseBtnText.raycastTarget = false;
        intersCloseBtnText.fontWeight = FontWeight.Bold;

        // Video AD
        GameObject videoAdGO = new("VideoAdGO");
        videoAdGO.SetActive(false);

        RectTransform videoAdRT = videoAdGO.AddComponent<RectTransform>();
        videoAdRT.SetParent(adCanvasGO.transform);
        videoAdRT.anchorMin = Vector2.zero;
        videoAdRT.anchorMax = Vector2.one;
        videoAdRT.pivot = new Vector2(0.5f, 0.5f);
        videoAdRT.sizeDelta = Vector2.zero;
        videoAdRT.anchoredPosition = Vector2.zero;

        Image videoAdImg = videoAdGO.AddComponent<Image>();
        videoAdImg.color = bgColor;

        GameObject videoAdTitleGO = new("Title");

        RectTransform videoAdTitleRT = videoAdTitleGO.AddComponent<RectTransform>();
        videoAdTitleRT.SetParent(videoAdRT);
        videoAdTitleRT.anchorMin = new Vector2(0, 0.5f);
        videoAdTitleRT.anchorMax = new Vector2(1, 0.5f);
        videoAdTitleRT.pivot = new Vector2(0.5f, 0.5f);
        videoAdTitleRT.sizeDelta = new Vector2(0, 100);
        videoAdTitleRT.anchoredPosition = new Vector2(0, 200);

        TextMeshProUGUI videoAdTitleText = videoAdTitleGO.AddComponent<TextMeshProUGUI>();
        videoAdTitleText.text = "MOCK REWARDED VIDEO AD";
        videoAdTitleText.alignment = TextAlignmentOptions.Converted;
        videoAdTitleText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        videoAdTitleText.verticalAlignment = VerticalAlignmentOptions.Geometry;
        videoAdTitleText.fontSize = 62;
        videoAdTitleText.raycastTarget = false;
        videoAdTitleText.fontWeight = FontWeight.Bold;

        GameObject videoAdCloseBtnGO = new("CloseBtn");

        RectTransform videoAdCloseBtnRT = videoAdCloseBtnGO.AddComponent<RectTransform>();
        videoAdCloseBtnRT.SetParent(videoAdRT);
        videoAdCloseBtnRT.anchorMin = new Vector2(0.5f, 0);
        videoAdCloseBtnRT.anchorMax = new Vector2(0.5f, 0);
        videoAdCloseBtnRT.pivot = new Vector2(0.5f, 0);
        videoAdCloseBtnRT.sizeDelta = new Vector2(600, 200);
        videoAdCloseBtnRT.anchoredPosition = new Vector2(0, 360);

        Image videoAdCloseBtnImg = videoAdCloseBtnGO.AddComponent<Image>();
        videoAdCloseBtnImg.color = btnClor;

        Button videoAdCloseBtn = videoAdCloseBtnGO.AddComponent<Button>();
        videoAdCloseBtn.targetGraphic = videoAdCloseBtnImg;

        // VideoAd Close Button Text
        GameObject videoAdCloseBtnTextGO = new ("Text");

        RectTransform videoAdCloseBtnTextRT = videoAdCloseBtnTextGO.AddComponent<RectTransform>();
        videoAdCloseBtnTextRT.SetParent(videoAdCloseBtnRT);
        videoAdCloseBtnTextRT.anchorMin = Vector2.zero;
        videoAdCloseBtnTextRT.anchorMax = Vector2.one;
        videoAdCloseBtnTextRT.pivot = new Vector2(0.5f, 0.5f);
        videoAdCloseBtnTextRT.sizeDelta = Vector2.zero;
        videoAdCloseBtnTextRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI videoAdCloseBtnText = videoAdCloseBtnTextGO.AddComponent<TextMeshProUGUI>();
        videoAdCloseBtnText.text = "CLOSE";
        videoAdCloseBtnText.alignment = TextAlignmentOptions.Converted;
        videoAdCloseBtnText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        videoAdCloseBtnText.verticalAlignment = VerticalAlignmentOptions.Geometry;
        videoAdCloseBtnText.fontSize = 60;
        videoAdCloseBtnText.raycastTarget = false;
        videoAdCloseBtnText.fontWeight = FontWeight.Bold;

        // RV Reward Button
        GameObject videoAdRewardBtnGO = new ("Get Reward Button");

        RectTransform videoAdRewardBtnRT = videoAdRewardBtnGO.AddComponent<RectTransform>();
        videoAdRewardBtnRT.SetParent(videoAdRT);
        videoAdRewardBtnRT.anchorMin = new Vector2(0.5f, 0);
        videoAdRewardBtnRT.anchorMax = new Vector2(0.5f, 0);
        videoAdRewardBtnRT.pivot = new Vector2(0.5f, 0);
        videoAdRewardBtnRT.sizeDelta = new Vector2(600, 200);
        videoAdRewardBtnRT.anchoredPosition = new Vector2(0, 600);

        Image videoAdRewardBtnImg = videoAdRewardBtnGO.AddComponent<Image>();
        videoAdRewardBtnImg.color = mainColor;

        Button videoAdRewardBtn = videoAdRewardBtnGO.AddComponent<Button>();
        videoAdRewardBtn.targetGraphic = videoAdRewardBtnImg;

        // RV Reward Button Text
        GameObject videoAdRewardBtnTextGO = new ("Text");

        RectTransform videoAdRewardBtnTextRT = videoAdRewardBtnTextGO.AddComponent<RectTransform>();
        videoAdRewardBtnTextRT.SetParent(videoAdRewardBtnRT);
        videoAdRewardBtnTextRT.anchorMin = Vector2.zero;
        videoAdRewardBtnTextRT.anchorMax = Vector2.one;
        videoAdRewardBtnTextRT.pivot = new Vector2(0.5f, 0.5f);
        videoAdRewardBtnTextRT.sizeDelta = Vector2.zero;
        videoAdRewardBtnTextRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI videoAdRewardBtnText = videoAdRewardBtnTextGO.AddComponent<TextMeshProUGUI>();
        videoAdRewardBtnText.text = "GET REWARD";
        videoAdRewardBtnText.alignment = TextAlignmentOptions.Converted;
        videoAdRewardBtnText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        videoAdRewardBtnText.verticalAlignment = VerticalAlignmentOptions.Geometry;
        videoAdRewardBtnText.fontSize = 60;
        videoAdRewardBtnText.raycastTarget = false;
        videoAdRewardBtnText.fontWeight = FontWeight.Bold;

        var controller = adCanvasGO.AddComponent<MockAdController>();
        controller.m_bannerAdGO = bannerAdGO;
        controller.m_interstitialAdGO = interstitialAdGO;
        controller.m_videoAdGO = videoAdGO;

        controller.m_bannerRT = bannerRT;

        controller.m_interstitialCloseBtn = intersCloseBtn;
        controller.m_videoAdCloseBtn = videoAdCloseBtn;
        controller.m_bannerCloseBtn = bannerCloseBtn;
        controller.m_videoAdRewardBtn = videoAdRewardBtn;

        return controller;
    }

    public void ShowBannerAD(int width = 360, float left = 0, float top = 0)
    {
        m_bannerRT.sizeDelta = new(width, width / 3);
        m_bannerRT.anchoredPosition =new Vector2(left, top);
        m_bannerAdGO.SetActive(true);
    }

    public void ShowInterstitialAD()
    {
        m_interstitialAdGO.SetActive(true);
    }
    public void ShowRewardedVideoAD(Action<bool, int> onClose = null)
    {
        onVideoAdClose = onClose;
        m_videoAdGO.SetActive(true);
    }
    public void CloseRewardedVideoAD()
    {
        m_videoAdGO.SetActive(false);
    }
    public void HideBannerAD()
    {
        onBannerAdClose?.Invoke();
        onBannerAdClose = null;
        m_bannerAdGO.SetActive(false);
        Debug.Log("编辑器Banner广告关闭");
    }

    private void InterstitialCloseBtnClicked()
    {

    }
    private void VideoAdCloseBtnClicked()
    {
        onVideoAdClose?.Invoke(false, 0);
        onVideoAdClose = null;
        CloseRewardedVideoAD();
    }
    private void VideoAdRewardBtnClicked()
    {
        onVideoAdClose?.Invoke(true, 1);
        onVideoAdClose = null;
        CloseRewardedVideoAD();
    }
}
#endif
