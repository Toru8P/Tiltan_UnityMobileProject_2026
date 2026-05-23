using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    public enum MovementMode
    {
        Rigidbody,
        Transform
    }

    [Header("Movement")]
    public MovementMode movementMode = MovementMode.Rigidbody;
    public float moveSpeed = 6f;

    [Header("Roll Settings")]
    public float rollForce = 12f;
    public float rollDuration = 0.35f;
    public float rollCooldown = 0.8f;

    [Header("References")]
    public Animator animator;
    private PlayerAnimationController animController;

    private Rigidbody rb;
    private Vector2 moveInput;
    private Vector3 lastPosition;

    private bool isRolling;
    private float rollTimer;
    private float rollCooldownTimer;
    private Vector3 rollDirection;

    void Start()
    {
        animController = GetComponent<PlayerAnimationController>();
        rb = GetComponent<Rigidbody>();

        if (movementMode == MovementMode.Rigidbody && rb == null)
        {
            Debug.LogWarning("Rigidbody movement selected but no Rigidbody found. Falling back to Transform movement.");
            movementMode = MovementMode.Transform;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        lastPosition = transform.position;

        if (movementMode == MovementMode.Rigidbody && rb != null)
        {
            rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Update()
    {
        moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        bool isMoving = moveInput.magnitude > 0.1f;
        animController?.SetMoving(isMoving);

        if (rollCooldownTimer > 0f)
            rollCooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) && !isRolling && rollCooldownTimer <= 0f)
            StartRoll();
    }

    void FixedUpdate()
    {
        if (isRolling)
        {
            ApplyRoll();
            return; // skip normal movement while rolling
        }

        if (movementMode == MovementMode.Rigidbody && rb != null && !rb.isKinematic)
        {
            ApplyMovementRigidbody();
        }
        else
        {
            ApplyMovementTransform(Time.fixedDeltaTime);
        }

        lastPosition = transform.position;
    }

    Vector3 GetCameraRelativeDirection()
    {
        var cam = Camera.main;
        if (cam == null)
            return new Vector3(moveInput.x, 0f, moveInput.y);

        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();
        return camForward * moveInput.y + camRight * moveInput.x;
    }

    void ApplyMovementRigidbody()
    {
        Vector3 moveDir = GetCameraRelativeDirection();

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        Vector3 horizontalVel = new Vector3(
            moveDir.x * moveSpeed,
            rb.linearVelocity.y,
            moveDir.z * moveSpeed
        );

        rb.linearVelocity = horizontalVel;
    }

    void ApplyMovementTransform(float dt)
    {
        Vector3 moveDir = GetCameraRelativeDirection();

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        Vector3 horizontalMove = moveDir * moveSpeed;
        Vector3 delta = horizontalMove * dt;
        transform.position += delta;
    }

    void StartRoll()
    {
        if (moveInput.magnitude < 0.1f)
            rollDirection = transform.forward;
        else
            rollDirection = GetCameraRelativeDirection().normalized;

        isRolling = true;
        rollTimer = rollDuration;
        rollCooldownTimer = rollCooldown;
        animator?.SetTrigger("Roll");
    }

    void ApplyRoll()
    {
        rollTimer -= Time.fixedDeltaTime;

        if (movementMode == MovementMode.Rigidbody && rb != null)
        {
            rb.linearVelocity = rollDirection * rollForce;
        }
        else
        {
            transform.position += rollDirection * rollForce * Time.fixedDeltaTime;
        }

        if (rollTimer <= 0f)
            isRolling = false;
    }
}
