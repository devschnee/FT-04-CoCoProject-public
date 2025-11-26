using System;
using UnityEngine;

public class OtherAnimalAnimationController : MonoBehaviour
{
    [SerializeField] AnimalType animalType;
    public Animator anim;

    [ShowIfAnimal(AnimalType.cow)]
    public Buffalo buffalo;

    [ShowIfAnimal(AnimalType.turtle)]
    public Turtle turtle;

    [ShowIfAnimal(AnimalType.pig)]
    public Boar boar;
    [ShowIfAnimal(AnimalType.pig)]
    public bool hello;

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        switch (animalType)
        {
            case AnimalType.pig:
            if (boar == null) boar = GetComponentInParent<Boar>();
            break;
            case AnimalType.turtle:
            if (turtle == null) turtle = GetComponentInParent<Turtle>();
            break;
            case AnimalType.cow:
            if (buffalo == null) buffalo = GetComponentInParent<Buffalo>();
            break;
            default: throw new Exception("넌 누구냐");
        }
    }

    private void OnEnable()
    {
        switch (animalType)
        {
            case AnimalType.pig:
            boar.OnPushStart += HandleStartPushPush;
            break;
            case AnimalType.turtle:
            break;
            case AnimalType.cow:
            buffalo.OnBombStart += HandleStartBuffaloJumpAnim;
            break;
        }
    }

    void Update()
    {
        switch (animalType)
        {
            case AnimalType.pig:
            hello = boar.IsMoving;
            anim.SetBool("Run", hello);
            break;
            case AnimalType.turtle:
            break;
            case AnimalType.cow:
            break;
            default:
            break;
        }
    }

    private void OnDisable()
    {
        switch (animalType)
        {
            case AnimalType.pig:
            boar.OnPushStart -= HandleStartPushPush;
            break;
            case AnimalType.turtle:
            break;
            case AnimalType.cow:
            buffalo.OnBombStart -= HandleStartBuffaloJumpAnim;
            break;
        }
    }

    // 버팔로

    /// <summary>
    /// 버팔로 스킬 준비
    /// </summary>
    public void HandleStartBuffaloJumpAnim()
    {
        anim.SetTrigger("Lets");
    }
    /// <summary>
    /// 버팔로 점프 후 쾅
    /// </summary>
    public void ChangeJumpToBounce()
    {
        anim.SetTrigger("Bomb");
    }

    // 앞다리살

    /// <summary>
    /// 멧돼지 미는 순간 
    /// </summary>
    public void HandleStartPushPush()
    {
        anim.SetTrigger("Push");
    }

    // 공용 (거북이는 기본 State가 Swim임)
    public void ReturnToIdleState()
    {
        anim.Play("Idle_A");
    }
}
