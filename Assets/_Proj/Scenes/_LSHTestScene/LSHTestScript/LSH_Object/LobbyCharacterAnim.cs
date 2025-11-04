using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class LobbyCharacterAnim
{
    private int number;
    private readonly Animator anim;
    private AnimationClip animClip;

    private String[] lobbyInteractionAnimalAnimsName = { "Bounce", "Roll", "Spin", "Jump" };
    private String[] masterClick = { "Click0", "Click1" };
    
    public LobbyCharacterAnim(Animator anim)
    {
        this.anim = anim;
    }

    public void PlaySpinAmin()
    {
        anim.Play("Spin");
    }

    public void MoveAnim(float speed)
    {
        anim.SetFloat("Speed", speed);
    }

    public void InteractionAnim()
    {
        int number = UnityEngine.Random.Range(0, lobbyInteractionAnimalAnimsName.Length);

        anim.Play(lobbyInteractionAnimalAnimsName[number]);
        anim.speed = 0.8f;
    }

    public void ClickMaster()
    {
        int number = UnityEngine.Random.Range(0, masterClick.Length);
        anim.Play(masterClick[number]);
        anim.speed = 4;
        
    }
    
    public void StopAnim()
    {
        
    }

}
