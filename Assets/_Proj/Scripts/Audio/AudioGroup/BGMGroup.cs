using UnityEngine;

public class BGMGroup : BaseAudioGroup
{
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
    public override void Init()
    {
        base.Init();
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.BGM);
        Debug.Log($"BGMGroup : {group}");
        player = new BGMPlayer(mixer, transform, group);
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
        player.SetVolumeHalf();   
    }
    public override void SetVolumeNormal()
    {
        player.SetVolumeNormal();
    }

    public override void SetVolumeZero()
    {
        player.SetVolumeZero();
    }
}
