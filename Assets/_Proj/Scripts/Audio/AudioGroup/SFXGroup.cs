using UnityEngine;
using UnityEngine.Audio;

public class SFXGroup : BaseAudioGroup
{
    [Header("Pooling Settings")]
    [SerializeField] private int poolSize = 10;

    private SFXPlayer player;
    private AudioPool audioPool;

    public void PlaySFX(AudioClip clip, bool loop, bool pooled, Vector3? pos = null)
    {
        player.PlayAudio(clip, group, loop, pooled, pos);
    }

    // IAudioController 영역
    public override void Init()
    {
        base.Init();
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.SFX);
        Debug.Log($"SFXGroup : {group}");
        audioPool = new AudioPool(transform, group, poolSize);
        player = new SFXPlayer(mixer, transform, audioPool);
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
