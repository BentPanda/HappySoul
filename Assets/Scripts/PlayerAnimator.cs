using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    public PlayerMovement movement;
    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // 1) Speed = ground-plane speed magnitude
        Vector3 horizVel = new Vector3(movement.Velocity.x, 0, movement.Velocity.z);
        anim.SetFloat("Speed", horizVel.magnitude);

        // 2) LockedOn flag
        anim.SetBool("LockedOn", movement.IsLockedOn);

        // 3) If locked, drive the 2D blend
        if (movement.IsLockedOn)
        {
            // Map input to local space so X=right, Y=forward
            Vector3 inputDir = new Vector3(movement.InputVector.x, 0, movement.InputVector.y);
            Vector3 local = transform.InverseTransformDirection(inputDir).normalized;
            anim.SetFloat("MoveX", local.x);
            anim.SetFloat("MoveY", local.z);
        }
        else
        {
            // clear if not locked
            anim.SetFloat("MoveX", 0f);
            anim.SetFloat("MoveY", 0f);
        }
    }
}
