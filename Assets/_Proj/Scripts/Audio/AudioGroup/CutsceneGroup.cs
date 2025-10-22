using UnityEngine;
using UnityEngine.Audio;

public class CutsceneGroup : MonoBehaviour, IAudioGroup
{
    private AudioMixer mixer;
    private AudioMixerGroup group;
    private CutscenePlayer player;

    private void Awake()
    {
        mixer = AudioManager.AudioGroupProvider.GetMixer();
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.Cutscene);
        Debug.Log($"CutsceneGroup : {group}");
        player = new CutscenePlayer(mixer, transform);
    }

    // 오디오 실행
    public void PlayCutscene(AudioClip clip, float fadeIn, float fadeOut, bool loop)
    {
        player.PlayAudio(clip, group, fadeIn, fadeOut, loop);
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
