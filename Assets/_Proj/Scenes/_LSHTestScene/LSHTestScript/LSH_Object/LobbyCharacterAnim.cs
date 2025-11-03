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
    }
    
    public void StopAnim()
    {
        
    }

}
