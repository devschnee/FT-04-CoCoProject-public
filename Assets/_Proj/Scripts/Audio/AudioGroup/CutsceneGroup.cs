using UnityEngine;
using UnityEngine.Audio;

public class CutsceneGroup : MonoBehaviour, IAudioController
{
    private AudioMixer mixer;
    private AudioMixerGroup group;
    private CutscenePlayer player;

    public AudioSource GetCutsceneSource()
    {
        return player.GetCutsceneAS();
    }
    public void Init()
    {
        mixer = AudioManager.AudioGroupProvider.GetMixer();
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.Cutscene);
        Debug.Log($"CutsceneGroup : {group}");
        player = new CutscenePlayer(mixer, transform, group);
    }

    public void PostInit() { }

    // ����� ����
    public void PlayCutscene(AudioClip clip, float fadeIn, float fadeOut, bool loop)
    {
        player.PlayAudio(clip, group, fadeIn, fadeOut, loop);
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
