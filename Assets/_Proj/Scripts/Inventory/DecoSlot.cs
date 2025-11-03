using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 안의 슬롯 하나.
/// - 아이콘 / 수량 텍스트 보여주고
/// - 클릭하면 EditModeController 에게 "이거 배치해" 라고 요청
/// </summary>
public class DecoSlot : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button clickArea;

    [Header("Data")]
    [SerializeField] private int decoId;  // 이 슬롯이 나타내는 decoId

    private void OnEnable()
    {
        // 클릭 연결
        if (clickArea)
        {
            clickArea.onClick.RemoveAllListeners();
            clickArea.onClick.AddListener(OnClick);
        }

        RefreshNow();

        // 인벤 변경 감지
        if (DecoInventoryRuntime.I != null)
            DecoInventoryRuntime.I.OnChanged += OnInvChanged;
    }

    private void OnDisable()
    {
        if (DecoInventoryRuntime.I != null)
            DecoInventoryRuntime.I.OnChanged -= OnInvChanged;
    }

    private void OnInvChanged(int changedId, int newCount)
    {
        if (changedId == decoId)
            RefreshNow();
    }

    /// <summary>슬롯이 어떤 decoId 를 표시할지 설정</summary>
    public void SetDecoId(int id)
    {
        decoId = id;
        RefreshNow();
    }

    /// <summary>UI 갱신</summary>
    public void RefreshNow()
    {
        if (!DecoInventoryRuntime.I) return;
        var db = DecoInventoryRuntime.I.DB;
        if (!db) return;

        var data = db.decoList.Find(d => d.deco_id == decoId);
        if (data == null) return;

        // 아이콘
        if (icon)
            icon.sprite = DataManager.Instance.Deco.GetIcon(data.deco_id);

        // 수량
        int c = DecoInventoryRuntime.I.Count(decoId);
        if (countText)
        {
            if (c > 1) countText.text = $"x{c}";
            else if (c == 1) countText.text = "";     // 1개일 때는 숫자 안 보여줌
            else countText.text = "0";    // 0일 때는 0
        }
    }

    /// <summary>
    /// 슬롯 클릭 → 수량 1 소비 → 편집모드 컨트롤러에 스폰 요청
    /// </summary>
    private void OnClick()
    {
        // 인벤/DB 체크
        if (!DecoInventoryRuntime.I) return;
        var db = DecoInventoryRuntime.I.DB;
        if (!db) return;

        var data = db.decoList.Find(d => d.deco_id == decoId);
        if (data == null) return;

        // 1) 인벤에서 1개 빼기
        if (!DecoInventoryRuntime.I.TryConsume(decoId, 1))
        {
            // 수량이 없으면 끝
            return;
        }

        // 2) 편집모드 컨트롤러 찾기
        var edit = FindFirstObjectByType<EditModeController>();
        if (!edit)
        {
            // 컨트롤러가 없으면 다시 되돌려줘야 함
            DecoInventoryRuntime.I.Add(decoId, 1);
            Debug.LogWarning("[DecoSlot] EditModeController를 찾지 못했습니다.");
            return;
        }

        // 3) 실제로 씬에 프리팹 배치
        edit.SpawnFromDecoData(data);
    }
}
