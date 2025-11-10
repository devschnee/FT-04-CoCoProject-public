using System.Collections.Generic;
using System.Linq;

public class DialogueProvider : IDataProvider<string, List<DialogueData>>
{
    private DialogueDatabase database;
    private IResourceLoader loader;

    public DialogueProvider(DialogueDatabase db, IResourceLoader resLoader)
    {
        database = db;
        loader = resLoader;
    }

    public List<DialogueData> GetData(string id)
    {
        List<DialogueData> sequences = database.dialogueList
            .Where(d => d.dialogue_id == id)
            .OrderBy(d => d.seq) 
            .ToList();

        return sequences;
    }
}