using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class LobbyCharacterAnim
{
    private int number;
    private readonly MonoBehaviour owner;
    private readonly Animator anim;
    private AnimationClip animClip;

    private String[] lobbyInteractionAnimalAnimsName = { "Bounce", "Roll", "Jump" };
    //private String[] masterClick = { "Click0", "Click1" };
    
    public LobbyCharacterAnim(MonoBehaviour owner, Animator anim)
    {
        this.owner = owner;
        this.anim = anim;
    }

    public void PlayCocoInterationWithMaster()
    {
        //anim.SetLookAtPosition(pos);
        anim.Play("Spin");
        AudioEvents.Raise(SFXKey.OutGameCocodoogy, 0, loop: false, pooled: true, pos: owner.transform.position);
    }

    public void PlayMasterInterationWithCoco()
    {
        //anim.SetLookAtPosition(pos);
        anim.Play("InteractCoco2");
        AudioEvents.Raise(SFXKey.OutGameMaster, 1, loop: false, pooled: true, pos: owner.transform.position);
    }

    public void PlayCocoInteractionWithAnimal()
    {
        anim.Play("Spin2");
        AudioEvents.Raise(SFXKey.OutGameCocodoogy, 0, loop: false, pooled: true, pos: owner.transform.position);
    }
    public void PlayAnimalInteractionWithCoco()
    {
        anim.Play("Spin");
        AudioEvents.Raise(SFXKey.OutGameMaster, 1, loop: false, pooled: true, pos: owner.transform.position);
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
