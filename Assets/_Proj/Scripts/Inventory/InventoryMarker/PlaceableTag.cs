using UnityEngine;

/// <summary>
/// 씬에 배치된 오브젝트가 어떤 카테고리/ID인지 표시하는 공통 태그
/// (저장/복원/인포 표시/툴바 동작 등에서 공통 사용)
/// </summary>
[DisallowMultipleComponent]
public class PlaceableTag : MonoBehaviour
{
    public PlaceableCategory category;
    public int id;
}
