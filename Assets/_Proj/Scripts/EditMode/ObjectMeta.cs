using UnityEngine;

[DisallowMultipleComponent]
public class ObjectMeta : MonoBehaviour
{
    [Header("Meta")]
    public string displayName = "이름 없음";
    [TextArea] public string description = "설명이 없습니다.";
    public Sprite icon; // 선택사항

    // 편의: 패널에 나 자신을 표시
    public void ShowInfo(InfoPanel panel)
    {
        if (!panel) return;
        panel.Show(displayName, description, icon);
    }
}
