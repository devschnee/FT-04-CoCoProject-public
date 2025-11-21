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
        ///  InventoryService가 Deco 수량 변화를 통지할 때 UI 즉시 갱신
        /// - newCount == 0  → 슬롯 숨김
        /// - newCount  > 0  →
        ///     - 슬롯 있으면: 수량만 갱신
        ///     - 슬롯 없으면: Rebuild()로 전체 다시 그리고 슬롯 생성
        /// </summary>
        private void OnInvChanged(int id, int newCount)
        {
            // 이 패널이 Deco 인벤이 아닐 때는 무시
            if (category != InventoryCategory.Deco)
                return;

            // Deco id 범위: 10000 < id < 20000
            if (id <= 10000 || id >= 20000)
                return;

            // 1) 0개가 된 경우 → 슬롯 숨김
            if (newCount <= 0)
            {
                if (_slotById.TryGetValue(id, out var slot) && slot)
                    slot.gameObject.SetActive(false);
                return;
            }

            // 2) 슬롯이 이미 있으면 → 수량만 업데이트
            if (_slotById.TryGetValue(id, out var existingSlot) && existingSlot)
            {
                existingSlot.SetCount(newCount.ToString());
                existingSlot.gameObject.SetActive(true);
                return;
            }

            // 3) 슬롯이 없으면 → 전체 Rebuild
            Rebuild();
        }





        // ─────────────────────────────────────────────
        // Builders
        // ─────────────────────────────────────────────
        /// <summary>
        ///  Home 인벤토리 빌드 (UserData 기반)
        /// - 유저가 실제 소유한 집(Home)만 표시
        /// - 아이콘/이름은 Provider(DataManager.Home)에서 id로 조회
        /// - 수량 개념 없음 (집은 항상 1개 단위)
        /// </summary>
        private void BuildHome()
        {
            // ───────────────────────────────────────────────
            // 0) UserData 또는 inventory가 유효하지 않으면 중단
            // ───────────────────────────────────────────────
            if (UserData.Local == null || UserData.Local.inventory == null)
                return;

            var inventoryItems = UserData.Local.inventory.items;
            if (inventoryItems == null || inventoryItems.Count == 0)
                return;

            // ───────────────────────────────────────────────
            // 1) 유저 인벤토리에서 “Home id만” 필터링
            //    Home id 규칙: 40000 ~ 50000
            // ───────────────────────────────────────────────
            foreach (var kv in inventoryItems)
            {
                string key = kv.Key;    // "40001" 형태 문자열
                if (!int.TryParse(key, out int id))
                    continue;

                if (id <= 40000 || id >= 50000)
                    continue;

                // ───────────────────────────────────────────
                // 2) HomeProvider에서 실제 HomeData를 가져오기
                // ───────────────────────────────────────────
                var data = DataManager.Instance.Home.GetData(id);
                if (data == null)
                {
                    Debug.LogWarning($"[Inventory] HomeData not found. id={id}");
                    continue;
                }

                // ───────────────────────────────────────────
                // 3) 슬롯 생성
                // ───────────────────────────────────────────
                var slot = GetSlot();

                slot.SetIcon(data.GetIcon(_loader));

                string homeName = string.IsNullOrEmpty(data.home_name)
                    ? $"Home {id}"
                    : data.home_name;

                slot.SetName(homeName);

                // 집은 수량 개념 없음 → count 표시 끔
                slot.SetCount(null);

                // ───────────────────────────────────────────
                // 4) 클릭 시: 홈 프리뷰/교체 로직 실행
                // ───────────────────────────────────────────
                var localData = data;
                slot.SetOnClick(() =>
                {
                    if (!_edit) _edit = FindFirstObjectByType<EditModeController>();
                    if (_edit == null) return;

                    // 기존 EditModeController의 집 프리뷰 로직 유지
                    if (_edit.IsHomePreviewActive) return;

                    _edit.PreviewSwapHome(new HomePlaceable(localData));
                });

                // ───────────────────────────────────────────
                // 5) id → slot 저장 (추후 필요 대비)
                // ───────────────────────────────────────────
                _slotById[id] = slot;
            }
        }


        /// <summary>
        ///  Background 인벤토리 빌드 (UserData 기반)
        /// - 유저가 실제로 소유한 Background만 슬롯 생성
        /// - 0개인 Background는 슬롯 생성하지 않음
        /// - 아이콘/이름은 DataManager.BackgroundProvider에서 id로 조회
        /// - 수량 개념 없음 (배경은 1개 단위)
        /// </summary>
        private void BuildBackground()
        {
            // ───────────────────────────────────────────────
            // 0) UserData 유효성 검사
            // ───────────────────────────────────────────────
            if (UserData.Local == null || UserData.Local.inventory == null)
                return;

            var inventoryItems = UserData.Local.inventory.items;
            if (inventoryItems == null || inventoryItems.Count == 0)
                return;

            // ───────────────────────────────────────────────
            // 1) 유저 인벤토리에서 Background ID만 필터링
            //    Background id 범위: 60000 ≤ id < 70000
            // ───────────────────────────────────────────────
            foreach (var kv in inventoryItems)
            {
                string key = kv.Key;
                int ownedCount = kv.Value;

                if (!int.TryParse(key, out int id))
                    continue;

                // Background id 규칙 적용
                if (id < 60000 || id >= 70000)
                    continue;

                // 0개는 표시하지 않음
                if (ownedCount <= 0)
                    continue;

                // ───────────────────────────────────────────
                // 2) Provider에서 BackgroundData 조회
                // ───────────────────────────────────────────
                var data = DataManager.Instance.Background.GetData(id);
                if (data == null)
                {
                    Debug.LogWarning($"[Inventory] BackgroundData not found. id={id}");
                    continue;
                }

                // ───────────────────────────────────────────
                // 3) 슬롯 생성
                // ───────────────────────────────────────────
                var slot = GetSlot();

                // 아이콘
                slot.SetIcon(data.GetIcon(_loader));

                // 이름(보이게 할지 말지는 팀에서 결정하면 됨)
                string bgName = string.IsNullOrEmpty(data.bg_name)
                    ? $"BG {id}"
                    : data.bg_name;
                slot.SetName(bgName);

                // Background는 수량 없음 → count 비활성
                slot.SetCount(null);

                // ───────────────────────────────────────────
                // 4) 클릭 시 동작 (배경 적용)
                // ───────────────────────────────────────────
                var localData = data;
                slot.SetOnClick(() =>
                {
                    Debug.Log($"[BackgroundInventory] 선택됨: {localData.bg_name} ({localData.bg_id})");

                    // TODO: 실제 배경 적용 로직
                    // ex: BackgroundManager.Instance.Apply(localData);
                });

                // 필요하면 아래처럼 slotById에 저장하여
                // Background 변경 이벤트가 생겼을 때 UI 업데이트 가능
                // _slotById[id] = slot;
            }
        }


        /// <summary>
        ///  Animal 인벤토리 빌드 (UserData 기반)
        /// - 유저가 실제로 가진 Animal만 표시
        /// - 아이콘/이름은 Provider(DataManager.Animal)로 id 조회
        /// - 씬에 존재하는 Animal id는 슬롯 숨김 (에딧모드 기존 로직 유지)
        /// </summary>
        private void BuildAnimal()
        {
            // ───────────────────────────────────────────────
            // 0) UserData가 없으면 중단
            // ───────────────────────────────────────────────
            if (UserData.Local == null || UserData.Local.inventory == null)
                return;

            var inventoryItems = UserData.Local.inventory.items;
            if (inventoryItems == null || inventoryItems.Count == 0)
                return;

            // ───────────────────────────────────────────────
            // 1) 씬에 이미 배치된 Animal id 목록 수집
            // ───────────────────────────────────────────────
            RefreshHiddenAnimalsFromScene();

            // AnimalReturned 이벤트 등록 (중복 방지 처리)
            EditModeController.AnimalReturnedToInventory -= OnAnimalReturned;
            EditModeController.AnimalReturnedToInventory += OnAnimalReturned;

            // ───────────────────────────────────────────────
            // 2) 유저 인벤토리 순회 → Animal id 필터링
            // ───────────────────────────────────────────────
            foreach (var kv in inventoryItems)
            {
                string key = kv.Key;    // "30001"
                if (!int.TryParse(key, out int id))
                    continue;

                // Animal id 범위: 30000 ~ 40000
                if (id <= 30000 || id >= 40000)
                    continue;

                // ───────────────────────────────────────────
                // 3) AnimalProvider에서 실제 데이터 가져오기
                // ───────────────────────────────────────────
                var data = DataManager.Instance.Animal.GetData(id);
                if (data == null)
                {
                    Debug.LogWarning($"[Inventory] AnimalData not found. id={id}");
                    continue;
                }

                // ───────────────────────────────────────────
                // 4) 슬롯 생성
                // ───────────────────────────────────────────
                var slot = GetSlot();

                slot.SetIcon(data.GetIcon(_loader));

                string displayName = string.IsNullOrEmpty(data.animal_name)
                    ? $"Animal {id}"
                    : data.animal_name;

                slot.SetName(displayName);

                // Animal은 수량 개념 없음
                slot.SetCount(null);    

                // ───────────────────────────────────────────
                // 5) 씬에 이미 있는 Animal → 슬롯 숨김
                // ───────────────────────────────────────────
                bool alreadyPlaced = _hiddenAnimalIds.Contains(id);
                slot.gameObject.SetActive(!alreadyPlaced);

                // ───────────────────────────────────────────
                // 6) 클릭 시: Animal 생성(Spawn) + 슬롯 숨김
                // ───────────────────────────────────────────
                var localData = data;
                slot.SetOnClick(() =>
                {
                    if (!_edit) _edit = FindFirstObjectByType<EditModeController>();
                    if (_edit == null) return;

                    _edit.SpawnFromPlaceable(new AnimalPlaceable(localData), PlaceableCategory.Animal);

                    // 생성되면 슬롯 숨김
                    _hiddenAnimalIds.Add(id);
                    if (_slotById.TryGetValue(id, out var s) && s)
                        s.gameObject.SetActive(false);
                });

                // ───────────────────────────────────────────
                // 7) id → slot 매핑 저장
                // ───────────────────────────────────────────
                _slotById[id] = slot;
            }
        }

        /// <summary>
        ///  Deco 인벤토리 빌드 (UserData 기반)
        /// - 유저가 실제 소유한 Deco만 표시
        /// - 0개인 Deco는 슬롯 생성하지 않음
        /// - 수량은 InventoryService를 기준으로 표시
        /// - 아이콘/이름은 DataManager.DecoProvider로 얻음
        /// </summary>
        private void BuildDeco()
        {
            // ───────────────────────────────────────────────
            // 0) UserData 유효성 검사
            // ───────────────────────────────────────────────
            if (UserData.Local == null || UserData.Local.inventory == null)
                return;

            var inventoryItems = UserData.Local.inventory.items;
            if (inventoryItems == null || inventoryItems.Count == 0)
                return;

            // ───────────────────────────────────────────────
            // 1) 유저가 가진 Deco 아이템만 순회
            // ───────────────────────────────────────────────
            foreach (var kv in inventoryItems)
            {
                string key = kv.Key;        // "10001"
                int ownedCount = kv.Value;  // 유저가 가진 수량

                if (!int.TryParse(key, out int id))
                    continue;

                // Deco id 규칙: 10000 < id < 20000
                if (id <= 10000 || id >= 20000)
                    continue;

                // ❗ 유저 수량이 0개면 아예 슬롯을 만들지 않음
                if (ownedCount <= 0)
                    continue;

                // ───────────────────────────────────────────
                // 2) Provider에서 DecoData 조회
                // ───────────────────────────────────────────
                var data = DataManager.Instance.Deco.GetData(id);
                if (data == null)
                {
                    Debug.LogWarning($"[Inventory] DecoData not found for id={id}");
                    continue;
                }

                // ───────────────────────────────────────────
                // 3) 슬롯 생성
                // ───────────────────────────────────────────
                var slot = GetSlot();

                // 아이콘
                slot.SetIcon(data.GetIcon(_loader));

                // 이름
                string decoName = string.IsNullOrEmpty(data.deco_name)
                    ? $"Deco {id}"
                    : data.deco_name;
                slot.SetName(decoName);

                // ───────────────────────────────────────────
                // 4) 수량 표시 (1개도 숫자가 보이도록 수정)
                // ───────────────────────────────────────────
                int trueCount = InventoryService.I.GetCount(id);
                string countLabel =
                    trueCount > 0 ? trueCount.ToString() : ""; // 1 → "1", 2 → "2", ...

                slot.SetCount(countLabel);

                // ───────────────────────────────────────────
                // 5) 클릭 시 동작
                //    - 먼저 인벤토리에서 1개 소비 (TryConsume)
                //    - 성공하면 Deco 스폰
                //    - 실패하면(재고 없음) 아무 것도 안 함
                // ───────────────────────────────────────────
                var localData = data; // lambda 캡처용
                slot.SetOnClick(() =>
                {
                    // 수량 확인 + 소비
                    if (!InventoryService.I.TryConsume(id, 1))
                    {
                        Debug.Log($"[Inventory] Deco id={id} 수량 부족");
                        return;
                    }

                    if (!_edit) _edit = FindFirstObjectByType<EditModeController>();
                    if (_edit != null)
                    {
                        _edit.SpawnFromDecoData(localData);
                    }
                });

                // ───────────────────────────────────────────
                // 6) slotById에 저장 → OnInvChanged에서 갱신 가능
                // ───────────────────────────────────────────
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
