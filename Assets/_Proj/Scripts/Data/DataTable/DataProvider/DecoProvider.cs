using UnityEngine;

public class DecoProvider : IDataProvider<int, DecoData>
{
    private DecoDatabase database;
    private IResourceLoader loader;

    public DecoDatabase Value => database;

    

    public DecoProvider(DecoDatabase db, IResourceLoader resLoader)
    {
        database = db;
        loader = resLoader;
    }

    public DecoData GetData(int id)
    {
        return database.decoList.Find(a => a.deco_id == id);
    }

    public GameObject GetPrefab(int id)
    {
        var data = GetData(id);
        return data?.GetPrefab(loader);
    }
    public Sprite GetIcon(int id)
    {
        var data = GetData(id);
        return data?.GetIcon(loader);
    }


    
}
