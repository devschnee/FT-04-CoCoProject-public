using System.Collections.Generic;
using System.Linq;
public class DialogueProvider : IDataProvider<string, DialogueData>
{
    private DialogueDatabase database;
    private IResourceLoader loader;
    public DialogueProvider(DialogueDatabase db, IResourceLoader resLoader)
    {
        database = db;
        loader = resLoader;
    }
    public DialogueData GetData(string id)
    {
        return database.dialogueList.Find(s => s.dialogue_id == id);
    }

    public DialogueData GetSeqData(string dialogueId, int seq)
    {
        return database.dialogueList
            .FirstOrDefault(s => s.dialogue_id == dialogueId && s.seq == seq);
    }

    //public List<DialogueData> GetData(string id)
    //{
    //    List<DialogueData> sequences = database.dialogueList
    //        .Where(d => d.dialogue_id == id)
    //        .OrderBy(d => d.seq)
    //        .ToList();

    //    return sequences;
    //}
}