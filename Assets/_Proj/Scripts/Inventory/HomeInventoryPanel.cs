using UnityEngine;
using UnityEngine.UI;

public class HomeInventoryPanel : MonoBehaviour
{
    [Header("DB & UI")]
    [SerializeField] private HomeDatabase homeDB;          // ← 인스펙터에 할당
    [SerializeField] private RectTransform content;        // Grid/Content
    [SerializeField] private GenericInvSlot slotPrefab;    // 공용 슬롯 프리팹

    private EditModeController _edit;

    public void OnEnable() => Rebuild();

    public void Rebuild()
    {
        if (!homeDB || !content || !slotPrefab) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        foreach (var data in homeDB.homeList)
        {
            if (data == null) continue;
            var slot = Instantiate(slotPrefab, content);

            var icon = data.GetIcon(new ResourcesLoader());
            slot.SetIcon(icon);
            slot.SetCountVisible(false);

            // ✅ 집은 프리뷰 교체로 동작
            slot.SetOnClick(() =>
            {
                if (_edit == null) _edit = FindFirstObjectByType<EditModeController>();
                if (_edit == null) return;
                _edit.PreviewSwapHome(new HomePlaceable(data));
            });
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
}
