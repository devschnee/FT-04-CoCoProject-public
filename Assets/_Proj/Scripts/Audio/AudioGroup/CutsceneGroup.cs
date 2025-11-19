using UnityEngine;
using UnityEngine.Audio;

// 영상으로 대체. 이제 오디오소스만 관리해야함

public class CutsceneGroup : MonoBehaviour, IAudioController
{
    private AudioMixer mixer;
    private AudioMixerGroup group;
    private CutscenePlayer player;

    public AudioSource GetCutsceneSource()
    {
        return player.GetCutsceneAS();
    }

    // IAudioController 영역
    public void Init()
    {
        mixer = AudioManager.AudioGroupProvider.GetMixer();
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.Cutscene);
        Debug.Log($"CutsceneGroup : {group}");
        player = new CutscenePlayer(mixer, transform, group);
    }
    public void PostInit() { }
    public void PlayPlayer()
    {
        player.PlayAll();
    }
    public void PausePlayer()
    {
        player.PauseAll();
    }
    public void ResumePlayer()
    {
        player.ResumeAll();
    }
    public void StopPlayer()
    {
        player.StopAll();
    }
    public void ResetPlayer()
    {
        player.ResetAll();
    }
    public void SetVolumeHalf()
    {
        
    }
    public void SetVolumeNormal()
    {
        
    }
}
