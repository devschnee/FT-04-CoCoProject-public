using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DecoSlot : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button clickArea;

    [Header("Data")]
    [SerializeField] private int decoId;

    // 캐시
    private static EditModeController _edit;   // UnityEngine.Object 이므로 if (!_edit) 가능
    private static ResourcesLoader _loader;

    private void Awake()
    {
        // 🔧 null 체크 방식 수정
        if (_loader == null) _loader = new ResourcesLoader();

        if (clickArea)
        {
            clickArea.onClick.RemoveAllListeners();
            clickArea.onClick.AddListener(OnClick);
        }
    }

    private void OnEnable()
    {
        RefreshNow();
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
        if (changedId == decoId) RefreshNow();
    }

    public void SetDecoId(int id)
    {
        decoId = id;
        RefreshNow();
    }

    public void RefreshNow()
    {
        if (!DecoInventoryRuntime.I) return;
        var db = DecoInventoryRuntime.I.DB;
        if (!db) return;

        var data = db.decoList.Find(d => d != null && d.deco_id == decoId);
        if (data == null) return;

        // 🔧 2-인수 GetIcon 제거 → 1-인수 버전 사용
        if (icon) icon.sprite = DataManager.Instance.Deco.GetIcon(data.deco_id);

        int c = DecoInventoryRuntime.I.Count(decoId);
        if (countText)
        {
            if (c > 1) countText.text = $"x{c}";
            else if (c == 1) countText.text = "";
            else countText.text = "0";
        }
    }

    private void OnClick()
    {
        if (!DecoInventoryRuntime.I) return;
        var db = DecoInventoryRuntime.I.DB;
        if (!db) return;

        var data = db.decoList.Find(d => d != null && d.deco_id == decoId);
        if (data == null) return;

        if (!DecoInventoryRuntime.I.TryConsume(decoId, 1))
            return;

        if (!_edit) _edit = FindFirstObjectByType<EditModeController>();
        if (!_edit)
        {
            // 스폰 실패 롤백
            DecoInventoryRuntime.I.Add(decoId, 1);
            Debug.LogWarning("[DecoSlot] EditModeController를 찾지 못했습니다. 소비를 되돌립니다.");
            return;
        }

        _edit.SpawnFromDecoData(data);
    }
}
