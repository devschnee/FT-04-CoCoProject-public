using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToggleSwitch : MonoBehaviour
{
    [Header("UI")]
    public RectTransform handle;
    public Image bg;
    public TextMeshProUGUI labelOn;
    public TextMeshProUGUI labelOff;

    [Header("Colours")]
    public Color onColour = new Color(0.5f, 0.4f, 0.9f);
    public Color offColour = new Color(1f, 1f, 1f);
    private Color inactivateColor = new Color(0.4f, 0.38f, 0.35f);

    [Header("Target Position")]
    public float handleMoveX = 60f;

    [Header("Settings")]
    public float animSpeed = 0.15f;
    public bool isSkipOn = false;

    private Vector2 defaultPos;

    void Start()
    {
        defaultPos = handle.anchoredPosition;
        isSkipOn = UserData.Local.preferences.skipDialogues;
        //UpdateVisual(UserData.Local.preferences.skipDialogues);
        UpdateVisual(true); // 토글 상태 애니메이션 없이 즉시 반영
    }

    public void Toggle() // 토글 버튼 이벤트에 추가
    {
        //isSkipOn = !isSkipOn;
        //UserData.Local.preferences.skipDialogues = isSkipOn;
        UserData.Local.preferences.skipDialogues = !UserData.Local.preferences.skipDialogues;
        isSkipOn = UserData.Local.preferences.skipDialogues;
        UserData.Local.preferences.Save();
        UpdateVisual();
    }

    private void UpdateVisual(bool instant = false)
    {
        bool isOn = UserData.Local.preferences.skipDialogues;
        float targetX = UserData.Local.preferences.skipDialogues ? handleMoveX : 0f;

        if (instant)
        {
            handle.anchoredPosition = new Vector3(defaultPos.x + targetX, defaultPos.y); 
        }
        else
        {
            {
                StopAllCoroutines();
                StartCoroutine(AnimateHandle(targetX));
            }
        }
        labelOn.color = isSkipOn ? onColour : inactivateColor;
        labelOff.color = isSkipOn ? inactivateColor : offColour;
    }

    IEnumerator AnimateHandle(float targetX)
    {
        Vector2 start = handle.anchoredPosition;
        Vector2 end = new Vector2(defaultPos.x + targetX, defaultPos.y);

        float t = 0f;
        while(t < 1f)
        {
            t += Time.unscaledDeltaTime / animSpeed;
            handle.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }
        handle.anchoredPosition = end;
    }
}
