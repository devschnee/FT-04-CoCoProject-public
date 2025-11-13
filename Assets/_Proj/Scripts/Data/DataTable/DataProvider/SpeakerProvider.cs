using UnityEngine;
using static SpeakerData;

public class SpeakerProvider : IDataProvider<SpeakerId, SpeakerData>
{
    private SpeakerDatabase database;
    private IResourceLoader loader;

    public SpeakerProvider(SpeakerDatabase db, IResourceLoader resLoader)
    {
        database = db; ;
        loader = resLoader;
    }

    public SpeakerData GetData(SpeakerId id)
    {
        return database.speakerList.Find(s => s.speaker_id == id);
    }

    public Sprite GetPortrait(SpeakerId id, string portrait_set_prefix)
    {
        var data = GetData(id);
        return data?.GetPortrait(loader, portrait_set_prefix);
    }
}
