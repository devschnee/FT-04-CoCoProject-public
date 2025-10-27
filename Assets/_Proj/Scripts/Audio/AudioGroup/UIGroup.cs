using UnityEngine;
using UnityEngine.Audio;

public class UIGroup : MonoBehaviour, IAudioController
{
    private AudioMixer mixer;
    private AudioMixerGroup group;
    private UIPlayer player;

    private void Awake()
    {
        mixer = AudioManager.AudioGroupProvider.GetMixer();
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.UI);
        Debug.Log($"UIGroup.cs : {group}, SFX그룹이면 OK");
        player = new UIPlayer(mixer, transform);
    }

    // 오디오 실행
    public void PlayVoice(AudioClip clip)
    {
        player.PlayAudio(clip, group);
    }

    // UI 부분은 어떤 상태든 제어에서 자유로운 몸이니 굳이 필요없을 듯? 그래도 넣긴 합시다
    public void PlayPlayer()
    {
        //player.PlayAll();
    }

    public void PausePlayer()
    {
        //player.PauseAll();
    }

    public void ResumePlayer()
    {
        //player.ResumeAll();
    }

    public void StopPlayer()
    {
        //player.StopAll();
    }

    public void ResetPlayer()
    {
        //player.ResetAll();
    }
}
