#if MINIGAME_SUBPLATFORM_WEIXIN
using System;
using UnityEngine;
using WeChatWASM;

public class WeChatUtilityService : IPlatformUtilityService
{
    public Rect GetLogicSafeArea()
    {
        var info = WX.GetWindowInfo();
        var safeArea = info.safeArea;

        // 微信的坐标系和 Unity 的坐标系原点不同
        // Unity 原点在屏幕左下角，Y轴向上为正
        // 微信 原点在屏幕左上角，Y轴向下为正
        // 微信->Unity安全区下边缘 = 屏幕高度 − 安全区右下角纵坐标 = screenHeight - bottom

        return new Rect
            (
                (float)safeArea.left,
                (float)(info.windowHeight - safeArea.bottom),
                (float)safeArea.width,
                (float)safeArea.height
            );
    }

    public Vector2 GetLogicScreenSize()
    {
        var info = WX.GetSystemInfoSync();
        return new((float)info.windowWidth, (float)info.windowHeight);
    }

    public void SetPreferredFrames(int fps)
    {
        WX.SetPreferredFramesPerSecond(fps);
    }
    public void SetScreenOrientation(ScreenOrientation orientation,Action onSuccess,Action<string> onFail = null)
    {
        SetDeviceOrientationOption op = new()
        {
            value = orientation switch
            {
                ScreenOrientation.Portrait => "portrait",
                _ => "landscape",
            },
            success = (result) => onSuccess?.Invoke(),
            fail = (result) => onFail?.Invoke(result.errMsg),
        };
        WX.SetDeviceOrientation(op);
    }

    public void ShowKeyboard(string initialText, int maxLength, bool multiple = false)
    {
        var op = new ShowKeyboardOption()
        {
            defaultValue = initialText,
            maxLength = maxLength,
            multiple = multiple,
            success = (res) => { },
            fail = (res) => { },
            complete = (res) => { }
        };
        WX.ShowKeyboard(op);
    }

    public void HideKeyboard()
    {
        var op = new HideKeyboardOption()
        {
            success = (res) => { },
            fail = (res) => { },
            complete = (res) => { }
        };
        WX.HideKeyboard(op);
    }

    public void OnKeyboardHeightChange(Action<float> callback)
    {
        WX.OnKeyboardHeightChange((result) => callback?.Invoke((float)result.height));
    }

    public void RemoveKeyboardHeightChange()
    {
        throw new NotImplementedException();
    }

    public Font GetDefaultFont()
    {
        throw new NotImplementedException();
    }

    public void GetSystemFont(Action<Font> callback)
    {
        throw new NotImplementedException();
    }

    public void AddTouchStartListener(Action<int, float, float> callback)
    {
        throw new NotImplementedException();
    }

    public void AddTouchMoveListener(Action<int, float, float> callback)
    {
        throw new NotImplementedException();
    }

    public void AddTouchEndListener(Action<int, float, float> callback)
    {
        throw new NotImplementedException();
    }

    public void RemmoveAllTouchListeners()
    {
        throw new NotImplementedException();
    }

    #region 震动 / 剪贴板

    public void VibrateShort()
    {
        WX.VibrateShort(new VibrateShortOption { type = "medium" });
    }

    public void VibrateLong()
    {
        WX.VibrateLong(new VibrateLongOption());
    }

    public void SetClipboardText(string text)
    {
        WX.SetClipboardData(new SetClipboardDataOption { data = text });
    }

    public void GetClipboardText(Action<string> callback)
    {
        WX.GetClipboardData(new GetClipboardDataOption
        {
            success = (res) => callback?.Invoke(res.data),
            fail = (err) =>
            {
                Debug.LogWarning("[WeChatUtility] 读取剪贴板失败: " + err.errMsg);
                callback?.Invoke(null);
            },
        });
    }

    #endregion
}
#endif