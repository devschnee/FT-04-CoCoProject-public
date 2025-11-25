using UnityEngine;

public class Profile_iconProvider : IDataProvider<int, Profile_iconData>
{
    private Profile_iconDatabase database;
    private IResourceLoader loader;

    public Profile_iconProvider(Profile_iconDatabase db, IResourceLoader resLoader)
    {
        database = db;
        loader = resLoader;
    }

    public Profile_iconDatabase Value => database;

    public Profile_iconData GetData(int id)
    {
        return database.profileList.Find(a => a.icon_id == id);
    }

    public Sprite GetIcon(int id)
    {
        var data = GetData(id);
        return data?.GetIcon(loader);
    }
}
