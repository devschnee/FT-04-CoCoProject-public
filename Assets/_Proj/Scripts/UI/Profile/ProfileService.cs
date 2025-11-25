using System.Collections.Generic;
using System.Text;
using UnityEditor.Experimental;
using UnityEngine;

public class ProfileService : MonoBehaviour
{
    
    public List<ProfileEntry> GetAll()
    {
        var list = new List<ProfileEntry>();
        list.AddRange(GetByType(ProfileType.animal));
        list.AddRange(GetByType(ProfileType.deco));
        list.AddRange(GetByType(ProfileType.costume));
        list.AddRange(GetByType(ProfileType.artifact));
        list.AddRange(GetByType(ProfileType.icon));
        return list;
    }

    
    public List<ProfileEntry> GetByType(ProfileType type)
    {
        var entries = new List<ProfileEntry>();

        switch (type)
        {
            case ProfileType.animal:
                foreach (var animal in DataManager.Instance.Animal.Value)
                {
                    entries.Add(new ProfileEntry(animal.animal_id));
                }
                break;
            case ProfileType.deco:
                foreach (var deco in DataManager.Instance.Deco.Value)
                {
                    entries.Add(new ProfileEntry(deco.deco_id));
                }
                break;
            case ProfileType.costume:
                foreach (var costume in DataManager.Instance.Costume.Value)
                {
                    entries.Add(new ProfileEntry(costume.costume_id));
                }
                break;
            case ProfileType.artifact:
                foreach (var artifact in DataManager.Instance.Artifact.Value)
                {
                    entries.Add(new ProfileEntry(artifact.artifact_id));
                }
                break;
            case ProfileType.icon:
                Debug.Log($"构啊 null老鳖? DataManager.Instance?: {DataManager.Instance==null}");
                Debug.Log($"构啊 null老鳖? DataManager.Instance.Profile?: {DataManager.Instance.Profile==null}");
                Debug.Log($"构啊 null老鳖? Profile.Value?: {DataManager.Instance.Profile.Value==null}");
                foreach (var profile in DataManager.Instance.Profile.Value)
                {
                    entries.Add(new ProfileEntry(profile.icon_id));
                }
                break;
        }

        return entries;
    }
}