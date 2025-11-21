using UnityEngine;
using UnityEngine.Audio;

public class DialogueGroup : BaseAudioGroup
{
    private AudioMixerGroup bgm;
    private AudioMixerGroup sfx;
    private DialoguePlayer player;
    private AudioSource audioS;

    public void PlayDialogue(AudioType type, string audioFileName)
    {
        player.PlayDialogueAudio(type, audioFileName);
    }

    public override void Init()
    {
        base.Init();
        bgm = AudioManager.AudioGroupProvider.GetGroup(AudioType.DialogueBGM);
        sfx = AudioManager.AudioGroupProvider.GetGroup(AudioType.DialogueSFX);
        Debug.Log($"BGMGroup : {bgm}, SFXGroup : {sfx}");
        player = new DialoguePlayer(mixer, transform, bgm, sfx);
    }
    public override void PostInit() { }
    public override void PlayPlayer()
    {
        player.PlayAll();
    }
    public override void PausePlayer()
    {
        player.PauseAll();
    }
    public override void ResumePlayer()
    {
        player.ResumeAll();
    }
    public override void StopPlayer()
    {
        player.StopAll();
    }
    public override void ResetPlayer()
    {
        player.ResetAll();
    }
    public override void SetVolumeHalf()
    {
        // ?필요없음
    }
    public override void SetVolumeNormal()
    {
        // ?필요없음
    }

    public override void SetVolumeZero()
    {
        // ?필요없음
    }
}
