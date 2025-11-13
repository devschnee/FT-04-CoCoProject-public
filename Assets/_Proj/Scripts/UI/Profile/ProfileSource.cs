using System.Collections.Generic;
using UnityEngine;

public class ProfileSource : MonoBehaviour, IProfileSource
{
    private AnimalDatabase AnimalDB => DataManager.Instance.Animal.Value;
    private DecoDatabase DecoDB => DataManager.Instance.Deco.Value;
    private CostumeDatabase CostumeDB => DataManager.Instance.Costume.Value;
    private ArtifactDatabase ArtifactDB => DataManager.Instance.Artifact.Value;
    private Profile_iconDatabase IconDB => DataManager.Instance.Profile.Value;

    private readonly IResourceLoader _loader = new ResourcesLoader();

    public IReadOnlyList<ProfileEntry> GetAll()
    {
        var list = new List<ProfileEntry>();
        list.AddRange(GetByType(ProfileType.animal));
        list.AddRange(GetByType(ProfileType.deco));
        list.AddRange(GetByType(ProfileType.costume));
        list.AddRange(GetByType(ProfileType.artifact));
        list.AddRange(GetByType(ProfileType.icon));
        return list;
    }

    public IReadOnlyList<ProfileEntry> GetByType(ProfileType type)
    {
        var result = new List<ProfileEntry>();

        switch (type)
        {
            case ProfileType.animal:
                if (AnimalDB != null)
                {
                    foreach (var d in AnimalDB)
                    {
                        result.Add(new ProfileEntry
                        {
                            Id = d.animal_id,
                            Name = d.animal_name,
                            Icon = d.GetIcon(_loader),
                            Type = ProfileType.animal,
                            IsUnlocked = true
                        });
                    }
                }
                break;

            case ProfileType.deco:
                if (DecoDB != null)
                {
                    foreach (var d in DecoDB)
                    {
                        result.Add(new ProfileEntry
                        {
                            Id = d.deco_id,
                            Name = d.deco_name,
                            Icon = d.GetIcon(_loader),
                            Type = ProfileType.deco,
                            IsUnlocked = true
                        });
                    }
                }
                break;

            case ProfileType.costume:
                if (CostumeDB != null)
                {
                    foreach (var d in CostumeDB)
                    {
                        result.Add(new ProfileEntry
                        {
                            Id = d.costume_id,
                            Name = d.costume_name,
                            Icon = d.GetIcon(_loader),
                            Type = ProfileType.costume,
                            IsUnlocked = true
                        });
                    }
                }
                break;

            case ProfileType.artifact:
                if (ArtifactDB != null)
                {
                    foreach (var d in ArtifactDB)
                    {
                        result.Add(new ProfileEntry
                        {
                            Id = d.artifact_id,
                            Name = d.artifact_name,
                            Icon = d.GetIcon(_loader),
                            Type = ProfileType.artifact,
                            IsUnlocked = true
                        });
                    }
                }
                break;

            case ProfileType.icon:
                if (IconDB != null)
                {
                    foreach (var d in IconDB)
                    {
                        result.Add(new ProfileEntry
                        {
                            Id = d.icon_id,
                            Name = d.icon_name,
                            Icon = d.GetIcon(_loader),
                            Type = ProfileType.icon,
                            IsUnlocked = true
                        });
                    }
                }
                break;
        }

        return result;
    }
}