using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DecoInventoryPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform content;   // 슬롯 컨테이너
    [SerializeField] private DecoSlot slotPrefab;     // 슬롯 프리팹

    // 간단 풀링: DB 순서대로 재사용
    private readonly List<DecoSlot> _pool = new(64);
    private bool _builtOnce;

    private void OnEnable()
    {
        // 탭 전환 시 비용 절약: 첫 빌드 이후에는 아이콘/텍스트만 갱신
        if (!_builtOnce) Rebuild();
        else RefreshAllVisible();
    }

    /// <summary>DB 순서대로 슬롯 구성(최초 1회 Instantiate, 이후 재사용)</summary>
    public void Rebuild()
    {
        if (!content || !slotPrefab) return;
        if (DecoInventoryRuntime.I == null || DecoInventoryRuntime.I.DB == null) return;

        var db = DecoInventoryRuntime.I.DB;
        EnsurePoolSize(db.decoList.Count);

        for (int i = 0; i < db.decoList.Count; i++)
        {
            var data = db.decoList[i];
            var slot = _pool[i];

            if (data == null)
            {
                slot.gameObject.SetActive(false);
                continue;
            }

            slot.gameObject.SetActive(true);
            slot.SetDecoId(data.deco_id);
        }

        // 남는 풀 비활성화
        for (int i = db.decoList.Count; i < _pool.Count; i++)
            _pool[i].gameObject.SetActive(false);

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        _builtOnce = true;
    }

    /// <summary>보이는 슬롯들만 수량/아이콘 빠르게 새로고침</summary>
    private void RefreshAllVisible()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            var s = _pool[i];
            if (s && s.gameObject.activeSelf)
                s.RefreshNow();
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private void EnsurePoolSize(int want)
    {
        // 늘려야 하면 생성
        while (_pool.Count < want)
        {
            var slot = Instantiate(slotPrefab, content);
            // RectTransform 기본값 보정
            if (slot.transform is RectTransform rt)
            {
                rt.localScale = Vector3.one;
                rt.anchoredPosition3D = Vector3.zero;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            _pool.Add(slot);
        }
    }
}
