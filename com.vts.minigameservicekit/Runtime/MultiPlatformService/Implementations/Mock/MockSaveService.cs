public class MockSaveService : SaveServiceBase
{
    protected override void SaveString(string key, string json) => PlayerPrefs.SetString(key, json);
    protected override string LoadString(string key) => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
    protected override void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
    protected override bool HasKeyInternal(string key) => PlayerPrefs.HasKey(key);

    public override SaveResultDto DeleteAll()
    {
        PlayerPrefs.DeleteAll();
        return new SaveResultDto { isSuccess = true };
    }
}
