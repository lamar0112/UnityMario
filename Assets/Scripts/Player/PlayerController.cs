using UnityEngine;

// =====================================================================
// CURRICULUM: CharacterController (ch. 5), Input.GetAxis (ch. 7),
//             Physics/gravity (ch. 6), C# scripting (ch. 7-8)
// NOTE: We use the old Input system (Input.GetAxis) as it is in curriculum.
//       The new Input System is NOT in curriculum for PG2202.
// =====================================================================

/// <summary>
/// Controls player movement using CharacterController and Input.GetAxis.
/// CURRICULUM: CharacterController (ch. 5), Input.GetAxis (ch. 7)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintMultiplier = 1.6f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Gravity")]
    // CURRICULUM: Manual gravity with CharacterController (ch. 5-6)
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundMask;

    [Header("Character Stat Multipliers")]
    public float speedMultiplier = 1f;
    public float jumpMultiplier = 1f;

    // Private state
    private CharacterController cc;
    private Vector3 velocity;
    private bool isGrounded;
    private bool canDoubleJump = false;
    private bool hasDoubleJumped = false;
    private float speedBoostMultiplier = 1f;

    // CURRICULUM: Animator is curriculum (lecture 9)
    private Animator animator;

    // Cache animator parameter hashes for performance
    private static readonly int AnimSpeed     = Animator.StringToHash("Speed");
    private static readonly int AnimGrounded  = Animator.StringToHash("IsGrounded");
    private static readonly int AnimJump      = Animator.StringToHash("Jump");

    // CURRICULUM: Awake and Start are Unity lifecycle methods (ch. 7)
    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        // CURRICULUM: GetComponentInChildren searches child objects (ch. 7)
        animator = GetComponentInChildren<Animator>();

        if (cc == null)
            Debug.LogError("PlayerController: Missing CharacterController component!");
    }

    private void Start()
    {
        // Apply character stats from GameManager
        if (GameManager.Instance?.SelectedCharacter != null)
        {
            speedMultiplier = GameManager.Instance.SelectedCharacter.speedMultiplier;
            jumpMultiplier  = GameManager.Instance.SelectedCharacter.jumpMultiplier;
        }
    }

    // CURRICULUM: Update runs once per frame (ch. 7)
    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        CheckGround();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        UpdateAnimator();
    }

    // CURRICULUM: Physics.CheckSphere — collision detection (ch. 6)
    private void CheckGround()
    {
        isGrounded = Physics.CheckSphere(
            transform.position + Vector3.down * 0.1f,
            groundCheckRadius,
            groundMask
        );

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            hasDoubleJumped = false;
        }
    }

    // CURRICULUM: Input.GetAxis is the curriculum input method (ch. 7)
    // NOTE: Cinemachine is NOT curriculum — we use Camera.main.transform directly
    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (Mathf.Abs(h) < 0.1f && Mathf.Abs(v) < 0.1f) return;

        // Move relative to camera direction
        Transform cam = Camera.main?.transform;
        Vector3 forward = cam != null ? cam.forward : transform.forward;
        Vector3 right   = cam != null ? cam.right   : transform.right;
        forward.y = 0f;
        right.y   = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = forward * v + right * h;

        float currentSpeed = moveSpeed * speedMultiplier * speedBoostMultiplier;

        // CURRICULUM: Input.GetButton for sprint (ch. 7)
        if (Input.GetButton("Fire3")) // Left Shift by default
            currentSpeed *= sprintMultiplier;

        // CURRICULUM: CharacterController.Move() (ch. 5)
        cc.Move(moveDir * currentSpeed * Time.deltaTime);

        // Rotate player to face movement direction
        if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 15f * Time.deltaTime);
        }
    }

    private void HandleJump()
    {
        // CURRICULUM: Input.GetButtonDown for single press (ch. 7)
        if (!Input.GetButtonDown("Jump")) return;

        if (isGrounded)
        {
            velocity.y = jumpForce * jumpMultiplier;
            // CURRICULUM: Animator.SetTrigger controls animation state (lecture 9)
            animator?.SetTrigger(AnimJump);
            AudioManager.Instance?.PlayJump();
        }
        else if (canDoubleJump && !hasDoubleJumped)
        {
            velocity.y = jumpForce * jumpMultiplier * 0.85f;
            hasDoubleJumped = true;
            animator?.SetTrigger(AnimJump);
        }
    }

    private void ApplyGravity()
    {
        // CURRICULUM: Gravity using deltaTime (ch. 6)
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }

    // CURRICULUM: Animator parameters controlled from script (lecture 9)
    private void UpdateAnimator()
    {
        if (animator == null) return;
        float speed = new Vector2(cc.velocity.x, cc.velocity.z).magnitude;
        animator.SetFloat(AnimSpeed, speed);
        animator.SetBool(AnimGrounded, isGrounded);
    }

    // Called by pause menu (Escape key)
    private void Update_Pause()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance?.IsPaused == true)
                GameManager.Instance.ResumeGame();
            else
                GameManager.Instance?.PauseGame();
        }
    }

    // Public methods called by PowerUp system
    public void SetDoubleJump(bool active)      => canDoubleJump = active;
    public void SetSpeedBoost(float multiplier) => speedBoostMultiplier = multiplier;
    public void ResetSpeedBoost()               => speedBoostMultiplier = 1f;

    // Called by JumpPad
    public void ApplyJumpPadForce(float force) => velocity.y = force;

    // CURRICULUM: Gizmos for debugging in Scene view (ch. 7)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * 0.1f, groundCheckRadius);
    }
}
