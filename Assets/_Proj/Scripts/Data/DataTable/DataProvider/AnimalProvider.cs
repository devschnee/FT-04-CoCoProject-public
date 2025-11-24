using UnityEngine;

public class AnimalProvider : IDataProvider<int, AnimalData>
{
    //각 데이터 별로 Provider 클래스를 두어서 자기 데이터만 관리하게 함
    private AnimalDatabase database;
    private IResourceLoader loader;

    public AnimalDatabase Value => database;

    public AnimalProvider(AnimalDatabase db, IResourceLoader resLoader)
    {
        database = db;
        loader = resLoader;
    }

    public AnimalData GetData(int id)
    {
        return database.animalList.Find(a => a.animal_id == id);
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