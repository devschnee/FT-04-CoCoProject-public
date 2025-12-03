using System;
using UnityEngine;

public struct CodexRedDotState
{
    public bool any;

    public bool hasAnimal;
    public bool hasDeco;
    public bool hasCostume;
    public bool hasArtifact;
    public bool hasHome;

    public bool hasAny =>
        hasAnimal || hasDeco || hasCostume || hasArtifact || hasHome;
}

public static class CodexRedDotManager
{
    public static CodexRedDotState Current { get; private set; }

    public static event Action<CodexRedDotState> OnStateChanged;

    public static void Recalculate()
    {
        var newState = CalculateInternal();

        Debug.Log($"[CodexRedDotManager] Recalculate: " +
                  $"any={newState.any}, " +
                  $"animal={newState.hasAnimal}, deco={newState.hasDeco}, costume={newState.hasCostume}, " +
                  $"artifact={newState.hasArtifact}, home={newState.hasHome}");

        if (IsSame(Current, newState))
            return;

        Current = newState;
        OnStateChanged?.Invoke(Current);
    }

    public static void ForceClear()
    {
        Current = default;
        OnStateChanged?.Invoke(Current);
    }

    private static bool IsSame(CodexRedDotState a, CodexRedDotState b)
    {
        return a.any == b.any &&
               a.hasAnimal == b.hasAnimal &&
               a.hasDeco == b.hasDeco &&
               a.hasCostume == b.hasCostume &&
               a.hasArtifact == b.hasArtifact &&
               a.hasHome == b.hasHome;
    }

    // ▼ 타입-아이템ID가 맞는지 체크
    private static bool IsValidForType(CodexType type, int itemId)
    {
        switch (type)
        {
            case CodexType.deco: return 10000 < itemId && itemId < 20000;
            case CodexType.costume: return 20000 < itemId && itemId < 30000;
            case CodexType.animal: return 30000 < itemId && itemId < 40000;
            case CodexType.home: return 40000 < itemId && itemId < 50000;

            case CodexType.artifact:
                // ✅ 일반 유물 50000번대 + 재화유물 110001~110003 둘 다 인정
                return (50000 < itemId && itemId < 60000)
                       || (110002 <= itemId && itemId <= 110003);

            default:
                return false;
        }
    }


    private static CodexRedDotState CalculateInternal()
    {
        CodexRedDotState state = default;

        if (UserData.Local == null || UserData.Local.codex == null)
            return state;

        var codex = UserData.Local.codex;
        var newly = codex.newlyUnlocked;
        if (newly == null)
            return state;

        bool HasNew(CodexType type)
        {
            string key = type.ToString().ToLower();
            if (!newly.TryGetValue(key, out var set) || set == null || set.Count == 0)
                return false;

            foreach (var id in set)
            {
                // 🔴 타입–ID 범위가 맞는 애들만 “새로 해금”으로 인정
                if (IsValidForType(type, id))
                    return true;
            }
            return false;
        }

        state.hasAnimal = HasNew(CodexType.animal);
        state.hasDeco = HasNew(CodexType.deco);
        state.hasCostume = HasNew(CodexType.costume);
        state.hasArtifact = HasNew(CodexType.artifact);
        state.hasHome = HasNew(CodexType.home);

        state.any =
            state.hasAnimal ||
            state.hasDeco ||
            state.hasCostume ||
            state.hasArtifact ||
            state.hasHome;

        return state;
    }
}
