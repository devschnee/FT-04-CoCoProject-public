using UnityEngine;

public class ArtifactProvider : IDataProvider<int, ArtifactData>
{
    private ArtifactDatabase database;
    private IResourceLoader loader;

    public ArtifactProvider(ArtifactDatabase db, IResourceLoader resloader)
    {
        database = db;
        loader = resloader;
    }

    public ArtifactDatabase Value => database;

    public ArtifactData GetData(int id)
    {
        return database.artifactList.Find(a => a.artifact_id == id);
    }

    public Sprite GetIcon(int id)
    {
        var data = GetData(id);
        return data?.GetIcon(loader);
    }
}
