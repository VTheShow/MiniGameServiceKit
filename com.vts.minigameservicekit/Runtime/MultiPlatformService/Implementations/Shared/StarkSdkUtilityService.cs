#if MINIGAME_SUBPLATFORM_DOUYIN

using System;
using System.Collections.Generic;
using UnityEngine;
using TTSDK;

/// <summary>
/// 抖音平台共用平台工具服务：安全区、系统信息、键盘、字体、转屏
/// </summary>
public abstract class StarkSdkUtilityServiceBase : IPlatformUtilityService
{
    public Rect GetLogicSafeArea()
    {
        var info = TT.GetSystemInfo();
        var sa = info.safeArea;

        // 抖音坐标系原点在屏幕左上角（Y 向下），Unity 原点在左下角（Y 向上）
        // 安全区下边缘（Unity坐标） = 屏幕高度 − safeArea.bottom
        return new Rect(sa.left, info.screenHeight - sa.bottom, sa.width, sa.height);
    }

    public Vector2 GetLogicScreenSize()
    {
        var info = TT.GetSystemInfo();
        return new Vector2(info.screenWidth, info.screenHeight);
    }

    public void SetPreferredFrames(int fps)
    {
        TT.SetPreferredFramesPerSecond(fps);
    }

    public void SetScreenOrientation(ScreenOrientation orientation, Action onSuccess, Action<string> onFail = null)
    {
        var param = new SetDeviceOrientationParam
        {
            Value = orientation == ScreenOrientation.Portrait ? TTDeviceOrientation.Portrait : TTDeviceOrientation.Landscape,
        };
        TT.SetDeviceOrientation(param);
        onSuccess?.Invoke();
    }

    #region 键盘

    public void ShowKeyboard(string initialText, int maxLength, bool multiple = false)
    {
        TT.ShowKeyboard(new TTKeyboard.ShowKeyboardOptions
        {
            defaultValue = initialText,
            maxLength = maxLength,
            multiple = multiple,
        });
    }

    public void HideKeyboard()
    {
        TT.HideKeyboard();
    }

    public void OnKeyboardHeightChange(Action<float> callback)
    {
        // StarkSDK 未提供键盘高度变化接口，仅有输入内容事件（TT.OnKeyboardInput 等）
        Debug.Log("[StarkUtility] 抖音平台不支持键盘高度变化监听");
    }

    public void RemoveKeyboardHeightChange()
    {
        // 同上，no-op
    }

    #endregion

    #region 系统字体

    public Font GetDefaultFont()
    {
        // 系统字体只能通过 GetSystemFont 异步获取，没有同步版本
        Debug.LogWarning("[StarkUtility] 系统字体需通过 GetSystemFont 异步获取");
        return null;
    }

    public void GetSystemFont(Action<Font> callback)
    {
        TT.GetSystemFont(callback);
    }

    #endregion

    #region 触摸事件（StarkSDK 将触摸注入 Unity 输入管线，此处仅保存回调）

    private readonly List<Action<int, float, float>> m_touchStart = new();
    private readonly List<Action<int, float, float>> m_touchMove = new();
    private readonly List<Action<int, float, float>> m_touchEnd = new();

    public void AddTouchStartListener(Action<int, float, float> callback)
    {
        m_touchStart.Add(callback);
    }

    public void AddTouchMoveListener(Action<int, float, float> callback)
    {
        m_touchMove.Add(callback);
    }

    public void AddTouchEndListener(Action<int, float, float> callback)
    {
        m_touchEnd.Add(callback);
    }

    public void RemmoveAllTouchListeners()
    {
        m_touchStart.Clear();
        m_touchMove.Clear();
        m_touchEnd.Clear();
    }

    #endregion

    #region 震动 / 剪贴板

    public void VibrateShort()
    {
        TT.VibrateShort(new VibrateShortParam());
    }

    public void VibrateLong()
    {
        TT.VibrateLong(new VibrateLongParam());
    }

    public void SetClipboardText(string text)
    {
        TT.SetClipboardData(text, (success, errMsg) =>
        {
            if (!success)
            {
                Debug.LogWarning("[StarkUtility] 写入剪贴板失败: " + errMsg);
            }
        });
    }

    public void GetClipboardText(Action<string> callback)
    {
        TT.GetClipboardData((success, data) =>
        {
            if (!success)
            {
                Debug.LogWarning("[StarkUtility] 读取剪贴板失败: " + data);
            }
            callback?.Invoke(success ? data : null);
        });
    }

    #endregion
}

#endif
