using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [Header("Movement")]
    public float punchForwardForce = 1f;
    public float kickForwardForce = 1.5f;
    public float returnSpeed = 5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.1f;
    public LayerMask groundMask;

    private Animator animator;
    private Rigidbody rb;

    // Internal state
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool returningToOriginal = false;

    // Queue for attack animations
    private Queue<string> attackQueue = new Queue<string>();
    private bool isAttacking = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        animator.applyRootMotion = false;
    }

    void Update()
    {
        if (IsGrounded())
        {
            // Queue punch
            if (Input.GetKeyDown(KeyCode.Space))
            {
                attackQueue.Enqueue("Punch");
                if (!isAttacking) StartCoroutine(ProcessAttackQueue());
            }

            // Queue kick
            if (Input.GetKeyDown(KeyCode.K))
            {
                attackQueue.Enqueue("Kick");
                if (!isAttacking) StartCoroutine(ProcessAttackQueue());
            }
        }
    }

    void FixedUpdate()
    {
        // Keep Y velocity stable
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, rb.linearVelocity.z);

        // Smoothly return to original position after animation
        if (returningToOriginal)
        {
            rb.MovePosition(Vector3.Lerp(rb.position, originalPosition, Time.fixedDeltaTime * returnSpeed));
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, originalRotation, Time.fixedDeltaTime * returnSpeed));

            if (Vector3.Distance(rb.position, originalPosition) < 0.01f)
                returningToOriginal = false;
        }
    }

    IEnumerator ProcessAttackQueue()
    {
        isAttacking = true;

        while (attackQueue.Count > 0)
        {
            string attackName = attackQueue.Dequeue();
            float forwardForce = attackName == "Punch" ? punchForwardForce : kickForwardForce;

            yield return StartCoroutine(PlayCombatAnimation(attackName, forwardForce));
        }

        isAttacking = false;
    }

    IEnumerator PlayCombatAnimation(string triggerName, float forwardForce)
    {
        // Save position/rotation before attack
        originalPosition = rb.position;
        originalRotation = rb.rotation;
        returningToOriginal = false;

        animator.SetTrigger(triggerName);

        // Move forward slightly during attack
        float elapsed = 0f;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float animDuration = stateInfo.length;

        while (elapsed < animDuration)
        {
            Vector3 forwardMove = transform.forward * forwardForce * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + forwardMove);

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        returningToOriginal = true;
        yield return new WaitForSeconds(animDuration / 2); // optional buffer
    }

    bool IsGrounded()
    {
        return Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
    }
}

