using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 模拟支付面板只存在于编辑器；真机降级时绝不能让玩家「模拟支付成功」拿到商品
#if UNITY_EDITOR
/// <summary>
/// 编辑器模拟支付面板（与 MockAdController 同模式，纯代码构建 UI）：
/// 模拟平台支付流程——显示商品/价格，点击「模拟支付成功」或「取消」回调结果
/// </summary>
public class MockPayController : MonoBehaviour
{
    private GameObject      m_panelGO;
    private TextMeshProUGUI m_titleText;
    private TextMeshProUGUI m_priceText;
    private Button          m_payBtn;
    private Button          m_cancelBtn;

    private Action<bool> m_onClosed;

    public static MockPayController CreateObject()
    {
        var go = new GameObject("MOCK PAY CANVAS");

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        go.AddComponent<GraphicRaycaster>();
        go.AddComponent<MockPayController>();
        return go.GetComponent<MockPayController>();
    }

    public void Init()
    {
        DontDestroyOnLoad(gameObject);
        BuildPanel();
        gameObject.SetActive(false);
    }

    private void BuildPanel()
    {
        Color mainColor = new(0.2f, 0.25f, 0.55f, 1f);
        Color btnColor  = new(1f, 0.6f, 0.125f, 1f);

        // 半透明遮罩
        var maskGO = new GameObject("Mask");
        var maskRT = maskGO.AddComponent<RectTransform>();
        maskRT.SetParent(transform, false);
        maskRT.anchorMin = Vector2.zero;
        maskRT.anchorMax = Vector2.one;
        maskRT.sizeDelta = Vector2.zero;
        var maskImg = maskGO.AddComponent<Image>();
        maskImg.color = new Color(0, 0, 0, 0.6f);

        // 面板
        m_panelGO = new GameObject("Panel");
        var panelRT = m_panelGO.AddComponent<RectTransform>();
        panelRT.SetParent(transform, false);
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(720, 520);
        var panelImg = m_panelGO.AddComponent<Image>();
        panelImg.color = mainColor;

        m_titleText = CreateText(m_panelGO.transform, "MOCK 支付", 44, new Vector2(0, 170), new Vector2(640, 80));
        m_priceText = CreateText(m_panelGO.transform, string.Empty, 56, Vector2.zero, new Vector2(640, 160));

        m_payBtn = CreateButton(m_panelGO.transform, "模拟支付成功", btnColor, new Vector2(0, -140), new Vector2(520, 110), 40);
        m_cancelBtn = CreateButton(m_panelGO.transform, "取消", new Color(0.45f, 0.45f, 0.5f), new Vector2(0, -270), new Vector2(520, 110), 40);

        m_payBtn.onClick.AddListener(() => Close(true));
        m_cancelBtn.onClick.AddListener(() => Close(false));
    }

    public void Show(string productName, int amountCents, Action<bool> onClosed)
    {
        m_onClosed = onClosed;
        m_titleText.text = "MOCK 支付";
        m_priceText.text = $"{productName}\n¥{amountCents / 100f:0.00}";
        gameObject.SetActive(true);
    }

    private void Close(bool success)
    {
        gameObject.SetActive(false);
        var cb = m_onClosed;
        m_onClosed = null;
        cb?.Invoke(success);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string content, float size, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject("Text");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.fontWeight = FontWeight.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(Transform parent, string label, Color color, Vector2 pos, Vector2 sizeDelta, float fontSize)
    {
        var go = new GameObject(label);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var textGO = new GameObject("Text");
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.SetParent(rt, false);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
        textRT.anchoredPosition = Vector2.zero;
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = fontSize;
        text.fontWeight = FontWeight.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return btn;
    }
}
#endif
