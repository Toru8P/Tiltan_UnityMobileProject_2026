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
    public float gravity = -20f;
    public float jumpForce = 8f;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayers = ~0;

    [Header("References")]
    public Animator animator;

    private Rigidbody rb;
    private Vector3 velocity; // used only for Transform mode
    private Vector2 moveInput;
    private bool jumpRequested;
    private bool isGrounded;
    private Vector3 lastPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (movementMode == MovementMode.Rigidbody && rb == null)
        {
            Debug.LogWarning("Rigidbody movement selected but no Rigidbody found. Falling back to Transform movement.");
            movementMode = MovementMode.Transform;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        lastPosition = transform.position;

        // If using Rigidbody, make it stable: prevent tipping and enable interpolation for smoother motion.
        if (movementMode == MovementMode.Rigidbody && rb != null)
        {
            // Freeze X and Z rotation so the character doesn't fall over
            rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            // Prefer using Unity gravity and Rigidbody velocity for vertical motion
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Update()
    {
        // Read input every frame
        moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (Input.GetButtonDown("Jump"))
            jumpRequested = true;

        // Update animator parameters that don't depend on physics timestep
        UpdateAnimationsPrePhysics();
    }

    void FixedUpdate()
    {
        // Ground check uses the object's position and configured ground layers
        CheckGrounded();

        if (movementMode == MovementMode.Rigidbody && rb != null && !rb.isKinematic)
        {
            ApplyMovementRigidbody();
            HandleJumpRigidbody();
            // Track last position for animation calculation after physics
            lastPosition = transform.position;
        }
        else
        {
            // Transform-based (non-physics) movement
            ApplyGravity(Time.fixedDeltaTime);
            HandleJumpTransform();
            ApplyMovementTransform(Time.fixedDeltaTime);
            lastPosition = transform.position;
        }

        // Reset jump request (consumed in this physics tick)
        jumpRequested = false;
    }

    void CheckGrounded()
    {
        // Raycast downward from slightly above the object's position.
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayers);
    }

    // --- Rigidbody mode helpers ---

    void ApplyMovementRigidbody()
    {
        // Horizontal move in world XZ as original implementation
        Vector3 horizontalVel = new Vector3(moveInput.x * moveSpeed, rb.linearVelocity.y, moveInput.y * moveSpeed);

        // If near-ground and no vertical movement requested, ensure small downward velocity to stay grounded
        if (isGrounded && rb.linearVelocity.y < 0f)
            horizontalVel.y = -2f;

        rb.linearVelocity = horizontalVel;
    }

    void HandleJumpRigidbody()
    {
        if (jumpRequested && isGrounded)
        {
            Vector3 v = rb.linearVelocity;
            v.y = jumpForce;
            rb.linearVelocity = v;
            animator?.SetTrigger("Jump");
        }
    }

    // --- Transform mode helpers (non-physics fallback) ---

    void ApplyGravity(float dt)
    {
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f; // small downward force to keep grounded

        velocity.y += gravity * dt;
    }

    void HandleJumpTransform()
    {
        if (jumpRequested && isGrounded)
        {
            velocity.y = jumpForce;
            animator?.SetTrigger("Jump");
        }
    }

    void ApplyMovementTransform(float dt)
    {
        Vector3 horizontalMove = new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed;
        Vector3 verticalMove = Vector3.up * velocity.y;
        Vector3 delta = (horizontalMove + verticalMove) * dt;
        transform.position += delta;
    }

    void UpdateAnimationsPrePhysics()
    {
        if (animator == null)
            return;

        if (movementMode == MovementMode.Transform)
        {
            float approxSpeed = new Vector2(moveInput.x, moveInput.y).magnitude * moveSpeed;
            animator.SetFloat("Speed", approxSpeed);
        }
    }

    void UpdateAnimations()
    {
        if (animator == null)
            return;

        float speed;

        if (movementMode == MovementMode.Rigidbody && rb != null)
        {
            Vector3 horVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            speed = horVel.magnitude;
        }
        else
        {
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;
            // Use fixedDeltaTime because lastPosition is updated in FixedUpdate
            speed = delta.magnitude / Time.fixedDeltaTime;
        }

        animator.SetFloat("Speed", speed);
    }

    void LateUpdate()
    {
        // UpdateAnimations runs after physics so the animator sees the final velocity/position
        UpdateAnimations();
    }
}
