// PlayerAnimator.cs
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    public PlayerMovement movement;
    Animator anim;

    void Awake() => anim = GetComponent<Animator>();

    void Update()
    {
        // drive locomotion blend
        Vector3 hv = new Vector3(movement.Velocity.x, 0, movement.Velocity.z);
        anim.SetFloat("Speed", hv.magnitude);

        anim.SetBool("LockedOn", movement.IsLockedOn);

        if (movement.IsLockedOn)
        {
            var inV = new Vector3(movement.InputVector.x, 0, movement.InputVector.y);
            var local = transform.InverseTransformDirection(inV).normalized;
            anim.SetFloat("MoveX", local.x);
            anim.SetFloat("MoveY", local.z);
        }
        else
        {
            anim.SetFloat("MoveX", 0f);
            anim.SetFloat("MoveY", 0f);
        }
    }
}
