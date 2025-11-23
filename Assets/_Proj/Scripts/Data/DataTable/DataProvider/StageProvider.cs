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
    public StageDatabase Value => database;

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

    public string GetStartCutsceneUrl(string stageId)
    {
        var data = GetData(stageId);
        if (data == null) return null;

        var relativePath = data.GetStartCutscenePath();
        return CutscenePathBuilder.BuildUrl(relativePath);
    }

    public string GetEndCutsceneUrl(string stageId)
    {
        var data = GetData(stageId);
        if (data == null) return null;

        var relativePath = data.GetEndCutscenePath();
        return CutscenePathBuilder.BuildUrl(relativePath);
    }

    public AudioClip GetAudioClip(string id)
    {
        var data = GetData(id);
        return data?.GetAudio(loader);
    }
}