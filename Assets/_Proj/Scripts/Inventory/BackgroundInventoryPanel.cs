using UnityEngine;
using UnityEngine.UI;

public class BackgroundInventoryPanel : MonoBehaviour
{
    [Header("DB & UI")]
    [SerializeField] private BackgroundDatabase bgDB;
    [SerializeField] private RectTransform content;
    [SerializeField] private GenericInvSlot slotPrefab;

    private ResourcesLoader _loader;

    private void Awake()
    {
        _loader = new ResourcesLoader();
    }

    public void OnEnable() => Rebuild();

    public void Rebuild()
    {
        if (!bgDB || !content || !slotPrefab) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        foreach (var data in bgDB.bgList)
        {
            if (data == null) continue;
            var slot = Instantiate(slotPrefab, content);
            slot.SetIcon(data.GetIcon(_loader));
            slot.SetCountVisible(false);
            slot.SetOnClick(() =>
            {
                Debug.Log($"[BackgroundInventory] 클릭: {data.bg_name} ({data.bg_id})");
                // TODO: 실제 배경 적용 로직 (예: RenderSettings.skybox = data.GetMaterial(_loader);)
            });
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
}
