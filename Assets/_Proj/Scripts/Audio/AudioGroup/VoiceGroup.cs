using UnityEngine;
using UnityEngine.Audio;

public class VoiceGroup : MonoBehaviour, IAudioGroup
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

    public void Play()
    {
        throw new System.NotImplementedException();
    }

    public void Pause()
    {
        throw new System.NotImplementedException();
    }

    public void Resume()
    {
        throw new System.NotImplementedException();
    }

    public void StopAll()
    {
        throw new System.NotImplementedException();
    }

    public void ResetGroup()
    {
        throw new System.NotImplementedException();
    }
}
