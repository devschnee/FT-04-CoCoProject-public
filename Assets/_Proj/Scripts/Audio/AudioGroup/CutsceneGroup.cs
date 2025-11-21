using UnityEngine;

// 영상으로 대체. 이제 오디오소스만 관리해야함

public class CutsceneGroup : BaseAudioGroup
{
    private CutscenePlayer player;

    public AudioSource GetCutsceneSource()
    {
        return player.GetCutsceneAS();
    }

    // IAudioController 영역
    public override void Init()
    {
        base.Init();
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.Cutscene);
        Debug.Log($"CutsceneGroup : {group}");
        player = new CutscenePlayer(mixer, transform, group);
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
        
    }
    public override void SetVolumeNormal()
    {
        
    }

    public override void SetVolumeZero()
    {
        
    }
}
