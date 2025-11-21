using UnityEngine;

public class UIGroup : BaseAudioGroup
{
    private UIPlayer player;

    public void PlayUI(AudioClip clip)
    {
        player.PlayAudio(clip);
    }

    // IAudioController 영역
    public override void Init()
    {
        mixer = AudioManager.AudioGroupProvider.GetMixer();
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.UI);
        Debug.Log($"UIGroup.cs : {group}, SFX 그룹이면 OK");
        player = new UIPlayer(mixer, transform, group);
    }
    public override void PostInit() { }
    // UI는 oneshot이니 제어 X
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
