using System;
using UnityEngine;

public abstract class SaveServiceBase: ISaveService
{
    protected abstract void SaveString(string key, string json);
    protected abstract string LoadString(string key);
    protected abstract void DeleteKey(string key);
    protected abstract bool HasKeyInternal(string key);

    #region 同步封装
    public SaveResultDto Save<T>(string key, T data)
    {
        try
        {
            string json = JsonUtility.ToJson(data);
            SaveString(key, json);
            return new SaveResultDto { isSuccess = true };
        }
        catch (System.Exception e)
        {
            return new SaveResultDto { isSuccess = false, message = e.Message };
        }
    }

    public T Load<T>(string key)
    {
        try
        {
            string json = LoadString(key);
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }
            return JsonUtility.FromJson<T>(json);
        }
        catch
        {
            return default;
        }
    }

    public SaveResultDto Delete(string key)
    {
        try
        {
            DeleteKey(key);
            return new SaveResultDto { isSuccess = true };
        }
        catch (System.Exception e)
        {
            return new SaveResultDto { isSuccess = false, message = e.Message };
        }
    }
    public abstract SaveResultDto DeleteAll();

    public bool HasKey(string key) => HasKeyInternal(key);

    #endregion

    #region 异步封装 
    //  异步方法在基类中默认使用同步实现，实际异步实现由子类重写

    public virtual void SaveAsync<T>(string key, T data, Action<SaveResultDto> onComplete = null)
    {
        var result = Save(key, data);
        onComplete?.Invoke(result);
    }

    public virtual void LoadAsync<T>(string key, Action<T, SaveResultDto> onComplete)
    {
        var data = Load<T>(key);
        var result = new SaveResultDto { isSuccess = data != null };
        onComplete?.Invoke(data, result);
    }

    public virtual void DeleteAsync(string key, Action<SaveResultDto> onComplete = null)
    {
        var result = Delete(key);
        onComplete?.Invoke(result);
    }
    public virtual void DeleteAllAsync(Action<SaveResultDto> onComplete = null)
    {
        var result = DeleteAll();
        onComplete?.Invoke(result);
    }
    #endregion
}