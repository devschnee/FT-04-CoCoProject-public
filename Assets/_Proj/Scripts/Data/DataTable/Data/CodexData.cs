using System;
using UnityEngine;

[Serializable]
public class CodexData
{
    public string codex_id;
    public CodexType codex_type;
    public int item_id;
    public string codex_lore;
    public string codex_display;

    [NonSerialized] public Sprite codexDisplay;

    public Sprite GetCodexDisplay(IResourceLoader loader)
    {
        if (codexDisplay == null && !string.IsNullOrEmpty(codex_display))
            codexDisplay = loader.LoadSprite(codex_display);
        return codexDisplay;
    }
}

public enum CodexType
{
    deco, costume, animal, home, artifact
}
