using DG.Tweening;
using UnityEngine;

public class ObjectAnimationController
{
    private readonly Animator anim;
    public ObjectAnimationController(Animator anim)
    {
        this.anim = anim;
    }

    public void PlayRunAmin()
    {
        anim.Play("Run");
    }

    public void PlaySpinAmin()
    {
        anim.Play("Spin");
    }
    public void PlayRollAmin()
    {
        anim.Play("Roll");
    }

    public void MoveAnim(float speed)
    {
        anim.SetFloat("Speed", speed);
    }

    public void StopAnim()
    {
        anim.Play("Jump");
    }
}
