using System.Collections.Generic;

public interface IProfileSource
{
    IReadOnlyList<ProfileEntry> GetAll();
    IReadOnlyList<ProfileEntry> GetByType(ProfileType type);
}