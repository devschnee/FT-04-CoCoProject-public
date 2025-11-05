using UnityEngine;
using UnityEngine.UI;

public enum InventoryCategory
{
    Home,
    Background,
    Animal,
    Deco
}

public class GenericInventoryPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform content;
    [SerializeField] private GenericInvSlot slotPrefab;

    [Header("DB")]
    [SerializeField] private HomeDatabase homeDB;
    [SerializeField] private BackgroundDatabase backgroundDB;
    [SerializeField] private AnimalDatabase animalDB;
    [SerializeField] private DecoDatabase decoDB;

    // 캐시
    private static EditModeController _edit;
    private ResourcesLoader _loader;

    private void Awake()
    {
        if (_edit == null) _edit = FindFirstObjectByType<EditModeController>();
        _loader = new ResourcesLoader();
    }

    public void Rebuild(InventoryCategory category)
    {
        if (!content || !slotPrefab) return;

        // 기존 슬롯 제거(이 패널은 빈도 낮아 간단 제거 유지)
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        switch (category)
        {
            case InventoryCategory.Home:
                if (homeDB)
                    foreach (var d in homeDB.homeList)
                        MakeSlot(d?.GetIcon(_loader), d?.home_name, () =>
                        {
                            _edit?.SpawnFromPlaceable(new HomePlaceable(d), PlaceableCategory.Home);
                        });
                break;

            case InventoryCategory.Background:
                if (backgroundDB)
                    foreach (var d in backgroundDB.bgList)
                        MakeSlot(d?.GetIcon(_loader), d?.bg_name, () =>
                        {
                            Debug.Log($"[BackgroundInventory] 클릭: {d.bg_name} ({d.bg_id})");
                            // TODO: 배경 적용 로직
                        });
                break;

            case InventoryCategory.Animal:
                if (animalDB)
                    foreach (var d in animalDB.animalList)
                        MakeSlot(d?.GetIcon(_loader), d?.animal_name, () =>
                        {
                            _edit?.SpawnFromPlaceable(new AnimalPlaceable(d), PlaceableCategory.Animal);
                        });
                break;

            case InventoryCategory.Deco:
                if (decoDB)
                {
                    foreach (var d in decoDB.decoList)
                    {
                        var slot = Instantiate(slotPrefab, content);
                        slot.SetIcon(d?.GetIcon(_loader));

                        int count = (d != null && DecoInventoryRuntime.I != null)
                            ? DecoInventoryRuntime.I.Count(d.deco_id) : 0;
                        slot.SetCountVisible(true, $"x{count}");

                        slot.SetOnClick(() =>
                        {
                            if (d == null || DecoInventoryRuntime.I == null) return;

                            if (DecoInventoryRuntime.I.TryConsume(d.deco_id, 1))
                            {
                                if (_edit == null) _edit = FindFirstObjectByType<EditModeController>();
                                if (_edit != null)
                                {
                                    _edit.SpawnFromDecoData(d);
                                }
                                else
                                {
                                    // 컨트롤러가 없으면 되돌림
                                    DecoInventoryRuntime.I.Add(d.deco_id, 1);
                                }
                            }
                        });
                    }
                }
                break;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private void MakeSlot(Sprite icon, string name, System.Action onClick)
    {
        var slot = Instantiate(slotPrefab, content);
        slot.SetIcon(icon);
        slot.SetCountVisible(false);
        slot.SetOnClick(onClick ?? (() => Debug.Log($"Clicked item: {name}")));
    }
}
