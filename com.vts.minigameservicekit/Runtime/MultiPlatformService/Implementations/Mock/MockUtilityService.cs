using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MockUtilityService : IPlatformUtilityService
{
    /// <summary>
    /// Simulator 模式,Unity 模拟不同设备的屏幕物理分辨率,但不会提供“逻辑像素”概念,返回物理尺寸
    /// </summary>
    public Rect GetLogicSafeArea()
    {
        return Screen.safeArea;
    }

    public Vector2 GetLogicScreenSize()
    {
        return new(Screen.width, Screen.height);
    }

    public void SetPreferredFrames(int fps)
    {
        Application.targetFrameRate = fps;
    }

    public void SetScreenOrientation(ScreenOrientation orientation, Action onSuccess, Action<string> onFail = null)
    {
        Screen.orientation = orientation;
        onSuccess?.Invoke();
    }

    #region 键盘（编辑器无微信键盘，空实现并记录调用）

    public void ShowKeyboard(string initialText, int maxLength, bool multiple = false)
    {
        Debug.Log($"[Mock] ShowKeyboard: \"{initialText}\" maxLength={maxLength}");
    }

    public void HideKeyboard()
    {
        Debug.Log("[Mock] HideKeyboard");
    }

    public void OnKeyboardHeightChange(Action<float> callback)
    {
        Debug.Log("[Mock] 注册键盘高度变化监听（编辑器内不会触发）");
    }

    public void RemoveKeyboardHeightChange()
    {
        Debug.Log("[Mock] 移除键盘高度变化监听");
    }

    #endregion

    #region 系统字体

    public Font GetDefaultFont()
    {
        // 2022 起内置字体为 LegacyRuntime.ttf
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public void GetSystemFont(Action<Font> callback)
    {
        callback?.Invoke(GetDefaultFont());
    }

    #endregion

    #region 触摸事件（编辑器内走 Unity 输入，此处仅保存回调并记录）

    private readonly List<Action<int, float, float>> m_touchStart = new();
    private readonly List<Action<int, float, float>> m_touchMove = new();
    private readonly List<Action<int, float, float>> m_touchEnd = new();

    public void AddTouchStartListener(Action<int, float, float> callback)
    {
        m_touchStart.Add(callback);
        Debug.Log("[Mock] 注册 TouchStart 监听（编辑器内不会触发）");
    }

    public void AddTouchMoveListener(Action<int, float, float> callback)
    {
        m_touchMove.Add(callback);
        Debug.Log("[Mock] 注册 TouchMove 监听（编辑器内不会触发）");
    }

    public void AddTouchEndListener(Action<int, float, float> callback)
    {
        m_touchEnd.Add(callback);
        Debug.Log("[Mock] 注册 TouchEnd 监听（编辑器内不会触发）");
    }

    public void RemmoveAllTouchListeners()
    {
        m_touchStart.Clear();
        m_touchMove.Clear();
        m_touchEnd.Clear();
    }

    #endregion

    #region 震动 / 剪贴板（编辑器模拟）

    public void VibrateShort()
    {
        Debug.Log("[Mock] 模拟短震动");
    }

    public void VibrateLong()
    {
        Debug.Log("[Mock] 模拟长震动");
    }

    public void SetClipboardText(string text)
    {
        GUIUtility.systemCopyBuffer = text;
        Debug.Log($"[Mock] 写入剪贴板: {text}");
    }

    public void GetClipboardText(Action<string> callback)
    {
        callback?.Invoke(GUIUtility.systemCopyBuffer);
    }

    #endregion
}
