using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 창이 열릴 때 DB 기준으로 슬롯을 전부 다시 만든다.
/// ScrollView 안의 Content 에 DecoSlot 프리팹을 쭉 깔아주는 역할만 한다.
/// </summary>
public class DecoInventoryPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform content;   // 슬롯들이 붙을 컨테이너
    [SerializeField] private DecoSlot slotPrefab;     // 개별 슬롯 프리팹

    private void OnEnable()
    {
        Rebuild();
    }

    /// <summary>
    /// 현재 DB에 있는 deco 리스트를 전부 슬롯으로 만든다.
    /// </summary>
    public void Rebuild()
    {
        // 필수 참조 체크
        if (!content || !slotPrefab) return;
        if (DecoInventoryRuntime.I == null || DecoInventoryRuntime.I.DB == null) return;

        // 1) 기존 슬롯들 제거
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        // 2) DB 순회하면서 슬롯 생성
        var db = DecoInventoryRuntime.I.DB;
        foreach (var data in db.decoList)
        {
            if (data == null) continue;

            var slot = Instantiate(slotPrefab, content);

            // RectTransform 기본값으로 맞춰주기
            if (slot.transform is RectTransform rt)
            {
                rt.localScale = Vector3.one;
                rt.anchoredPosition3D = Vector3.zero;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            slot.SetDecoId(data.deco_id);
        }

        // 3) 레이아웃 강제 갱신 (Grid / VerticalLayout 같은 거 있을 때)
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
}
