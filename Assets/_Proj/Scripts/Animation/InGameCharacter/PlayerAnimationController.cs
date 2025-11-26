using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    public Animator anim;
    public PlayerMovement move;
    public PlayerPush push;

    private void Update()
    {
        bool isPushing = push.isPushing;
        bool isRunning = move.isRunning;
        //float speed = move.rb.linearVelocity.magnitude;

        anim.SetBool("Push", isPushing);
        anim.SetBool("Run", isRunning && !isPushing);
        //anim.SetFloat("Speed", speed);
    }
}

