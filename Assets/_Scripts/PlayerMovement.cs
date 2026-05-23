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
    private PlayerAnimationController animController;

    private Rigidbody rb;
    private Vector3 velocity;
    private Vector2 moveInput;
    private bool jumpRequested;
    private bool isGrounded;
    private Vector3 lastPosition;

    Vector3 GetCameraRelativeDirection()
    {
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        return camForward * moveInput.y + camRight * moveInput.x;
    }

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
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Update()
    {
        moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        bool isMoving = moveInput.magnitude > 0.1f;
        animController.SetMoving(isMoving);


        UpdateAnimationsPrePhysics();
    }

    void FixedUpdate()
    {
        CheckGrounded();

        if (movementMode == MovementMode.Rigidbody && rb != null && !rb.isKinematic)
        {
            ApplyMovementRigidbody();
            HandleJumpRigidbody();
            lastPosition = transform.position;
        }
        else
        {
            ApplyGravity(Time.fixedDeltaTime);
            HandleJumpTransform();
            ApplyMovementTransform(Time.fixedDeltaTime);
            lastPosition = transform.position;
        }

        jumpRequested = false;
    }


    void CheckGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayers);
    }

    void ApplyMovementRigidbody()
    {
        Vector3 moveDir = GetCameraRelativeDirection();

        Vector3 horizontalVel = new Vector3(
            moveDir.x * moveSpeed,
            rb.linearVelocity.y,
            moveDir.z * moveSpeed
        );

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

    void ApplyGravity(float dt)
    {
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

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
        Vector3 moveDir = GetCameraRelativeDirection();

        Vector3 horizontalMove = moveDir * moveSpeed;
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
            speed = delta.magnitude / Time.fixedDeltaTime;
        }
    }

    void LateUpdate()
    {
        UpdateAnimations();
    }
}
