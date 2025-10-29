using DG.Tweening;
using UnityEngine;

public class ObjectAnimationControl
{
    private readonly Animator anim;
    public ObjectAnimationControl(Animator anim)
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
        anim.Play("EditMode");
    }
}
