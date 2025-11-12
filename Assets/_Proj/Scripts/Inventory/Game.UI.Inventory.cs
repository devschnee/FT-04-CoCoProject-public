using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Inventory;
using Game.UI.Inventory.Slot;

namespace Game.UI.Inventory
{
    public enum InventoryCategory { Home, Background, Animal, Deco }

    /// <summary>
    /// 통합 인벤토리 패널.
    /// - category 에 따라 Home/Background/Animal/Deco를 한 컴포넌트에서 그린다.
    /// - 슬롯은 UnifiedInvSlot 사용(아이콘/수량/클릭 콜백만 담당)
    /// - 데코 수량 변화는 InventoryService 이벤트로 즉시 갱신
    /// - 동물은 씬 스캔하여 화면에 있는 id는 슬롯 숨김
    /// </summary>
    public class UnifiedInventoryPanel : MonoBehaviour
    {
        [Header("Category")]
        [SerializeField] private InventoryCategory category = InventoryCategory.Deco;

        [Header("UI")]
        [SerializeField] private RectTransform content;
        [SerializeField] private UnifiedInvSlot slotPrefab;

        [Header("Databases")]
        [SerializeField] private HomeDatabase homeDB;
        [SerializeField] private BackgroundDatabase backgroundDB;
        [SerializeField] private AnimalDatabase animalDB;
        [SerializeField] private DecoDatabase decoDB;

        private readonly List<UnifiedInvSlot> _pool = new(128);
        private int _poolUsed = 0;

        private readonly Dictionary<int, UnifiedInvSlot> _slotById = new();
        private readonly HashSet<int> _hiddenAnimalIds = new();

        private ResourcesLoader _loader;
        private EditModeController _edit;

        private void Awake()
        {
            _loader = new ResourcesLoader();
            if (!_edit) _edit = FindFirstObjectByType<EditModeController>();
        }

        private void OnEnable()
        {
            if (InventoryService.I != null)
                InventoryService.I.OnChanged += OnInvChanged;

            Rebuild();
        }

        private void OnDisable()
        {
            if (InventoryService.I != null)
                InventoryService.I.OnChanged -= OnInvChanged;
        }

        // ─────────────────────────────────────────────
        // Public
        // ─────────────────────────────────────────────
        public void SetCategory(InventoryCategory cat)
        {
            if (category == cat) return;
            category = cat;
            Rebuild();
        }

        public void Rebuild()
        {
            if (!content || !slotPrefab) return;

            _slotById.Clear();
            BeginPool();

            switch (category)
            {
                case InventoryCategory.Home:
                    BuildHome();
                    break;

                case InventoryCategory.Background:
                    BuildBackground();
                    break;

                case InventoryCategory.Animal:
                    BuildAnimal();
                    break;

                case InventoryCategory.Deco:
                    BuildDeco();
                    break;
            }

            EndPool();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        // ─────────────────────────────────────────────
        // InventoryService 이벤트 → 데코 수량 라벨 갱신
        // ─────────────────────────────────────────────
        private void OnInvChanged(PlaceableCategory cat, int id, int newCount)
        {
            if (category != InventoryCategory.Deco) return;
            if (cat != PlaceableCategory.Deco) return;

            if (_slotById.TryGetValue(id, out var slot) && slot)
            {
                slot.SetCount(newCount > 1 ? $"x{newCount}" : (newCount == 1 ? "" : "0"));
            }
        }

        // ─────────────────────────────────────────────
        // Builders
        // ─────────────────────────────────────────────
        private void BuildHome()
        {
            if (!homeDB) return;

            foreach (var data in homeDB.homeList)
            {
                if (data == null) continue;

                var slot = GetSlot();
                slot.SetIcon(data.GetIcon(_loader));
                slot.SetName(string.IsNullOrEmpty(data.home_name) ? $"Home {data.home_id}" : data.home_name);

                // 집: 수량 필요 없음
                slot.SetCount(null);

                var localData = data;
                slot.SetOnClick(() =>
                {
                    if (!_edit) _edit = FindFirstObjectByType<EditModeController>();
                    if (!_edit) return;
                    if (_edit.IsHomePreviewActive) return; // 기존 로직 유지
                    _edit.PreviewSwapHome(new HomePlaceable(localData));
                });
            }
        }

        private void BuildBackground()
        {
            if (!backgroundDB) return;

            foreach (var data in backgroundDB.bgList)
            {
                if (data == null) continue;

                var slot = GetSlot();
                slot.SetIcon(data.GetIcon(_loader));
                slot.SetCount(null); // 수량 없음

                var localData = data;
                slot.SetOnClick(() =>
                {
                    // TODO: 실제 배경 적용 로직 (머티리얼/스카이박스 등)
                    Debug.Log($"[BackgroundInventory] 클릭: {localData.bg_name} ({localData.bg_id})");
                });
            }
        }

        private void BuildAnimal()
        {
            if (!animalDB) return;

            RefreshHiddenAnimalsFromScene();

            foreach (var data in animalDB.animalList)
            {
                if (data == null) continue;
                var id = data.animal_id;
                bool visible = !_hiddenAnimalIds.Contains(id);

                var slot = GetSlot();
                slot.gameObject.SetActive(visible);
                slot.SetIcon(data.GetIcon(_loader));
                slot.SetName(string.IsNullOrEmpty(data.animal_name) ? $"Animal {id}" : data.animal_name);

                // 동물: 수량 필요 없음
                slot.SetCount(null);

                var localData = data;
                slot.SetOnClick(() =>
                {
                    if (!_edit) _edit = FindFirstObjectByType<EditModeController>();
                    if (!_edit) return;
                    _edit.SpawnFromPlaceable(new AnimalPlaceable(localData), PlaceableCategory.Animal);

                    // 한 마리 씬에 있으면 슬롯 숨김(기존 동작 유지)
                    _hiddenAnimalIds.Add(id);
                    if (_slotById.TryGetValue(id, out var s) && s) s.gameObject.SetActive(false);
                });

                _slotById[id] = slot;
            }

            EditModeController.AnimalReturnedToInventory -= OnAnimalReturned;
            EditModeController.AnimalReturnedToInventory += OnAnimalReturned;
        }

        private void BuildDeco()
        {
            if (!decoDB) return;

            foreach (var data in decoDB.decoList)
            {
                if (data == null) continue;
                var id = data.deco_id;

                var slot = GetSlot();
                slot.SetIcon(data.GetIcon(_loader));
                slot.SetName(string.IsNullOrEmpty(data.deco_name) ? $"Deco {id}" : data.deco_name);

                // ★ 수량: DB의 deco_stack 을 그대로 표시
                int c = Mathf.Max(0, data.deco_stack);
                slot.SetCount(c > 1 ? $"x{c}" : (c == 1 ? "" : "0"));

                var localData = data;
                slot.SetOnClick(() =>
                {
                    // 클릭 동작은 기존대로(스폰). 수량 갱신을 DB로 즉시 반영하고 싶다면 아래 주석 참고.
                    if (!_edit) _edit = FindFirstObjectByType<EditModeController>();
                    if (_edit != null)
                    {
                        _edit.SpawnFromDecoData(localData);

                        // (선택) DB 수량도 즉시 감소하여 라벨 반영하고 싶으면:
                        // if (localData.deco_stack > 0) localData.deco_stack--;
                        // slot.SetCount(localData.deco_stack > 1 ? $"x{localData.deco_stack}" :
                        //               (localData.deco_stack == 1 ? "" : "0"));
                    }
                });

                _slotById[id] = slot;
            }
        }

        // ─────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────
        private void OnAnimalReturned(int id)
        {
            _hiddenAnimalIds.Remove(id);
            if (_slotById.TryGetValue(id, out var slot) && slot)
                slot.gameObject.SetActive(true);
        }

        private void RefreshHiddenAnimalsFromScene()
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
                if (t.category != PlaceableCategory.Animal) continue;
                _hiddenAnimalIds.Add(t.id);
            }
        }

        private void BeginPool()
        {
            _poolUsed = 0;
            for (int i = 0; i < _pool.Count; i++)
                if (_pool[i]) _pool[i].gameObject.SetActive(false);
        }

        private UnifiedInvSlot GetSlot()
        {
            UnifiedInvSlot s;
            if (_poolUsed < _pool.Count && _pool[_poolUsed])
            {
                s = _pool[_poolUsed++];
                s.gameObject.SetActive(true);
                return s;
            }
            s = Instantiate(slotPrefab, content);
            if (s.transform is RectTransform rt)
            {
                rt.localScale = Vector3.one;
                rt.anchoredPosition3D = Vector3.zero;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            _pool.Add(s);
            _poolUsed++;
            return s;
        }

        private void EndPool()
        {
            for (int i = _poolUsed; i < _pool.Count; i++)
            {
                var s = _pool[i];
                if (s) s.gameObject.SetActive(false);
            }
        }
    }
}
