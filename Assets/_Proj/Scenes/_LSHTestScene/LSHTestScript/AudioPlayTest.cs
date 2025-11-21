using UnityEngine;

public class AudioPlayTest : MonoBehaviour
{
    public void BGMPlay()
    {
        AudioEvents.Raise(BGMKey.Main, -1, 1f, 1f, true);
    }
    // public void CutsPlay()
    // {
    //     AudioEvents.Raise(CutsceneKey.CutsceneId01, -1, 1f, 1f, true);
    // }
    // public void SFXPlay()
    // {
    //     AudioEvents.Raise(SFXKey.CocodoogyFootstep, -1, loop : false, pooled : false, pos : transform.position);
    // }
    // public void SFXPooledPlay()
    // {
    //     AudioEvents.Raise(SFXKey.CocodoogyFootstep, -1, loop : false, pooled : true, pos : transform.position);
    // }
    public void AmbientPlay()
    {
        AudioEvents.Raise(AmbientKey.Birdsong, -1, loop: false, pooled: false, pos: transform.position);
    }
    public void AmbientPooledPlay()
    {
        AudioEvents.Raise(AmbientKey.Birdsong, -1, loop: false, pooled: true, pos: transform.position);
    }
    // public void VoicePlay()
    // {
    //     AudioEvents.Raise(VoiceKey.Cocodoogy, -1);
    // }
    // public void UIPlay()
    // {
    //     AudioEvents.Raise(UIKey.UIClick, -1);
    // }
    // public void UIPlay2()
    // {
    //     AudioEvents.Raise(UIKey.UIOpen, -1);
    // }
    public void Quit()
    {
        SettingManager.Instance.SaveSettings();
        Application.Quit();
    }
}
