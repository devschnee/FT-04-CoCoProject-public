using UnityEngine;

/// <summary>
/// EditModeController - Toolbar / Selection 파트 (리팩토링)
/// - Home: 인벤 버튼 숨김
/// - Home 프리뷰: OK/Cancel 제공
/// - Animal/Deco: 확정 = Info/Rotate/인벤, 프리뷰 = Info/Rotate/OK/Cancel
/// </summary>
public partial class EditModeController
{
    private bool infoPanelAutoOpened = false;

    #region Selection
    // ─────────────────────────────────────────────────────────────
    // Selection
    // ─────────────────────────────────────────────────────────────
    public void SelectTarget(Transform t)
    {
        bool targetChanged = (CurrentTarget != t);

        // 이전 대상 하이라이트 해제
        SetHighlight(CurrentTarget, on: false, invalid: false);

        CurrentTarget = t;

        // 신규 대상 하이라이트
        SetHighlight(CurrentTarget, on: true, invalid: false);

        // InfoPanel 처리
        var panel = InfoPanel.FindInScene();
        if (IsEditMode && CurrentTarget)
        {
            if (!infoPanelAutoOpened && panel)
            {
                ShowInfoOfCurrentTarget(panel);
                infoPanelAutoOpened = true;
            }
            else if (targetChanged && panel && panel.IsVisible)
            {
                panel.Hide();
            }
        }
        else
        {
            if (panel && panel.IsVisible) panel.Hide();
        }

        UpdateToolbar();
        UpdateUndoUI();
    }

    private static void SetHighlight(Transform t, bool on, bool invalid)
    {
        if (!t) return;
        if (t.TryGetComponent<Draggable>(out var d))
        {
            d.SetInvalid(invalid);
            d.SetHighlighted(on);
        }
    }
    #endregion Selection

    #region Info Panel
    // ─────────────────────────────────────────────────────────────
    // Info Panel
    // ─────────────────────────────────────────────────────────────
    private void ShowInfoOfCurrentTarget(InfoPanel panel)
    {
        if (!CurrentTarget)
        {
            panel.Show("정보 없음", "선택된 오브젝트가 없습니다.");
            return;
        }

        if (!TryGetPlaceableTag(CurrentTarget, out var ptag))
        {
            panel.Show("정보 없음", "PlaceableTag가 없습니다.");
            return;
        }

        ResolveTitleAndDesc(ptag, out var title, out var desc);
        panel.Show(title, desc);
    }

    private static void ResolveTitleAndDesc(PlaceableTag ptag, out string title, out string desc)
    {
        title = $"{ptag.category} {ptag.id}";
        desc = "설명이 없습니다.";

        switch (ptag.category)
        {
            case PlaceableCategory.Deco:
                if (DecoInventoryRuntime.I?.DB)
                {
                    var d = DecoInventoryRuntime.I.DB.decoList.Find(x => x != null && x.deco_id == ptag.id);
                    if (d != null)
                    {
                        title = string.IsNullOrEmpty(d.deco_name) ? $"Deco {d.deco_id}" : d.deco_name;
                        if (!string.IsNullOrEmpty(d.deco_desc)) desc = d.deco_desc;
                    }
                }
                break;

            case PlaceableCategory.Home:
                {
                    var hd = DataManager.Instance?.Home?.GetData(ptag.id);
                    if (hd != null) title = hd.home_name;
                    break;
                }

            case PlaceableCategory.Animal:
                {
                    var ad = DataManager.Instance?.Animal?.GetData(ptag.id);
                    if (ad != null) title = ad.animal_name;
                    break;
                }
        }
    }
    #endregion Info Panel

    #region Toolbar (layout & dispatch)
    // ─────────────────────────────────────────────────────────────
    // Toolbar
    // ─────────────────────────────────────────────────────────────
    private void UpdateToolbar()
    {
        if (!actionToolbar) return;

        if (!IsEditMode || !CurrentTarget)
        {
            actionToolbar.Hide();
            return;
        }

        bool isTemp = IsInventoryTempObject(CurrentTarget);

        // 태그 없으면 안전 기본(Info/Rotate만)
        if (!TryGetPlaceableTag(CurrentTarget, out var ptag))
        {
            ShowToolbar(CurrentTarget, onInfo: OnToolbarInfo, onRotate: OnToolbarRotate);
            return;
        }

        switch (ptag.category)
        {
            case PlaceableCategory.Home:
                if (isTemp)
                {
                    // 인벤에서 꺼낸 집(프리뷰): Info / Rotate / OK / Cancel
                    ShowToolbar(CurrentTarget, OnToolbarInfo, OnToolbarRotate, onInventory: null, onOk: OnToolbarOk, onCancel: OnToolbarCancel);
                }
                else
                {
                    // 확정된 집: Info / Rotate
                    ShowToolbar(CurrentTarget, OnToolbarInfo, OnToolbarRotate);
                }
                break;

            case PlaceableCategory.Animal:
            case PlaceableCategory.Deco:
                if (isTemp)
                {
                    // 프리뷰: Info / Rotate / OK / Cancel
                    ShowToolbar(CurrentTarget, OnToolbarInfo, OnToolbarRotate, onInventory: null, onOk: OnToolbarOk, onCancel: OnToolbarCancel);
                }
                else
                {
                    // 확정: Info / Rotate / 인벤
                    ShowToolbar(CurrentTarget, OnToolbarInfo, OnToolbarRotate, onInventory: () => ReturnToInventory(CurrentTarget));
                }
                break;
        }
    }

    private void ShowToolbar(Transform target,
                             System.Action onInfo,
                             System.Action onRotate,
                             System.Action onInventory = null,
                             System.Action onOk = null,
                             System.Action onCancel = null)
    {
        actionToolbar.Show(
            target: target,
            worldCamera: cam,
            onInfo: onInfo,
            onRotate: onRotate,
            onInventory: onInventory,
            onOk: onOk,
            onCancel: onCancel
        );
    }

    private bool TryGetPlaceableTag(Transform t, out PlaceableTag tag)
    {
        tag = null;
        if (!t) return false;
        tag = t.GetComponentInParent<PlaceableTag>() ?? t.GetComponentInChildren<PlaceableTag>();
        return tag != null;
    }
    #endregion Toolbar (layout & dispatch)

    #region Toolbar Actions
    // ─────────────────────────────────────────────────────────────
    // Toolbar Actions
    // ─────────────────────────────────────────────────────────────
    private void OnToolbarInfo()
    {
        if (!CurrentTarget) return;

        var panel = InfoPanel.FindInScene();
        if (!panel)
        {
            Debug.LogWarning("[EditModeController] InfoPanel을 씬에서 찾지 못했습니다.");
            return;
        }

        if (!TryGetPlaceableTag(CurrentTarget, out var ptag))
        {
            panel.Toggle("정보 없음", "PlaceableTag가 없습니다.");
            return;
        }

        ResolveTitleAndDesc(ptag, out var title, out var desc);
        panel.Toggle(title, desc);
    }

    private void OnToolbarRotate()
    {
        if (!CurrentTarget) return;

        // (원한다면 Home 회전 금지)
        // if (IsHome(CurrentTarget)) return;

        var original = new Snap { pos = CurrentTarget.position, rot = CurrentTarget.rotation };
        CurrentTarget.Rotate(0f, 90f, 0f, Space.World);

        if (OverlapsOthers(CurrentTarget))
        {
            // 되돌림
            CurrentTarget.position = original.pos;
            CurrentTarget.rotation = original.rot;
            SetHighlight(CurrentTarget, on: true, invalid: true);
            Debug.Log("[Rotate] 겹쳐서 회전을 취소했습니다.");
            return;
        }

        // 히스토리 저장
        var stack = GetOrCreateHistory(CurrentTarget);
        stack.Push(original);
        TrimHistoryIfNeeded(stack);
        UpdateUndoUI();

        SetHighlight(CurrentTarget, on: true, invalid: false);
        hasUnsavedChanges = true;
    }

    private void OnToolbarOk()
    {
        if (!CurrentTarget) return;

        // Home 프리뷰 확정
        if (IsHome(CurrentTarget) && IsInventoryTempObject(CurrentTarget))
        {
            if (homePrev) Destroy(homePrev.gameObject);

            MarkAsInventoryTemp(CurrentTarget, false);
            homePreview = null;
            homePrev = CurrentTarget;

            if (homePrev.TryGetComponent<PlaceableTag>(out var t)) homePrevId = t.id;

            // 선택 해제 + 툴바 숨김
            SelectTarget(null);
            actionToolbar?.Hide();

            hasUnsavedChanges = true;
            pendingFromInventory = null;
            return;
        }

        // Animal/Deco 프리뷰 확정
        bool valid = IsOverGround(CurrentTarget.position) && !OverlapsOthers(CurrentTarget);
        if (!valid)
        {
            SetHighlight(CurrentTarget, on: true, invalid: true);
            Debug.Log("[EditModeController] 겹치거나 바닥이 아니어서 확정할 수 없습니다.");
            return;
        }

        MarkAsInventoryTemp(CurrentTarget, false);

        SelectTarget(null);
        actionToolbar?.Hide();

        hasUnsavedChanges = true;
        pendingFromInventory = null;
    }

    private void OnToolbarCancel()
    {
        if (!CurrentTarget) return;

        // Home 프리뷰 취소 → 기존 집 복구
        if (IsHome(CurrentTarget) && IsInventoryTempObject(CurrentTarget))
        {
            var previewGo = CurrentTarget.gameObject;

            SelectTarget(null);
            actionToolbar?.Hide();
            Destroy(previewGo);

            if (homePrev)
            {
                homePrev.gameObject.SetActive(true);
                SelectTarget(homePrev);
                SetLongPressTarget(homePrev);
            }

            homePreview = null;
            hasUnsavedChanges = false;
            pendingFromInventory = null;
            return;
        }

        // Animal/Deco 프리뷰 취소
        if (TryGetPlaceableTag(CurrentTarget, out var ptag))
        {
            // Animal: 슬롯 복귀 이벤트
            if (ptag.category == PlaceableCategory.Animal)
                AnimalReturnedToInventory?.Invoke(ptag.id);

            // Deco: 인벤 수량 원복
            if (ptag.category == PlaceableCategory.Deco && DecoInventoryRuntime.I != null)
                DecoInventoryRuntime.I.Add(ptag.id, 1);
        }

        var go = CurrentTarget.gameObject;
        SelectTarget(null);
        actionToolbar?.Hide();
        Destroy(go);

        hasUnsavedChanges = true;
        pendingFromInventory = null;
    }
    #endregion Toolbar Actions

    #region Return To Inventory
    private void ReturnToInventory(Transform t)
    {
        if (!t || !TryGetPlaceableTag(t, out var tag)) return;

        // 선택 해제 & 툴바 숨김
        if (t == CurrentTarget) SelectTarget(null);
        actionToolbar?.Hide();

        switch (tag.category)
        {
            case PlaceableCategory.Deco:
                if (DecoInventoryRuntime.I != null) DecoInventoryRuntime.I.Add(tag.id, 1);
                Destroy(t.gameObject);
                hasUnsavedChanges = true;
                break;

            case PlaceableCategory.Animal:
                AnimalReturnedToInventory?.Invoke(tag.id);
                Destroy(t.gameObject);
                hasUnsavedChanges = true;
                break;

            case PlaceableCategory.Home:
                // Home은 인벤 버튼 없음
                Debug.Log("[ReturnToInventory] Home은 인벤 버튼이 없습니다.");
                break;
        }
    }
    #endregion Return To Inventory

    #region InventoryTemp Marker
    // ─────────────────────────────────────────────────────────────
    // InventoryTemp Marker
    // ─────────────────────────────────────────────────────────────
    private bool IsInventoryTempObject(Transform tr)
    {
        if (!tr) return false;
        return tr.gameObject.GetComponent<InventoryTempMarker>() != null;
    }

    private void MarkAsInventoryTemp(Transform tr, bool on)
    {
        if (!tr) return;
        var m = tr.GetComponent<InventoryTempMarker>();
        if (on)
        {
            if (!m) tr.gameObject.AddComponent<InventoryTempMarker>();
        }
        else
        {
            if (m) Destroy(m);
        }
    }
    #endregion InventoryTemp Marker
}
