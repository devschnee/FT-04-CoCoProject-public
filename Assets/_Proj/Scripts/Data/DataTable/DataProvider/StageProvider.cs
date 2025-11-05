using UnityEngine;

public class StageProvider : IDataProvider<string, StageData>
{
    private StageDatabase database;
    private IResourceLoader loader;

    public StageProvider(StageDatabase db, IResourceLoader resLoader)
    {
        database = db;
        loader = resLoader;
    }

    public StageData GetData(string id)
    {
        return database.stageDataList.Find(a => a.stage_id == id);
    }
    public StageData GetMapNameData(string id)
    {
        return database.stageDataList.Find(a => a.map_id == id);
    }

    public Sprite GetIcon(string id)
    {
        var data = GetData(id);
        return data?.GetIcon(loader);
    }
}