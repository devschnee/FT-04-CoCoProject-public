using UnityEngine;

public class AudioGroupController
{
    private readonly BaseAudioGroup[] audioGroups;

    public AudioGroupController(BaseAudioGroup[] audioGroups)
    {
        this.audioGroups = audioGroups;
    }

    public void PlayAllAudioGroup()
    {
        foreach (IAudioController aG in audioGroups)
        {
            aG.PlayPlayer();
        }
    }
    public void PauseAllAudioGroup()
    {
        foreach (IAudioController aG in audioGroups)
        {
            aG.PausePlayer();
        }
    }
    public void ResumeAllAudioGroup()
    {
        foreach (IAudioController aG in audioGroups)
        {
            aG.ResumePlayer();
        }
    }
    public void StopAllAudioGroup()
    {
        foreach (IAudioController aG in audioGroups)
        {
            aG.StopPlayer();
        }
    }
    public void ResetAllAudioGroup()
    {
        foreach (IAudioController aG in audioGroups)
        {
            aG.ResetPlayer();
        }
    }
    /// <summary>
    /// true = 볼륨 0, false = 볼륨 1
    /// </summary>
    /// <param name="isEntering"></param>
    public void SetVolumeOneORZero(bool isEntering)
    {
        foreach (IAudioController aG in audioGroups)
        {
            if (aG is BGMGroup) continue;
            if (isEntering) aG.SetVolumeZero();
            else if (!isEntering) aG.SetVolumeNormal();
        }
    }
    /// <summary>
    /// true = 다이얼로그 입장, false = 다이얼로그 퇴장
    /// </summary>
    /// <param name="isEntering"></param>
    public void SetDialogueState(bool isEntering)
    {
        foreach (IAudioController aG in audioGroups)
        {
            if (aG is DialogueGroup) aG.ResetPlayer();

            if (isEntering && aG is not DialogueGroup) aG.SetVolumeHalf();
            else if (!isEntering && aG is not DialogueGroup) aG.SetVolumeNormal();
        }
    }

    /// <summary>
    /// true = 컷신 입장, false = 컷신 퇴장
    /// </summary>
    /// <param name="isEntering"></param>
    public void SetCutsceneState(bool isEntering)
    {
        foreach (IAudioController aG in audioGroups)
        {
            if (aG is CutsceneGroup) continue;

            if (isEntering) aG.PausePlayer();
            else aG.ResumePlayer();
        }
    }
}
