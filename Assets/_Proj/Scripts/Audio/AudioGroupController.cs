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
    public void ResetAllAudioGroupOutGame()
    {
        foreach (IAudioController aG in audioGroups)
        {
            if (aG is SFXGroup) (aG as SFXGroup).ResetPlayer(1f, SFXMode.OutGame);
            else if (aG is AmbientGroup) (aG as AmbientGroup).ResetPlayer(1f, SFXMode.OutGame);
            else if (aG is CutsceneGroup) aG.ResetPlayer(0.7f);
            else if (aG is UIGroup) aG.ResetPlayer(0.34f);
            else aG.ResetPlayer(1f);
        }
    }
    public void ResetAllAudioGroupInGame()
    {
        foreach (IAudioController aG in audioGroups)
        {
            if (aG is SFXGroup) (aG as SFXGroup).ResetPlayer(1f, SFXMode.InGame);
            else if (aG is AmbientGroup) (aG as AmbientGroup).ResetPlayer(1f, SFXMode.InGame);
            else if (aG is CutsceneGroup) aG.ResetPlayer(0.7f);
            else if (aG is UIGroup) aG.ResetPlayer(1f);
            else aG.ResetPlayer(1f);
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
            if (aG is DialogueGroup) aG.ResetPlayer(1f);

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

            if (isEntering)
            {
                if (aG is CutsceneGroup) aG.PlayPlayer();
                else aG.PausePlayer();  
            }
            else
            {
                if (aG is CutsceneGroup) aG.StopPlayer();
                else aG.ResumePlayer();  
            }
        }
    }
}
