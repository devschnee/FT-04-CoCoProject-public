using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class LobbyCharacterAnim
{
    private int number;
    private readonly Animator anim;
    private AnimationClip animClip;

    private String[] lobbyInteractionAnimalAnimsName = { "Bounce", "Roll", "Jump" };
    //private String[] masterClick = { "Click0", "Click1" };
    
    public LobbyCharacterAnim(Animator anim)
    {
        this.anim = anim;
    }

    public void PlayCocoInterationWithMaster(Vector3 pos)
    {
        //anim.SetLookAtPosition(pos);
        anim.Play("Spin");
        AudioEvents.Raise(SFXKey.Cocodoogy, 0, loop: false, pooled: true);
    }

    public void PlayMasterInterationWithCoco(Vector3 pos)
    {
        //anim.SetLookAtPosition(pos);
        anim.Play("InteractCoco0");
        AudioEvents.Raise(SFXKey.Master, 1, loop: false, pooled: true);
    }

    public void MoveAnim(float speed)
    {
        anim.SetFloat("Speed", speed);
    }

    public void ClickAnimals()
    {
        int number = UnityEngine.Random.Range(0, lobbyInteractionAnimalAnimsName.Length);

        anim.Play(lobbyInteractionAnimalAnimsName[number]);
        anim.speed = 0.6f;
    }

    public void ClickMaster()
    {
        //int number = UnityEngine.Random.Range(0, masterClick.Length);
        anim.Play("Click0");
        anim.speed = 3f;
        
    }

    public void StopAnim()
    {
        anim.Play("Idle_A");
    }
    
    public void DefaultAnimSpeed()
    {
        anim.speed = 1f;
    }

    // 인게임 영역
    public void PushBox()
    {
        anim.SetTrigger("Push");
    }
}
