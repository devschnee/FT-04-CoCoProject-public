using System;
using UnityEngine;

public struct CodexRedDotState
{
    /// <summary>도감 전체 중 하나라도 새로 열린 항목이 있는지</summary>
    public bool any;

    /// <summary>각 카테고리별 새로 열린 항목 존재 여부</summary>
    public bool hasAnimal;
    public bool hasDeco;
    public bool hasCostume;
    public bool hasArtifact;
    public bool hasHome;

    /// <summary>합쳐서라도 하나라도 있으면 true (편의용)</summary>
    public bool hasAny =>
        hasAnimal || hasDeco || hasCostume || hasArtifact || hasHome;
}

/// <summary>
/// 도감 빨간점 계산 전담 매니저
/// - 기준: UserData.Local.codex.newlyUnlocked
/// </summary>
public static class CodexRedDotManager
{
    public static CodexRedDotState Current { get; private set; }

    public static event Action<CodexRedDotState> OnStateChanged;

    /// <summary>
    /// 새로 해금/확인 후 빨간점 상태 다시 계산할 때 호출
    /// </summary>
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

    /// <summary>
    /// 전부 초기화해서 빨간점 강제로 끄고 싶을 때 사용
    /// </summary>
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
            if (!newly.TryGetValue(key, out var set) || set == null)
                return false;
            return set.Count > 0;
        }

        state.hasAnimal = HasNew(CodexType.animal);
        state.hasDeco = HasNew(CodexType.deco);
        state.hasCostume = HasNew(CodexType.costume);
        state.hasArtifact = HasNew(CodexType.artifact);
        state.hasHome = HasNew(CodexType.home);

        // 전체 any 플래그
        state.any = state.hasAnimal ||
                    state.hasDeco ||
                    state.hasCostume ||
                    state.hasArtifact ||
                    state.hasHome;

        return state;
    }
}
