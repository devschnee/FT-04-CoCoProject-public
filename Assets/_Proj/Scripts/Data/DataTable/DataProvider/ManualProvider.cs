using System.Collections.Generic;
using UnityEngine;

public class ManualProvider : IDataProvider<int, ManualData>
{
    private ManualDatabase database;
    private IResourceLoader loader;
    public List<ManualData> AllData => database.manualList;

    public ManualProvider(ManualDatabase db, IResourceLoader resLoader)
    {
        database = db;
        loader = resLoader;
    }

    public ManualData GetData(int id)
    {
        return database.manualList.Find(a => a.manual_id == id);
    }

    public Sprite GetIcon(int id)
    {
        var data = GetData(id);
        return data?.GetIcon(loader);
    }
}