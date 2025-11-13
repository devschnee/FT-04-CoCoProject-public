using System.Collections.Generic;
using UnityEngine;

public class ProfileService : MonoBehaviour
{
    [SerializeField] private MonoBehaviour sourceBehaviour;
    [SerializeField] private MonoBehaviour progressStoreBehaviour;

    private IProfileSource _source;
    private IProfileProgressStore _progress;

    private void Awake()
    {
        _source = sourceBehaviour as IProfileSource;
        _progress = progressStoreBehaviour as IProfileProgressStore;
    }

    public IReadOnlyList<ProfileEntry> GetByType(ProfileType type)
    {
        var entries = _source.GetByType(type);
        if (_progress == null) return entries;

        foreach (var e in entries)
            e.IsUnlocked = _progress.IsUnlocked(type, e.Id);

        return entries;
    }
}