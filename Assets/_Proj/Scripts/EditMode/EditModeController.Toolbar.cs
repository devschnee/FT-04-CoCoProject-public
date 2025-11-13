using UnityEngine;
using Game.Inventory; // ★ 추가: InventoryService

public partial class EditModeController
{
    private bool infoPanelAutoOpened = false;

    #region Selection
    private Camera camCache;
    private Camera WorldCam
    {
        get
        {
            if (!camCache) camCache = Camera.main;
            return camCache;
        }
    }
    public void SelectTarget(Transform t)
    {
        bool targetChanged = (CurrentTarget != t);
        SetHighlight(CurrentTarget, on: false, invalid: false);
        CurrentTarget = t;
        SetHighlight(CurrentTarget, on: true, invalid: false);

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
    #endregion

    #region Info Panel
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
                {
                    // ★ 변경: DataManager(혹은 decoDB)에서 정보 조회
                    var d = DataManager.Instance?.Deco?.GetData(ptag.id);
                    if (d != null)
                    {
                        title = string.IsNullOrEmpty(d.deco_name) ? $"Deco {d.deco_id}" : d.deco_name;
                        if (!string.IsNullOrEmpty(d.deco_desc)) desc = d.deco_desc;
                    }
                    break;
                }
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
    #endregion

    #region Toolbar (layout & dispatch)
    private void UpdateToolbar()
    {
        if (!actionToolbar) return;

        if (!IsEditMode || !CurrentTarget)
        {
            actionToolbar.Hide();
            return;
        }

        bool isTemp = IsInventoryTempObject(CurrentTarget);

        if (!TryGetPlaceableTag(CurrentTarget, out var ptag))
        {
            ShowToolbar(CurrentTarget, onInfo: OnToolbarInfo, onRotate: OnToolbarRotate);
            return;
        }

        switch (ptag.category)
        {
            case PlaceableCategory.Home:
                if (isTemp)
                    ShowToolbar(CurrentTarget, OnToolbarInfo, OnToolbarRotate, onInventory: null, onOk: OnToolbarOk, onCancel: OnToolbarCancel);
                else
                    ShowToolbar(CurrentTarget, OnToolbarInfo, OnToolbarRotate);
                break;

            case PlaceableCategory.Animal:
            case PlaceableCategory.Deco:
                if (isTemp)
                    ShowToolbar(CurrentTarget, OnToolbarInfo, OnToolbarRotate, onInventory: null, onOk: OnToolbarOk, onCancel: OnToolbarCancel);
                else
                    ShowToolbar(CurrentTarget, OnToolbarInfo, OnToolbarRotate, onInventory: () => ReturnToInventory(CurrentTarget));
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
            worldCamera: WorldCam,
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
    #endregion

    #region Toolbar Actions
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

        var original = new Snap { pos = CurrentTarget.position, rot = CurrentTarget.rotation };
        CurrentTarget.Rotate(0f, 90f, 0f, Space.World);

        if (OverlapsOthers(CurrentTarget))
        {
            CurrentTarget.position = original.pos;
            CurrentTarget.rotation = original.rot;
            SetHighlight(CurrentTarget, on: true, invalid: true);
            Debug.Log("[Rotate] 겹쳐서 회전을 취소했습니다.");
            return;
        }

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

        if (IsHome(CurrentTarget) && IsInventoryTempObject(CurrentTarget))
        {
            MarkAsInventoryTemp(CurrentTarget, false);
            homePreview = CurrentTarget;
            homePreviewConfirmed = true;

            SelectTarget(null);
            actionToolbar?.Hide();

            hasUnsavedChanges = true;
            pendingFromInventory = null;
            return;
        }
        HomePreviewActiveChanged?.Invoke(false);

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
            HomePreviewActiveChanged?.Invoke(false);

            homePreview = null;
            hasUnsavedChanges = false;
            pendingFromInventory = null;
            return;
        }

        if (TryGetPlaceableTag(CurrentTarget, out var ptag))
        {
            if (ptag.category == PlaceableCategory.Animal)
                AnimalReturnedToInventory?.Invoke(ptag.id);

            if (ptag.category == PlaceableCategory.Deco && InventoryService.I != null)
                InventoryService.I.Add(PlaceableCategory.Deco, ptag.id, 1); // ★ 변경
        }

        var go = CurrentTarget.gameObject;
        SelectTarget(null);
        actionToolbar?.Hide();
        Destroy(go);

        hasUnsavedChanges = true;
        pendingFromInventory = null;
    }
    #endregion

    #region Return To Inventory
    private void ReturnToInventory(Transform t)
    {
        if (!t || !TryGetPlaceableTag(t, out var tag)) return;

        if (t == CurrentTarget) SelectTarget(null);
        actionToolbar?.Hide();

        switch (tag.category)
        {
            case PlaceableCategory.Deco:
                if (InventoryService.I != null) // ★ 변경
                    InventoryService.I.Add(PlaceableCategory.Deco, tag.id, 1);
                Destroy(t.gameObject);
                hasUnsavedChanges = true;
                break;

            case PlaceableCategory.Animal:
                AnimalReturnedToInventory?.Invoke(tag.id);
                Destroy(t.gameObject);
                hasUnsavedChanges = true;
                break;

            case PlaceableCategory.Home:
                Debug.Log("[ReturnToInventory] Home은 인벤 버튼이 없습니다.");
                break;
        }
    }
    #endregion

    #region InventoryTemp Marker
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
    #endregion
}
