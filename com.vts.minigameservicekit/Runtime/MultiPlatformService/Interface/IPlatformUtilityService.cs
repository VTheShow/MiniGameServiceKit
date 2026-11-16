using System;
using UnityEngine;

/// <summary>
/// 平台基础工具服务：安全区域、键盘、系统字体、触摸事件
/// </summary>
public interface IPlatformUtilityService
{
    // 安全区域，返回逻辑像素安全区
    Rect GetLogicSafeArea();
    // 获取屏幕分辨率
    Vector2 GetLogicScreenSize();
    // 设置首选帧率
    void SetPreferredFrames(int fps);

    // 设置屏幕方向
    void SetScreenOrientation(ScreenOrientation orientation, Action onSuccess, Action<string> onFail = null);

    // 键盘
    void ShowKeyboard(string initialText, int maxLength, bool multiple = false);
    void HideKeyboard();
    void OnKeyboardHeightChange(Action<float> callback);
    void RemoveKeyboardHeightChange();

    // 系统字体
    Font GetDefaultFont();
    void GetSystemFont(Action<Font> callback);

    // 触摸事件
    void AddTouchStartListener(Action<int,float,float> callback);
    void AddTouchMoveListener(Action<int,float,float> callback);
    void AddTouchEndListener(Action<int,float,float> callback);
    void RemmoveAllTouchListeners();

    #region 震动 / 剪贴板（不支持的平台为 no-op 并记录日志）

    /// <summary> 短震动（约 40ms，交互反馈） </summary>
    void VibrateShort();
    /// <summary> 长震动（约 400ms，重要提示） </summary>
    void VibrateLong();
    /// <summary> 写入剪贴板 </summary>
    void SetClipboardText(string text);
    /// <summary> 读取剪贴板 </summary>
    void GetClipboardText(Action<string> callback);

    #endregion
}