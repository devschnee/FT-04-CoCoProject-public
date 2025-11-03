using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 편집모드 on/off, 저장/복원, 인벤에서 가져오기
/// (Core에서 이어지는 부분)
/// </summary>
public partial class EditModeController
{
    /// <summary>편집모드 on/off</summary>
    private void SetEditMode(bool on, bool keepTarget)
    {
        if (IsEditMode == on)
        {
            if (!on && !keepTarget)
                SelectTarget(null);
            return;
        }

        // 상단 버튼 보이기/숨기기
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
            CaptureBaseline();      // 오브젝트 + 인벤토리 상태 기억
            hasUnsavedChanges = false;
            UpdateUndoUI();
            UpdateToolbar();
        }
        else
        {
            // 편집 종료 시 선택 해제 + 하이라이트 제거 + 저장
            if (CurrentTarget && CurrentTarget.TryGetComponent<Draggable>(out var drag))
            {
                drag.SetInvalid(false);
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

    private void ToggleTopButtons(bool on)
    {
        if (undoButton) undoButton.gameObject.SetActive(on);
        if (saveButton) saveButton.gameObject.SetActive(on);
        if (backButton) backButton.gameObject.SetActive(on);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 뒤로가기/저장 버튼
    // ─────────────────────────────────────────────────────────────────────

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
        // 임시 오브젝트 정리
        CleanupInventoryTemps();

        if (restore)
        {
            RestoreBaseline();               // 오브젝트 + 인벤토리 복구
            SaveAllDraggablePositions();     // 복구된 위치로 다시 저장
            DecoInventoryRuntime.I?.SaveAll();
        }

        SetEditMode(false, keepTarget: false);

        hasUnsavedChanges = false;
        baseline.Clear();
        invBaseline.Clear();
        pendingFromInventory = null;
    }

    private void OnSaveClicked()
    {
        // 0) OK 안 된 임시물 제거/반환
        CleanupInventoryTemps();

        // 1) 씬의 Draggable 전부 저장
        SaveAllDraggablePositions();

        // 2) 씬에 배치 완료한 Deco 들 저장
        DecoPlacedStore.I?.SaveAllFromScene();

        // 3) 인벤 수량 저장
        DecoInventoryRuntime.I?.SaveAll();

        hasUnsavedChanges = false;
        CaptureBaseline(); // 저장 후 상태를 다시 baseline 으로
        if (savedInfoPanel) savedInfoPanel.SetActive(true);
    }

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
            var tag = tr.GetComponent<PlaceableTag_Deco>();
            if (tag != null) decoId = tag.decoId;

            Object.Destroy(tr.gameObject);

            if (decoId != 0 && DecoInventoryRuntime.I != null)
                DecoInventoryRuntime.I.Add(decoId, 1);
        }
    }

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

            d.SavePosition();
            count++;
        }
        Debug.Log($"[Save] Draggable (활성) {count}개 저장 완료");
    }

    // ─────────────────────────────────────────────────────────────────────
    // UI Wiring
    // ─────────────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────────────
    // Baseline (오브젝트 + 인벤토리)
    // ─────────────────────────────────────────────────────────────────────

    private static bool IsInLayerMask(int layer, LayerMask mask)
        => (mask.value & (1 << layer)) != 0;

    /// <summary>현재 씬 상태(오브젝트 위치 + 인벤 수량)를 baseline 으로 저장</summary>
    private void CaptureBaseline()
    {
        baseline.Clear();
        invBaseline.Clear();
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

        // 2) Draggable 은 아니지만 draggableMask 에 포함되는 콜라이더들
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

        // 3) 인벤토리 스냅샷
        if (DecoInventoryRuntime.I != null)
        {
            var all = DecoInventoryRuntime.I.GetAllCounts();
            foreach (var pair in all)
            {
                invBaseline.Add(new InventorySnapshot { id = pair.id, count = pair.count });
            }
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

        // 2) 인벤토리 복구
        if (DecoInventoryRuntime.I != null)
        {
            DecoInventoryRuntime.I.RestoreFromSnapshot(invBaseline);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // 인벤토리에서 꺼내서 바로 편집모드로
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>인벤토리에서 DecoData를 꺼내 씬에 배치 시작</summary>
    public void SpawnFromDecoData(DecoData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[EditModeController] DecoData가 null입니다.");
            return;
        }

        var prefab = DataManager.Instance.Deco.GetPrefab(data.deco_id);
        if (!prefab)
        {
            Debug.LogWarning($"[EditModeController] DecoData({data.deco_id})에 prefab이 없습니다. path={data.deco_prefab}");
            return;
        }

        GameObject go = Object.Instantiate(prefab);
        go.name = data.deco_name;

        // decoId 표시
        var tag = go.GetComponent<PlaceableTag_Deco>();
        if (!tag) tag = go.AddComponent<PlaceableTag_Deco>();
        tag.decoId = data.deco_id;

        // 드래그 가능하도록
        var drag = go.GetComponent<Draggable>();
        if (!drag) drag = go.AddComponent<Draggable>();

        // 인벤 임시 마킹
        MarkAsInventoryTemp(go.transform, true);
        pendingFromInventory = go.transform;

        // 편집모드로 강제 진입 + 선택
        SetEditMode(true, keepTarget: false);
        SelectTarget(go.transform);

        // 항상 (0,0,0)에서 시작
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;

        // 그리드 스냅
        if (snapToGrid)
            go.transform.position = SnapToGrid(go.transform.position);

        // 첫 상태 유효성 표현
        bool valid = IsOverGround(go.transform.position) && !OverlapsOthers(go.transform);
        if (drag)
        {
            drag.SetInvalid(!valid);
            drag.SetHighlighted(true);
        }
    }
}
