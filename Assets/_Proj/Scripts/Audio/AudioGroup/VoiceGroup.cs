using UnityEngine;
using UnityEngine.Audio;

public class VoiceGroup : MonoBehaviour, IAudioController
{
    private AudioMixer mixer;
    private AudioMixerGroup group;
    private VoicePlayer player;

    private void Awake()
    {
        mixer = AudioManager.AudioGroupProvider.GetMixer();
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.Voice);
        Debug.Log($"VoiceGroup.cs : {group}");
        player = new VoicePlayer(mixer, transform);
    }

    // 오디오 실행
    public void PlayVoice(AudioClip clip)
    {
        player.PlayAudio(clip, group);
    }

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
}
