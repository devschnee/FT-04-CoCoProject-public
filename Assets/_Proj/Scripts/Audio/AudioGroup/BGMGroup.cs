using UnityEngine;
using UnityEngine.Audio;

public class BGMGroup : MonoBehaviour, IAudioGroup
{
    private AudioMixer mixer;
    private AudioMixerGroup group;
    private BGMPlayer player;
    private AudioSource audioS;

    private void Awake()
    {
        mixer = AudioManager.AudioGroupProvider.GetMixer();
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.BGM);
        Debug.Log($"BGMGroup : {group}");
        player = new BGMPlayer(mixer, transform);
    }

    // 오디오 실행
    public void PlayBGM(AudioClip clip, float fadeIn, float fadeOut, bool loop)
    {
        player.PlayAudio(clip, group, fadeIn, fadeOut, loop);
    }

    // 오디오 상태 제어
    public void Play()
    {
        audioS = GetComponentInChildren<AudioSource>();
        audioS.Play();
    }

    public void Pause()
    {
        audioS = GetComponentInChildren<AudioSource>();
        audioS.Pause();
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
