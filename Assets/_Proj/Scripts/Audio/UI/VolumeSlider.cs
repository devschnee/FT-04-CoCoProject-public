using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField] AudioType audioType;
    private AudioManager audioManager;

    private void Start()
    {
        audioManager = AudioManager.Instance;

        switch (audioType)
        {
            case AudioType.BGM:
                slider.value = audioManager.GetVolume(audioType);
                slider.onValueChanged.AddListener(v => audioManager.SetVolume(audioType, v));
                break;
            case AudioType.SFX:
                slider.value = audioManager.GetVolume(audioType);
                slider.onValueChanged.AddListener(v => audioManager.SetVolume(audioType, v));
                break;
            // case AudioType.Ambient:
            //     slider.value = audioManager.GetVolume(audioType);
            //     slider.onValueChanged.AddListener(v => audioManager.SetVolume(audioType, v));
            //     break;
            // case AudioType.Cutscene:
            //     slider.value = audioManager.GetVolume(audioType);
            //     slider.onValueChanged.AddListener(v => audioManager.SetVolume(audioType, v));
            //     break;
            // case AudioType.Voice:
            //     slider.value = audioManager.GetVolume(audioType);
            //     slider.onValueChanged.AddListener(v => audioManager.SetVolume(audioType, v));
            //     break;
            case AudioType.Master:
                slider.value = audioManager.GetVolume(audioType);
                slider.onValueChanged.AddListener(v => audioManager.SetVolume(audioType, v));
                break;
        }

    }
}
