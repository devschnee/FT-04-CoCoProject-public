using UnityEngine;

public class CodexProvider : IDataProvider<string, CodexData>
{
    private CodexDatabase database;
    private IResourceLoader loader;

    public CodexProvider(CodexDatabase db, IResourceLoader resLoader)
    {
        database = db;
        loader = resLoader;
    }

    public CodexDatabase Value => database;

    public CodexData GetData(string id)
    {
        return database.codexList.Find(a => a.codex_id == id);
    }

    public CodexData GetData(int id) => database.codexList.Find(x=>x.item_id == id);
  

    public Sprite GetCodexDisplay(string id)
    {
        var data = GetData(id);
        return data?.GetCodexDisplay(loader);
    }
    public Sprite GetCodexIcon(string id)
    {
        var data = GetData(id);
        return data?.GetCodexIcon(loader);
    }

    public Sprite GetCodexIcon(int id)
    {
        var data = GetData(id);
        return data?.GetCodexIcon(loader);
    }
}
