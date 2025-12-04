using UnityEngine;
using Game.Inventory; // ★ 추가: InventoryService

public partial class EditModeController
{
    private bool infoPanelAutoOpened = false;
    private Coroutine savedInfoRoutine;
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

        // 1) PlaceableTag 우선 (집/동물/조형물)
        if (TryGetPlaceableTag(CurrentTarget, out var ptag))
        {
            ResolveTitleAndDesc(ptag, out var title, out var desc);
            panel.Show(title, desc);
            return;
        }

        // 2) 메인캐릭터(CocoDoogy / Master) 시도
        if (TryResolveMainCharacterInfo(CurrentTarget, out var mTitle, out var mDesc))
        {
            panel.Show(mTitle, mDesc);
            return;
        }

        // 3) 그 외에는 정보 없음
        panel.Show("정보 없음", "PlaceableTag가 없거나 메인캐릭터 데이터가 없습니다.");
    }


    private static void ResolveTitleAndDesc(PlaceableTag ptag, out string title, out string desc)
    {
        title = $"{ptag.category} {ptag.id}";
        desc = "설명이 없습니다.";

        switch (ptag.category)
        {
            case PlaceableCategory.Deco:
                {
                    var d = DataManager.Instance?.Deco?.GetData(ptag.id);
                    if (d != null)
                    {
                        title = string.IsNullOrEmpty(d.deco_name) ? $"Deco" : d.deco_name;
                        desc = string.IsNullOrEmpty(d.deco_desc) ? "" : d.deco_desc;
                    }
                    break;
                }

            case PlaceableCategory.Home:
                {
                    var hd = DataManager.Instance?.Home?.GetData(ptag.id);
                    if (hd != null)
                    {
                        title = string.IsNullOrEmpty(hd.home_name) ? $"Home" : hd.home_name;
                        desc = string.IsNullOrEmpty(hd.home_desc) ? "" : hd.home_desc;
                    }
                    break;
                }

            case PlaceableCategory.Animal:
                {
                    var ad = DataManager.Instance?.Animal?.GetData(ptag.id);
                    if (ad != null)
                    {
                        title = string.IsNullOrEmpty(ad.animal_name) ? $"Animal" : ad.animal_name;
                        desc = string.IsNullOrEmpty(ad.animal_desc) ? "" : ad.animal_desc;
                    }
                    break;
                }

            // 🔥 MainCharacter (NEW)
            case PlaceableCategory.MainCharacter:
                {
                    var mc = DataManager.Instance?.mainChar?.GetData(ptag.id);
                    if (mc != null)
                    {
                        // ❗ id 제거 → 오직 name + desc
                        title = string.IsNullOrEmpty(mc.mainChar_name) ? "캐릭터" : mc.mainChar_name;
                        desc = string.IsNullOrEmpty(mc.mainChar_desc) ? "" : mc.mainChar_desc;
                    }
                    break;
                }
        }
    }


    private bool TryResolveMainCharacterInfo(Transform t, out string title, out string desc)
    {
        title = null;
        desc = null;
        if (!t) return false;

        // Unity Tag 기준으로 타입 매핑
        MainCharacterType type;
        if (t.CompareTag("CocoDoogy"))
            type = MainCharacterType.CocoDoogy;
        else if (t.CompareTag("Master"))
            type = MainCharacterType.Master;
        else
            return false; // 메인캐릭터 아님

        // DB에서 찾아오기
        MainCharacterData data = null;

        // 1순위: 인스펙터에서 연결한 mainCharDB
        if (mainCharDB != null && mainCharDB.mainCharDataList != null)
        {
            data = mainCharDB.mainCharDataList.Find(d => d.mainChar_type == type);
        }

        // (선택) 2순위: DataManager에 메인캐릭터 DB가 있다면 여기도 시도
        // if (data == null && DataManager.Instance?.MainCharacter != null)
        // {
        //     data = DataManager.Instance.MainCharacter.GetDataByType(type);
        // }

        if (data == null) return false;

        // 🔥 InfoPanel에 보여줄 내용 구성
        string name = string.IsNullOrEmpty(data.mainChar_name)
            ? type.ToString()
            : data.mainChar_name;

        string descText = !string.IsNullOrEmpty(data.mainChar_desc)
            ? data.mainChar_desc
            : "설명이 없습니다.";

        title = name;
        desc = $"{descText}";

        return true;
    }
    private System.Collections.IEnumerator HideSavedPanelDelayed()
    {
        
        // 2초 대기
        // LSH 추가 1203 savedButton에 있던 로비캐릭터 초기화 작업을 여기서 이벤트로 묶음
        yield return new WaitForSeconds(1f);
        Debug.Log("로비변경되었습니다");
        OnChangedLobby?.Invoke();
        yield return new WaitForSeconds(1f);

        if (savedInfoPanel)
            savedInfoPanel.SetActive(false);

        // 기존 확인 버튼에서 하던 작업도 여기서 수행

        savedInfoRoutine = null;
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

        // 1) PlaceableTag 우선
        if (TryGetPlaceableTag(CurrentTarget, out var ptag))
        {
            ResolveTitleAndDesc(ptag, out var title, out var desc);
            panel.Toggle(title, desc);
            return;
        }

        // 2) 메인캐릭터 시도
        if (TryResolveMainCharacterInfo(CurrentTarget, out var mTitle, out var mDesc))
        {
            panel.Toggle(mTitle, mDesc);
            return;
        }

        // 3) 기타
        panel.Toggle("정보 없음", "PlaceableTag가 없거나 메인캐릭터 데이터가 없습니다.");
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

        // 🔥 집 + 인벤에서 꺼낸 임시 프리뷰일 때
        if (IsHome(CurrentTarget) && IsInventoryTempObject(CurrentTarget))
        {
            // 1) 바닥 체크 + 일반 겹침 체크 + Editable/InLobbyObject 겹침 체크
            bool valid =
                IsOverGround(CurrentTarget.position) &&
                !OverlapsOthers(CurrentTarget) &&
                !OverlapsHomeBlockers(CurrentTarget);   // ★ 새로 추가한 함수

            if (!valid)
            {
                SetHighlight(CurrentTarget, on: true, invalid: true);
                Debug.Log("[Home] 다른 오브젝트(Editable/InLobbyObject)와 겹쳐서 설치할 수 없습니다.");
                return;
            }

            // 2) 유효할 때만 설치 확정 플로우
            MarkAsInventoryTemp(CurrentTarget, false);
            homePreview = CurrentTarget;
            homePreviewConfirmed = true;

            SelectTarget(null);
            actionToolbar?.Hide();

            hasUnsavedChanges = true;
            pendingFromInventory = null;
            return;
        }

        // ★ 집이 아니거나, 임시물이 아닌 경우 기존 로직 유지
        HomePreviewActiveChanged?.Invoke(false);

        bool ok = IsOverGround(CurrentTarget.position) && !OverlapsOthers(CurrentTarget);
        if (!ok)
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

        // 항상 태그 기준 루트 Transform 사용
        Transform root = CurrentTarget;
        TryGetPlaceableTag(CurrentTarget, out var ptag);
        if (ptag != null)
            root = ptag.transform;

        // 집 프리뷰 취소
        if (IsHome(root) && IsInventoryTempObject(root))
        {
            var previewGo = root.gameObject;

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

        // Animal / Deco 인벤 반환
        if (ptag != null)
        {
            if (ptag.category == PlaceableCategory.Animal)
                AnimalReturnedToInventory?.Invoke(ptag.id);

            if (ptag.category == PlaceableCategory.Deco && InventoryService.I != null)
                InventoryService.I.Add(ptag.id, 1);
        }

        var go = root.gameObject;
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

        // 항상 PlaceableTag가 달린 루트 Transform 기준으로 처리
        Transform root = tag.transform;

        if (root == CurrentTarget)
            SelectTarget(null);

        actionToolbar?.Hide();

        switch (tag.category)
        {
            case PlaceableCategory.Deco:
                {
                    // 1) 인벤 수량은 바로 올려줌
                    if (InventoryService.I != null)
                        InventoryService.I.Add(tag.id, 1);

                    // 2) 이 오브젝트가 '인벤에서 막 꺼낸 임시 오브젝트'인지 확인
                    bool isTemp = IsInventoryTempObject(root);

                    if (isTemp)
                    {
                        // 인벤에서 꺼낸 임시 데코 → 통째로 삭제
                        Destroy(root.gameObject);
                    }
                    else
                    {
                        // 씬에 원래 있던 데코 → 비활성화만
                        root.gameObject.SetActive(false);
                    }

                    hasUnsavedChanges = true;
                    break;
                }

            case PlaceableCategory.Animal:
                {
                    AnimalReturnedToInventory?.Invoke(tag.id);

                    // LSH 추가 1127 : 위치 삭제 이벤트도 루트 기준으로
                    ETCEvent.InvokeDeleteAnimalPos(root);

                    // 🔥 루트 오브젝트를 삭제해야 화면에서 완전히 사라짐
                    Destroy(root.gameObject);

                    hasUnsavedChanges = true;
                    break;
                }

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
