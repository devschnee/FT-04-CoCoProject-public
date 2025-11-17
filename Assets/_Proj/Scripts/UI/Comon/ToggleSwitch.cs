using System;
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

    [Header("Target Position")]
    public float handleMoveX = 60f;

    [Header("Settings")]
    public float animSpeed = 0.15f;
    public bool isOn = false;

    private Vector2 defaultPos;


    void Start()
    {
        defaultPos = handle.anchoredPosition;
        UpdateVisual(true);
    }

    public void Toggle()
    {
        isOn = !isOn;
        UpdateVisual();
    }

    private void UpdateVisual(bool instant = false)
    {
        float targetX = isOn ? handleMoveX : 0f;

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

            labelOn.color = isOn ? onColour : new Color(0.2f, 0.2f, 0.2f);
            labelOff.color = isOn ? new Color(0.2f, 0.2f, 0.2f) : offColour;
        }
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
