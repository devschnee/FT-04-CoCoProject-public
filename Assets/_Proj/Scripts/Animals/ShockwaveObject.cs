using UnityEngine;

/// <summary>
/// 충격파 판정/이동용 태그 컴포넌트
/// </summary>
[DisallowMultipleComponent]
public class ShockwaveObject : MonoBehaviour
{
    [Header("Rules")]
    public bool isFixed = false;      // 고정O/X (false = 밀림 대상)
    public bool passThrough = false;  // 통과O/X (고정O & 통과X = 차폐물)

    [Header("Grid-ish")]
    public float tileSize = 1f;

    [Header("Motion")]
    public float move1TileSeconds = 1f; // 1칸 이동에 걸리는 시간

    // 내부 캐시(컨트롤러에서 사용하진 않지만 확장 대비)
    [HideInInspector] public int gx, gz, gh;
}
