using UnityEngine;

public class AudioPlayTest : MonoBehaviour
{
    public void BGMPlay()
    {
        AudioManager.Instance.PlayBGM<BGMKey>(AudioType.BGM, BGMKey.Main, -1, 1f, 1f, true);
    }
    public void CutsPlay()
    {
        AudioManager.Instance.PlayBGM<CutsceneKey>(AudioType.Cutscene, CutsceneKey.CutsceneId01, -1, 1f, 1f, true);
    }
    public void SFXPlay()
    {
        AudioManager.Instance.PlaySFX<SFXKey>(AudioType.SFX, SFXKey.UIClick, -1);
    }
    public void SFXPooledPlay()
    {
        AudioManager.Instance.PlaySFX<SFXKey>(AudioType.SFX, SFXKey.UIClick, -1, false, true);
    }
    public void AmbientPlay()
    {
        AudioManager.Instance.PlaySFX<AmbientKey>(AudioType.Ambient, AmbientKey.Birdsong, -1);
    }
    public void AmbientPooledPlay()
    {
        AudioManager.Instance.PlaySFX<AmbientKey>(AudioType.Ambient, AmbientKey.Birdsong, -1, false, true);
    }
    public void VoicePlay()
    {
        AudioManager.Instance.PlaySFX<VoiceKey>(AudioType.Voice, VoiceKey.Cocodoogy, -1);
    }
    public void VoicePooledPlay()
    {
        AudioManager.Instance.PlaySFX<VoiceKey>(AudioType.Voice, VoiceKey.Cocodoogy, -1, false, true);
    }
    
}
