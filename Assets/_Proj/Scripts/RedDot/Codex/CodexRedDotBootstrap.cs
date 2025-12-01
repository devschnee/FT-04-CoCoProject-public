using System.Collections;
using UnityEngine;

/// <summary>
/// 로비에서 도감 빨간점 초기 계산해주는 부트스트랩
/// </summary>
public class CodexRedDotBootstrap : MonoBehaviour
{
    private IEnumerator Start()
    {
        // UserData / Codex 로딩될 때까지 대기
        while (UserData.Local == null ||
               UserData.Local.codex == null)
        {
            yield return null;
        }

        // ⭐ 처음 한 번 도감 빨간점 상태 계산
        Debug.Log("[CodexRedDotBootstrap] Initial Recalculate");
        CodexRedDotManager.Recalculate();
    }
}
