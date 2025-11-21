using UnityEngine;
using UnityEngine.Audio;

public class AmbientGroup : BaseAudioGroup
{
    [Header("Pooling Settings")]
    [SerializeField] private int poolSize = 5;

    private AmbientPlayer player;
    private AudioPool audioPool;

    public override void Init()
    {
        base.Init();
        Debug.Log($"AmbientGroup Mixer : {mixer.name}");
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.Ambient);
        Debug.Log($"AmbientGroup MixerGroup : {group}");
        audioPool = new AudioPool(transform, group, poolSize);
        player = new AmbientPlayer(mixer, transform, audioPool);
    }

    public override void PostInit() { }
    // ����� ����
    public void PlayAmbient(AudioClip clip, bool loop, bool pooled, Vector3? pos = null)
    {
        player.PlayAudio(clip, group, loop, pooled, pos);
    }

    // ����� ����
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
