using UnityEngine;
using UnityEngine.Audio;

public class BGMGroup : MonoBehaviour, IAudioController
{
    private AudioMixer mixer;
    private AudioMixerGroup group;
    private BGMPlayer player;
    private AudioSource audioS;

    public void PlayBGM(AudioClip clip, float fadeIn, float fadeOut, bool loop)
    {
        player.PlayAudio(clip, fadeIn, fadeOut, loop);
    }

    public void PlayBGMForResources(AudioClip clip, float fadeIn, float fadeOut, bool loop)
    {
        player.PlayBGMForResources(clip, fadeIn, fadeOut, loop);
    }

    // IAudioController 영역
    public void Init()
    {
        mixer = AudioManager.AudioGroupProvider.GetMixer();
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.BGM);
        Debug.Log($"BGMGroup : {group}");
        player = new BGMPlayer(mixer, transform, group);
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
        player.SetVolumeHalf();   
    }
    public void SetVolumeNormal()
    {
        player.SetVolumeNormal();
    }
}
