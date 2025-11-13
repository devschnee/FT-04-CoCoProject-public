// Game/UI/Inventory/Slot/UnifiedInvSlot.cs
using System;
using TMPro;               // ← 이름 표기용
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Inventory.Slot
{
    [DisallowMultipleComponent]
    public class UnifiedInvSlot : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;    // ★ 추가: 아이템/유닛 이름
        [SerializeField] private TMP_Text countText;   // (조형물만 사용)
        [SerializeField] private Button clickArea;

        private Action _onClick;

        private void Awake()
        {
            if (clickArea)
            {
                clickArea.onClick.RemoveAllListeners();
                clickArea.onClick.AddListener(() => _onClick?.Invoke());
            }
        }

        public void SetIcon(Sprite s)
        {
            if (icon) icon.sprite = s;
        }

        public void SetName(string nameOrNull)
        {
            if (!nameText) return;
            nameText.text = string.IsNullOrEmpty(nameOrNull) ? "" : nameOrNull;
        }

        /// <summary>
        /// textOrNull:
        /// - null  → 라벨 비표시
        /// - ""    → 라벨 표시하되 빈 문자열 (권장: 수량 1은 빈 문자열로)
        /// - "xN"  → 라벨 표시 (예: x3)
        /// </summary>
        public void SetCount(string textOrNull)
        {
            if (!countText) return;
            bool visible = textOrNull != null;
            countText.gameObject.SetActive(visible);
            if (visible) countText.text = textOrNull;
        }

        public void SetOnClick(Action onClick) => _onClick = onClick;
    }
}
