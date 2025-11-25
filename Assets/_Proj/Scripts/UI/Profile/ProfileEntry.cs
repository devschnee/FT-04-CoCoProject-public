using UnityEngine;
using UnityEngine.UI;

public class ProfileEntry
{
    public int id;
    public Sprite icon;
    public ProfileType type;
    public bool isUnlocked;

    public ProfileEntry(int id)
    {
            this.id = id;
        if (120000 < id && id < 130000)
        {
            icon = DataManager.Instance.Profile.GetIcon(id);
            type = ProfileType.icon;
            //코코두기 아이콘은 기본 해금.
            isUnlocked = id == 120001;
        }

        if (10000 < id && id < 20000)
        {
            icon = DataManager.Instance.Deco.GetIcon(id);
            type = ProfileType.deco;
            isUnlocked = UserData.Local.codex[CodexType.deco, id];
        }

        if (20000 < id && id < 30000)
        {
            icon = DataManager.Instance.Costume.GetIcon(id);
            type = ProfileType.costume;
            isUnlocked = UserData.Local.codex[CodexType.costume, id];
        }

        if (30000 < id && id < 40000)
        {
            icon = DataManager.Instance.Animal.GetIcon(id);
            type = ProfileType.animal;
            isUnlocked = UserData.Local.codex[CodexType.animal, id];
        }

        if (50000 < id && id < 60000)
        {
            icon = DataManager.Instance.Artifact.GetIcon(id);
            type = ProfileType.artifact;
            isUnlocked = UserData.Local.codex[CodexType.artifact, id];
        }
    }


}