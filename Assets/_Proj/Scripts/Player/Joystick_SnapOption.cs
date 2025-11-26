using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Unity.VisualScripting;

public class Joystick_SnapOption : MonoBehaviour
{

    [SerializeField] Joystick joystick;

    [SerializeField] Slider angleSlider;
    [SerializeField] TMP_InputField angleInputField;
    [SerializeField] Toggle enhanceFourDirToggle;

    IEnumerator Start()
    {
        while (!joystick)
        {
            yield return null;
            var joystickGo = GameObject.FindAnyObjectByType(typeof(Joystick));
            if (joystickGo)
            {
                joystick = joystickGo.GetComponent<Joystick>();
                joystick.onUiSetup += SetUI;
            }
        }
    }

    public void ApplyOptions()
    {
        joystick.ApplyOptions(angleSlider.value, enhanceFourDirToggle.isOn);
    }
    public void SliderToInputField()
    {
        angleInputField.text = angleSlider.value.ToString();
        ApplyOptions();
    }
    public void InputFieldToSlider()
    {
        int angle = Mathf.Clamp(int.Parse(angleInputField.text), 0, 45);
        angleInputField.text = angle.ToString();
        angleSlider.value = angle;
        ApplyOptions();
    }
    public void OnToggleChange(bool isOn)
    {
        ApplyOptions();
    }
    private void SetUI(float angleThreshold, bool enhanceFourDirToggle)
    {
        angleSlider.value = angleThreshold;
        angleInputField.text = angleThreshold.ToString();
    }
    void OnDestroy()
    {
        joystick.onUiSetup -= SetUI;
    }
}
