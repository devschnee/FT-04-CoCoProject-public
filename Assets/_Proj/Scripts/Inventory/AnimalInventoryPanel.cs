using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimalInventoryPanel : MonoBehaviour
{
    [Header("DB & UI")]
    [SerializeField] private AnimalDatabase animalDB;
    [SerializeField] private RectTransform content;
    [SerializeField] private GenericInvSlot slotPrefab;

    private static EditModeController _edit;
    private ResourcesLoader _loader;

    private readonly Dictionary<int, GenericInvSlot> _slotById = new();
    private readonly HashSet<int> _hiddenAnimalIds = new();

    // (선택) 간단 풀링 사용 중이면 유지
    private readonly List<GenericInvSlot> _pool = new(64);
    private int _poolUsedCount = 0;

    private void Awake()
    {
        EnsureEditRef();
        _loader ??= new ResourcesLoader();
    }

    private void OnEnable()
    {
        EditModeController.AnimalTakenFromInventory += OnAnimalTaken;
        EditModeController.AnimalReturnedToInventory += OnAnimalReturned;

        Rebuild();
    }

    private void OnDisable()
    {
        EditModeController.AnimalTakenFromInventory -= OnAnimalTaken;
        EditModeController.AnimalReturnedToInventory -= OnAnimalReturned;
    }

    private void OnDestroy()
    {
        EditModeController.AnimalTakenFromInventory -= OnAnimalTaken;
        EditModeController.AnimalReturnedToInventory -= OnAnimalReturned;
    }

    public void Rebuild()
    {
        if (!animalDB || !content || !slotPrefab) return;

        // 🔸 씬 상태를 먼저 반영: 화면에 존재하는 동물 id는 슬롯 숨김 집합에 넣는다
        RefreshHiddenFromScene();

        _slotById.Clear();
        BeginPoolFrame();

        var list = animalDB.animalList;
        if (list != null)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var data = list[i];
                if (data == null) continue;

                var slot = CreateOrReuseSlot();
                slot.SetIcon(data.GetIcon(_loader));
                slot.SetCountVisible(false);

                int id = data.animal_id;
                BindClick(slot, id, data);

                _slotById[id] = slot;

                bool visible = !_hiddenAnimalIds.Contains(id);
                slot.gameObject.SetActive(visible);
            }
        }

        DisableUnusedPool();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    // ── 이벤트 콜백 ─────────────────────────────────────────
    private void OnAnimalTaken(int id)
    {
        _hiddenAnimalIds.Add(id);
        SetSlotVisible(id, false);
    }

    private void OnAnimalReturned(int id)
    {
        _hiddenAnimalIds.Remove(id);
        SetSlotVisible(id, true);
    }

    // ── 씬 스캔: 이미 배치된 동물 ID를 숨김 집합에 반영 ─────────────
    private void RefreshHiddenFromScene()
    {
        _hiddenAnimalIds.Clear();

#if UNITY_2022_2_OR_NEWER
        var tags = FindObjectsByType<PlaceableTag>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var tags = Object.FindObjectsOfType<PlaceableTag>();
#endif
        for (int i = 0; i < tags.Length; i++)
        {
            var t = tags[i];
            if (!t || !t.gameObject.activeInHierarchy) continue;

            // 동물만 추린다
            if (t.category != PlaceableCategory.Animal) continue;

            // 임시/확정 상관없이 "화면에 존재"하면 숨김
            // (임시물은 InventoryTempMarker 가 붙어있을 수 있지만, 정책상 동일 처리)
            _hiddenAnimalIds.Add(t.id);
        }
    }

    // ── 풀링 유틸(사용 중이면 그대로 둠) ───────────────────────────
    private void BeginPoolFrame() => _poolUsedCount = 0;

    private GenericInvSlot CreateOrReuseSlot()
    {
        if (_poolUsedCount < _pool.Count)
        {
            var slot = _pool[_poolUsedCount++];
            if (!slot) return CreateNewSlot();
            slot.gameObject.SetActive(true);
            return slot;
        }
        var s = CreateNewSlot();
        _pool.Add(s);
        _poolUsedCount++;
        return s;
    }

    private GenericInvSlot CreateNewSlot()
    {
        var slot = Object.Instantiate(slotPrefab, content);
        if (slot.transform is RectTransform rt)
        {
            rt.localScale = Vector3.one;
            rt.anchoredPosition3D = Vector3.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        return slot;
    }

    private void DisableUnusedPool()
    {
        for (int i = _poolUsedCount; i < _pool.Count; i++)
        {
            var slot = _pool[i];
            if (slot) slot.gameObject.SetActive(false);
        }
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────
    private void BindClick(GenericInvSlot slot, int id, AnimalData data)
    {
        slot.SetOnClick(() =>
        {
            EnsureEditRef();
            _edit?.SpawnFromPlaceable(new AnimalPlaceable(data), PlaceableCategory.Animal);

            // 즉시 반응 + 상태 기억
            _hiddenAnimalIds.Add(id);
            SetSlotVisible(id, false);
        });
    }

    private void SetSlotVisible(int id, bool visible)
    {
        if (_slotById.TryGetValue(id, out var slot) && slot)
            slot.gameObject.SetActive(visible);
    }

    private static void EnsureEditRef()
    {
        if (_edit == null)
            _edit = Object.FindFirstObjectByType<EditModeController>();
    }
}
