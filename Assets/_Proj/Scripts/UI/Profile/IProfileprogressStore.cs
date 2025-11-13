using System.Collections.Generic;

public interface IProfileProgressStore
{
    bool IsUnlocked(ProfileType type, int id);
    void Unlock(ProfileType type, int id);
    Dictionary<ProfileType, HashSet<int>> LoadAll();
}