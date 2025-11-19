using UnityEngine;
using UnityEngine.Audio;

public class UIGroup : MonoBehaviour, IAudioController
{
    private AudioMixer mixer;
    private AudioMixerGroup group;
    private UIPlayer player;

    public void PlayVoice(AudioClip clip)
    {
        player.PlayAudio(clip);
    }

    // IAudioController 영역
    public void Init()
    {
        mixer = AudioManager.AudioGroupProvider.GetMixer();
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.UI);
        Debug.Log($"UIGroup.cs : {group}, SFX 그룹이면 OK");
        player = new UIPlayer(mixer, transform, group);
    }
    public void PostInit() { }
    // UI는 oneshot이니 제어 X
    public void PlayPlayer()
    {
        //player.PlayAll();
    }
    public void PausePlayer()
    {
        //player.PauseAll();
    }
    public void ResumePlayer()
    {
        //player.ResumeAll();
    }
    public void StopPlayer()
    {
        //player.StopAll();
    }
    public void ResetPlayer()
    {
        //player.ResetAll();
    }
    public void SetVolumeHalf()
    {
        
    }
    public void SetVolumeNormal()
    {
        
    }
}
