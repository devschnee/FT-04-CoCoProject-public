using UnityEngine;
using UnityEngine.Audio;

public class OptionVolumeManager
{
    private readonly AudioMixer mixer;

    public OptionVolumeManager(AudioMixer mixer)
    {
        this.mixer = mixer;
    }

    public void SetVolume(string paramName, float linear)
    {
        float dB = (linear <= 0.0001f) ? -80f : Mathf.Log10(linear) * 20f;
        mixer.SetFloat(paramName, dB);
    }

    public float GetVolume(string paramName)
    {
        if (mixer.GetFloat(paramName, out float dB))
        {
            return Mathf.Pow(10, dB / 20f);
        }
        return 1f;
    }
}
