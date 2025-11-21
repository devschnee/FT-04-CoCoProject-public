using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using Game.Inventory;
using UnityEngine.Assertions.Must;

/// <summary>
/// 편집모드 on/off, 저장/복원, 인벤에서 가져오기
/// (Core에서 이어지는 부분)
/// </summary>
public partial class EditModeController
{
    #region ===== Edit Mode Toggle =====

    /// <summary>편집모드 on/off</summary>
    private void SetEditMode(bool on, bool keepTarget)
    {
        if (IsEditMode == on)
        {
            if (!on && !keepTarget)
                SelectTarget(null);
            return;
        }

        ToggleTopButtons(on);
        IsEditMode = on;

        // 외부 매니저에도 통보
        var mgr = FindAnyObjectByType<EditModeManager>();
        if (mgr != null)
        {
            if (on) mgr.EnterEditMode();
            else mgr.ExitEditMode();
        }

        if (on)
        {
            BlockOrbit = true;

            history.Clear();
            CaptureBaseline();      // 오브젝트 + 인벤토리
            hasUnsavedChanges = false;
            UpdateUndoUI();
            UpdateToolbar();
        }
        else
        {
            // ✅ 편집 종료 시 개별 오브젝트 저장하는 부분에 필터 추가
            if (CurrentTarget && CurrentTarget.TryGetComponent<Draggable>(out var drag))
            {
                drag.SetInvalid(false);

                // ← 여기서 태그로 저장 제외
                if (!ShouldSkipSave(CurrentTarget))
                    drag.SavePosition();

                drag.SetHighlighted(false);
            }
            if (!keepTarget)
                SelectTarget(null);

            lastBeforeDrag = null;
            isDragging = false;

            BlockOrbit = false;

            history.Clear();
            UpdateUndoUI();
            actionToolbar?.Hide();
        }
    }

    // CocoDoogy, Master 태그는 위치 저장에서 제외
    private static bool ShouldSkipSave(Transform t)
    {
        if (!t) return false;
        string tag = t.tag;
        return tag == "CocoDoogy" || tag == "Master";
    }

    private void ToggleTopButtons(bool on)
    {
        if (undoButton) undoButton.gameObject.SetActive(on);
        if (saveButton) saveButton.gameObject.SetActive(on);
        if (backButton) backButton.gameObject.SetActive(on);
    }

    #endregion


    #region ===== Back / Save Buttons =====

    private void OnBackClicked()
    {
        if (hasUnsavedChanges)
        {
            if (exitConfirmPanel) exitConfirmPanel.SetActive(true);
            else ExitWithoutSave(restore: true);
        }
        else
        {
            ExitWithoutSave(restore: false);
        }
    }


    /// <summary>저장 안 하고 나가기 (필요하면 baseline 으로 복구)</summary>
    private void ExitWithoutSave(bool restore)
    {
        CleanupInventoryTemps();

        if (restore)
        {
            // ✅ 집 복구 규칙
            if (homePreview != null)
            {
                Destroy(homePreview.gameObject);
                homePreview = null;
                homePreviewConfirmed = false;
            }

            if (homePrev)
            {
                homePrev.gameObject.SetActive(true);

                // ✅ 여기 추가
                SetLongPressTarget(homePrev);
            }

            RemoveNewlyCreatedSinceBaseline();
            RestoreBaseline();
            SaveAllDraggablePositions();
        }

        SetEditMode(false, keepTarget: false);

        hasUnsavedChanges = false;
        baseline.Clear();
        invBaseline.Clear();
        baselineIds.Clear();
        pendingFromInventory = null;
    }




    private void OnSaveClicked()
    {
        // 🔹 (0) 집 프리뷰가 있지만 OK(확정)를 안 한 상태라면 → 원래 집으로 되돌리기
        if (homePreview != null && !homePreviewConfirmed)
        {
            // 프리뷰 오브젝트 제거
            if (homePreview)
                Destroy(homePreview.gameObject);

            homePreview = null;
            homePreviewConfirmed = false;

            // 원래 집 다시 활성화
            if (homePrev)
            {
                homePrev.gameObject.SetActive(true);
                SetLongPressTarget(homePrev);   // 롱프레스 타깃도 원래 집으로
            }

            HomePreviewActiveChanged?.Invoke(false);
        }

        // (1) OK 안 된 임시물(동물/데코 등) 정리
        CleanupInventoryTemps();

        // (2) Home 확정 처리: 저장 시에만 기존 집 제거 → candidate 승격
        if (homePreview != null && homePreviewConfirmed)
        {
            // 이전 확정집 제거
            if (homePrev) Destroy(homePrev.gameObject);

            // 프리뷰 → 정식
            homePrev = homePreview;
            var ptag = homePrev.GetComponent<PlaceableTag>();
            homePrevId = ptag ? ptag.id : 0;

            // 상태 리셋
            homePreview = null;
            homePreviewConfirmed = false;
        }

        // (3) 씬 Draggable 저장
        SaveAllDraggablePositions();

        // (4) Placeable 저장
        PlaceableStore.I?.SaveAllFromScene();

        // (5) Firebase 동기화
        TrySaveLobbyToFirebase();

        // (6) 상태 정리
        hasUnsavedChanges = false;
        CaptureBaseline();

        // 선택 해제 + 저장 완료 패널
        SelectTarget(null);
        if (savedInfoPanel) savedInfoPanel.SetActive(true);
    }



    #endregion


    #region ===== Cleanup Helpers =====

    private void RemoveNewlyCreatedSinceBaseline()
    {
#if UNITY_2022_2_OR_NEWER
        var tags = FindObjectsByType<PlaceableTag>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
    var tags = FindObjectsOfType<PlaceableTag>();
#endif
        for (int i = 0; i < tags.Length; i++)
        {
            var tag = tags[i];
            if (!tag) continue;
            var tr = tag.transform;

            // 임시물은 이미 제거됨
            if (tr.GetComponent<InventoryTempMarker>()) continue;

            // baseline에 있던 오브젝트는 유지
            if (baselineIds.Contains(tr.GetInstanceID())) continue;

            // ✅ baseline 이후 새로 생긴 확정 배치물 → 제거
            switch (tag.category)
            {
                case PlaceableCategory.Deco:
                    InventoryService.I?.Add(tag.id, 1); // 수량 환원
                    break;

                case PlaceableCategory.Animal:
                    EditModeController.AnimalReturnedToInventory?.Invoke(tag.id); // 슬롯 되살림
                    break;

                case PlaceableCategory.Home:
                    // 홈은 인벤 개념 없음: 단순 제거만
                    break;
            }

            Destroy(tr.gameObject);
        }
    }


    // ✅ 최종 버전
    private void CleanupInventoryTemps()
    {
#if UNITY_2022_2_OR_NEWER
        var temps = FindObjectsByType<InventoryTempMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var temps = Resources.FindObjectsOfTypeAll<InventoryTempMarker>();
#endif
        foreach (var temp in temps)
        {
            if (!temp) continue;
            var tr = temp.transform;

            int decoId = 0;
            var ptag = tr.GetComponent<PlaceableTag>();
            if (ptag != null && ptag.category == PlaceableCategory.Deco)
                decoId = ptag.id;

            if (ptag != null && ptag.category == PlaceableCategory.Animal)
                AnimalReturnedToInventory?.Invoke(ptag.id);

            Object.Destroy(tr.gameObject);

            if (decoId != 0 && InventoryService.I != null)
                InventoryService.I.Add(decoId, 1);

        }
    }

    #endregion


    #region ===== Save Draggable Positions =====

    private void SaveAllDraggablePositions()
    {
#if UNITY_2022_2_OR_NEWER
        var drags = FindObjectsByType<Draggable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var drags = Resources.FindObjectsOfTypeAll<Draggable>();
#endif
        int count = 0;
        foreach (var d in drags)
        {
            if (!d) continue;
            if (!d.gameObject.activeInHierarchy) continue;

            // ✅ CocoDoogy / Master 는 저장하지 않음
            if (ShouldSkipSave(d.transform))
                continue;

            d.SavePosition();
            count++;
        }
        Debug.Log($"[Save] Draggable (활성) {count}개 저장 완료");
    }

    #endregion


    #region ===== UI Wiring =====

    private void WireUndoButton()
    {
        if (!undoButton) return;
        undoButton.gameObject.SetActive(false);
        undoButton.interactable = false;
        undoButton.onClick.RemoveAllListeners();
        undoButton.onClick.AddListener(UndoLastMove);
    }

    private void WireSaveButton()
    {
        if (!saveButton) return;
        saveButton.gameObject.SetActive(false);
        saveButton.onClick.RemoveAllListeners();
        saveButton.onClick.AddListener(OnSaveClicked);
    }

    private void WireBackButton()
    {
        if (!backButton) return;
        backButton.gameObject.SetActive(false);
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(OnBackClicked);
    }

    private void WireExitPanels()
    {
        if (exitConfirmPanel) exitConfirmPanel.SetActive(false);

        if (exitYesButton)
        {
            exitYesButton.onClick.RemoveAllListeners();
            exitYesButton.onClick.AddListener(() =>
            {
                if (exitConfirmPanel) exitConfirmPanel.SetActive(false);
                ExitWithoutSave(restore: true);
            });
        }

        if (exitNoButton)
        {
            exitNoButton.onClick.RemoveAllListeners();
            exitNoButton.onClick.AddListener(() =>
            {
                if (exitConfirmPanel) exitConfirmPanel.SetActive(false);
            });
        }
    }

    private void WireSavedInfoPanel()
    {
        if (savedInfoPanel) savedInfoPanel.SetActive(false);
        if (!savedOkButton) return;

        savedOkButton.onClick.RemoveAllListeners();
        savedOkButton.onClick.AddListener(() => savedInfoPanel?.SetActive(false));
    }

    #endregion


    #region ===== Baseline (Objects + Inventory) =====

    private static bool IsInLayerMask(int layer, LayerMask mask)
        => (mask.value & (1 << layer)) != 0;

    /// <summary>현재 씬 상태(오브젝트 위치 + 인벤 수량)를 baseline 으로 저장</summary>
    private void CaptureBaseline()
    {
        baseline.Clear();
        invBaseline.Clear();
        baselineIds.Clear();
        var set = new HashSet<int>();

        // 1) Draggable 모두
#if UNITY_2022_2_OR_NEWER
        var drags = FindObjectsByType<Draggable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
    var drags = Resources.FindObjectsOfTypeAll<Draggable>();
#endif
        foreach (var d in drags)
        {
            if (!d) continue;
            var tr = d.transform;
            if (tr && set.Add(tr.GetInstanceID()))
            {
                baseline.Add(new ObjSnapshot
                {
                    t = tr,
                    pos = tr.position,
                    rot = tr.rotation,
                    activeSelf = tr.gameObject.activeSelf
                });
            }
        }

        // 2) Draggable은 아니지만 draggableMask에 포함되는 콜라이더들
#if UNITY_2022_2_OR_NEWER
        var cols = FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
    var cols = Resources.FindObjectsOfTypeAll<Collider>();
#endif
        foreach (var c in cols)
        {
            if (!c) continue;
            var go = c.gameObject;
            if (!go.scene.IsValid()) continue;
            if (!IsInLayerMask(go.layer, draggableMask)) continue;

            var tr = c.transform;
            if (tr && set.Add(tr.GetInstanceID()))
            {
                baseline.Add(new ObjSnapshot
                {
                    t = tr,
                    pos = tr.position,
                    rot = tr.rotation,
                    activeSelf = tr.gameObject.activeSelf
                });
            }
        }

        // [2.5] Home은 무조건 baseline에 포함 (Draggable/레이어에 안 걸려도)
        TryCacheExistingHome(); // 필요 시 homePrev 캐시
        if (homePrev)
        {
            var tr = homePrev;
            if (set.Add(tr.GetInstanceID()))
            {
                baseline.Add(new ObjSnapshot
                {
                    t = tr,
                    pos = tr.position,
                    rot = tr.rotation,
                    activeSelf = tr.gameObject.activeSelf
                });
            }
        }

        // ⬇️ baseline에 담긴 트랜스폼들을 baselineIds 집합으로 기록
        for (int i = 0; i < baseline.Count; i++)
        {
            var tr = baseline[i].t;
            if (tr) baselineIds.Add(tr.GetInstanceID());
        }

        // 3) 인벤토리 스냅샷 (InventoryService 기반)
        invBaseline.Clear();
        var all = GetAllDecoCountsFromService();
        for (int i = 0; i < all.Count; i++)
        {
            invBaseline.Add(new InventorySnapshot { id = all[i].id, count = all[i].count });
        }
    }


    /// <summary>baseline 에서 다시 씬을 복구</summary>
    private void RestoreBaseline()
    {
        // 1) 오브젝트 복구
        foreach (var s in baseline)
        {
            if (!s.t) continue;

            if (s.t.gameObject.activeSelf != s.activeSelf)
                s.t.gameObject.SetActive(s.activeSelf);

            var rb = s.t.GetComponent<Rigidbody>();
            if (rb)
            {
                bool prevKinematic = rb.isKinematic;
                var prevDetect = rb.collisionDetectionMode;

                rb.isKinematic = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rb.position = s.pos;
                rb.rotation = s.rot;
                rb.collisionDetectionMode = prevDetect;
                rb.isKinematic = prevKinematic;
            }
            else
            {
                s.t.position = s.pos;
                s.t.rotation = s.rot;
            }

            var d = s.t.GetComponent<Draggable>();
            if (d)
            {
                d.SetInvalid(false);
                d.SetHighlighted(false);
            }
        }

        Physics.SyncTransforms();

        // 2) 인벤토리 복구 (InventoryService 기반)
        RestoreDecoCountsFromSnapshot(invBaseline);
    }
    private List<(int id, int count)> GetAllDecoCountsFromService()
    {
        var result = new List<(int, int)>();

        if (decoDB?.decoList == null)
            return result;

        foreach (var d in decoDB.decoList)
        {
            if (d == null) continue;

            int count = 0;
            if (InventoryService.I != null)
                count = InventoryService.I.GetCount(d.deco_id);


            result.Add((d.deco_id, count));
        }

        return result;
    }

    private void RestoreDecoCountsFromSnapshot(List<InventorySnapshot> snap)
    {
        if (InventoryService.I == null || snap == null)
            return;

        foreach (var s in snap)
        {
            InventoryService.I.Set(s.id, s.count);
        }
    }
    #endregion


    #region ===== Spawn From Inventory =====

    // 공통 스폰 진입점
    public void SpawnFromPlaceable(IPlaceableData data, PlaceableCategory cat)
    {
        if (data == null) { Debug.LogWarning("[EditModeController] data null"); return; }

        var loader = new ResourcesLoader();
        var prefab = data.GetPrefab(loader);
        if (!prefab) { Debug.LogWarning($"[EditModeController] Prefab not found for {cat}:{data.Id}"); return; }

        GameObject go = Instantiate(prefab);
        go.name = data.DisplayName;

        // 공통 태그
        var tag = go.GetComponent<PlaceableTag>() ?? go.AddComponent<PlaceableTag>();
        tag.category = cat;
        tag.id = data.Id;

        // 드래그 가능
        var drag = go.GetComponent<Draggable>() ?? go.AddComponent<Draggable>();

        // 인벤 임시
        MarkAsInventoryTemp(go.transform, true);
        pendingFromInventory = go.transform;

        // 편집모드 진입 + 선택
        SetEditMode(true, keepTarget: false);
        SelectTarget(go.transform);

        // ✅ 스폰 위치: (0, 0, -10)
        go.transform.position = new Vector3(0f, 0f, -20f);
        go.transform.rotation = Quaternion.identity;

        // 그리드 스냅 옵션
        if (snapToGrid) go.transform.position = SnapToGrid(go.transform.position);

        //LSH 추가
        switch (cat)
        {
            case PlaceableCategory.Home:
                if (go.GetComponent<NavMeshModifier>() == null) go.AddComponent<NavMeshModifier>();
                var nMM = go.AddComponent<NavMeshModifier>();
                nMM.overrideArea = true;
                nMM.area = 1; 
                break;
            case PlaceableCategory.Animal:
                go.tag = "Animal";
                if(go.GetComponent<AnimalBehaviour>() == null) go.AddComponent<AnimalBehaviour>();
                break;
            case PlaceableCategory.Deco:
                if (go.GetComponent<NavMeshModifier>() == null) go.AddComponent<NavMeshModifier>();
                var nMM1 = go.AddComponent<NavMeshModifier>();
                nMM1.overrideArea = true;
                nMM1.area = 1; 
                go.tag = "Decoration";
                break;
        }

        // 최초 유효성 마킹
        bool valid = IsOverGround(go.transform.position) && !OverlapsOthers(go.transform);
        if (drag) { drag.SetInvalid(!valid); drag.SetHighlighted(true); }

        if (cat == PlaceableCategory.Animal)
            AnimalTakenFromInventory?.Invoke(data.Id);

        // 선택 직후 툴바 갱신
        UpdateToolbar();
    }

    // (기존) Deco 전용을 공통 스폰으로 라우팅
    public void SpawnFromDecoData(DecoData data)
    {
        SpawnFromPlaceable(new DecoPlaceable(data), PlaceableCategory.Deco);
    }

    #endregion

    #region ===== Firebase Sync (Lobby Layout) =====

    /// <summary>
    /// 편집모드에서 저장 버튼 눌렀을 때
    /// - 현재 씬 배치 → UserData.Local.lobby 로 반영
    /// - lobby.Save() 로 Firebase 업로드 (실제 구현은 나중에)
    /// </summary>
    private void TrySaveLobbyToFirebase()
    {
        if (PlaceableStore.I == null)
        {
            Debug.LogWarning("[EditModeController] PlaceableStore 없음 → Firebase 저장 스킵");
            return;
        }
        if (UserData.Local == null || UserData.Local.lobby == null)
        {
            Debug.LogWarning("[EditModeController] UserData.Local 또는 lobby 없음 → Firebase 저장 스킵");
            return;
        }

        // 1) 씬에서 Placed 리스트 뽑기
        var placed = PlaceableStore.I.CollectPlacedFromScene();

        // 2) Lobby.props에 반영
        UserData.Local.lobby.PlacedListToUserDataLobby(placed);

        // 3) Firebase 업로드
        UserData.Local.lobby.Save();   // ← 확장 메서드 (FirebaseManager.Instance.UpdateLocalUserDataCategory 호출)

        Debug.Log($"[EditModeController] Firebase Lobby 저장 시도. placed={placed.Count}");
    }


    #endregion

}
