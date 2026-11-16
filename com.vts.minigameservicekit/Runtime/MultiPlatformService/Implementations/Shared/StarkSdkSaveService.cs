#if MINIGAME_SUBPLATFORM_DOUYIN

using TTSDK;

/// <summary>
/// 抖音平台共用存档服务：基于 TT.PlayerPrefs（同步 API）
/// </summary>
public abstract class StarkSdkSaveServiceBase : SaveServiceBase
{
    protected override void SaveString(string key, string json)
    {
        TT.PlayerPrefs.SetString(key, json);
        TT.PlayerPrefs.Save();
    }

    protected override string LoadString(string key)
    {
        return TT.PlayerPrefs.HasKey(key) ? TT.PlayerPrefs.GetString(key, string.Empty) : null;
    }

    protected override void DeleteKey(string key)
    {
        TT.PlayerPrefs.DeleteKey(key);
        TT.PlayerPrefs.Save();
    }

    protected override bool HasKeyInternal(string key)
    {
        return TT.PlayerPrefs.HasKey(key);
    }

    public override SaveResultDto DeleteAll()
    {
        TT.PlayerPrefs.DeleteAll();
        TT.PlayerPrefs.Save();
        return new SaveResultDto { isSuccess = true };
    }
}

#endif
