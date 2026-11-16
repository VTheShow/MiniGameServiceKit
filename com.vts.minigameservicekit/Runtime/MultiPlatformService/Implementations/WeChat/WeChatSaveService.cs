#if MINIGAME_SUBPLATFORM_WEIXIN

using WeChatWASM;

public class WeChatSaveService : SaveServiceBase   // 微信存档服务
{
    protected override void SaveString(string key, string json) => WX.StorageSetStringSync(key, json);
    protected override string LoadString(string key) => WX.StorageHasKeySync(key) ? WX.StorageGetStringSync(key, string.Empty) : null;
    protected override void DeleteKey(string key) => WX.StorageDeleteKeySync(key);
    protected override bool HasKeyInternal(string key) => WX.StorageHasKeySync(key);

    public override SaveResultDto DeleteAll()
    {
        WX.StorageDeleteAllSync();
        return new SaveResultDto { isSuccess = true };
    }

    // 微信只支持同步存储，无需重写异步方法。
    // 基类 SaveServiceBase 中的 SaveAsync / LoadAsync / DeleteAsync 默认会调用同步版本
}

#endif