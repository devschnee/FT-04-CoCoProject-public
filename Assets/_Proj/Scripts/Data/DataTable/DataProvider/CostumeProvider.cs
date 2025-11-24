using UnityEngine;

public class CostumeProvider : IDataProvider<int, CostumeData>
{
    private CostumeDatabase database;
    private IResourceLoader loader;

    public CostumeDatabase Value => database;

    public CostumeProvider(CostumeDatabase db, IResourceLoader resLoader)
    {
        database = db;
        loader = resLoader;
    }

    public CostumeData GetData(int id)
    {
        return database.costumeList.Find(a => a.costume_id == id);
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
