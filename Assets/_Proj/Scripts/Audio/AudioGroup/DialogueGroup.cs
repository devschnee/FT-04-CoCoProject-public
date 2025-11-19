using UnityEngine;
using UnityEngine.Audio;

public class DialogueGroup : MonoBehaviour, IAudioController
{
    private AudioMixer mixer;
    private AudioMixerGroup bgm;
    private AudioMixerGroup sfx;
    private DialoguePlayer player;
    private AudioSource audioS;

    public void PlayDialogue(AudioType type, string audioFileName)
    {
        player.PlayDialogueAudio(type, audioFileName);
    }

    public void Init()
    {
        mixer = AudioManager.AudioGroupProvider.GetMixer();
        bgm = AudioManager.AudioGroupProvider.GetGroup(AudioType.DialogueBGM);
        sfx = AudioManager.AudioGroupProvider.GetGroup(AudioType.DialogueSFX);
        Debug.Log($"BGMGroup : {bgm}, SFXGroup : {sfx}");
        player = new DialoguePlayer(mixer, transform, bgm, sfx);
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
        // ?필요없음
    }

    public void SetVolumeNormal()
    {
        // ?필요없음
    }
}
