
using System;

/// <summary>
/// 存档服务
/// </summary>
public interface ISaveService
{
    #region  同步操作
    /// <summary>
    /// 保存游戏数据
    /// </summary>
    /// <param name="key">存储键</param>
    /// <param name="data">要保存的游戏数据对象</param>
    /// <returns>操作结果。</returns>
    SaveResultDto Save<T>(string key, T data);

    /// <summary>
    /// 加载游戏数据
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>加载的游戏数据，如果不存在则返回null</returns>
    T Load<T>(string key);

    /// <summary>
    /// 删除指定存档
    /// </summary>
    /// <param name="key">要删除的存储键</param>
    /// <returns>操作结果</returns>
    SaveResultDto Delete(string key);
    SaveResultDto DeleteAll();

    #endregion

    #region 异步操作

    /// <summary>
    /// 异步保存游戏数据
    /// </summary>
    /// <param name="key">存储键</param>
    /// <param name="data">要保存的游戏数据对象</param>
    /// <param name="onComplete"></param>
    void SaveAsync<T>(string key, T data, Action<SaveResultDto> onComplete = null);

    /// <summary>
    /// 异步加载游戏数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="onComplete">函数执行完成的回调，数据通过回调返回</param>
    void LoadAsync<T>(string key, Action<T, SaveResultDto> onComplete);

    /// <summary>
    /// 异步删除指定存档
    /// </summary>
    /// <param name="key">要删除的存储键</param>
    /// <returns>操作结果</returns>
    void DeleteAsync(string key, Action<SaveResultDto> onComplete = null);
    void DeleteAllAsync(Action<SaveResultDto> onComplete = null);
    #endregion
}

 // 操作结果的通用数据传输对象，包含状态码和消息
[System.Serializable]
public class SaveResultDto
{
    public bool     isSuccess;
    public string   message; // 成功时为空，失败时为错误消息
}