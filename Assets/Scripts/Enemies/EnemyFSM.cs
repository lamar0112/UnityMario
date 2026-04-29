using UnityEngine;

// =====================================================================
// CURRICULUM: Game AI / Finite State Machines (lecture 5),
//             Steering Behaviors - Seek (lecture 6),
//             Rigidbody physics (ch. 6)
// =====================================================================

/// <summary>
/// Enemy AI using a Finite State Machine (FSM).
/// FSM is curriculum from lecture 5 (Game AI, Agents, FSMs).
/// Seek steering behavior is curriculum from lecture 6.
///
/// States: Patrol → Chase → Stunned → Dead
/// Transitions: detect player → chase | player too far → patrol
/// </summary>
public class EnemyFSM : MonoBehaviour
{
    // CURRICULUM: Enum for FSM states (lecture 5)
    // "A state is a condition or situation during the life of an object"
    public enum EnemyState
    {
        Patrol,   // Move between waypoints
        Chase,    // Seek toward player (lecture 6)
        Stunned,  // Briefly stopped
        Dead      // Game over for this enemy
    }

    [Header("FSM - Current State")]
    [SerializeField] private EnemyState currentState = EnemyState.Patrol;

    [Header("Waypoints (Patrol state)")]
    // CURRICULUM: Array of Transform references (ch. 7)
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float waypointReachDistance = 0.4f;
    private int currentWaypointIndex = 0;

    [Header("Detection (Patrol → Chase transition)")]
    [SerializeField] private float detectionRadius = 7f;
    [SerializeField] private float giveUpRadius = 13f;

    [Header("Chase Speed")]
    [SerializeField] private float chaseSpeed = 4f;

    [Header("Stun")]
    [SerializeField] private float stunDuration = 1f;
    private float stunTimer = 0f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 2;
    private int currentHealth;

    [Header("Damage to Player")]
    [SerializeField] private int damageToPlayer = 1;

    [Header("Effects")]
    // CURRICULUM: ParticleSystem (ch. 16)
    [SerializeField] private ParticleSystem deathEffect;
    // CURRICULUM: Animator (lecture 9)
    [SerializeField] private Animator animator;

    private static readonly int AnimWalking = Animator.StringToHash("IsWalking");
    private static readonly int AnimChasing = Animator.StringToHash("IsChasing");
    private static readonly int AnimHit     = Animator.StringToHash("Hit");
    private static readonly int AnimDie     = Animator.StringToHash("Die");

    private Transform player;
    private bool isDead = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // CURRICULUM: FindGameObjectWithTag (ch. 7)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning($"EnemyFSM on {name}: No GameObject with tag 'Player' found!");
    }

    // CURRICULUM: Update runs the FSM every frame (ch. 7)
    private void Update()
    {
        if (isDead) return;

        // CURRICULUM: FSM switch statement (lecture 5)
        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol_Update();
                CheckForPlayer();       // Transition: Patrol → Chase
                break;

            case EnemyState.Chase:
                Chase_Update();         // Seek steering behavior (lecture 6)
                CheckGiveUp();          // Transition: Chase → Patrol
                break;

            case EnemyState.Stunned:
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0f)
                    ChangeState(EnemyState.Patrol);
                break;

            case EnemyState.Dead:
                break;
        }
    }

    // ------------------------------------------------------------------
    // CURRICULUM: FSM state methods (lecture 5)
    // ------------------------------------------------------------------

    /// <summary>
    /// Patrol between waypoints — simplified pathfinding (lecture 7).
    /// </summary>
    private void Patrol_Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypointIndex];
        if (target == null) return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.magnitude < waypointReachDistance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            new Vector3(target.position.x, transform.position.y, target.position.z),
            moveSpeed * Time.deltaTime
        );

        if (direction.magnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(direction), 8f * Time.deltaTime);

        animator?.SetBool(AnimWalking, true);
        animator?.SetBool(AnimChasing, false);
    }

    /// <summary>
    /// CURRICULUM: Seek steering behavior — move toward target (lecture 6).
    /// "Seek moves toward the goal. Seek does not slow down near the goal."
    /// </summary>
    private void Chase_Update()
    {
        if (player == null) return;

        // CURRICULUM: Seek — calculate direction vector toward player
        Vector3 rawDirection  = player.position - transform.position;
        Vector3 normDirection = rawDirection.normalized;
        normDirection.y = 0f;

        // Move toward player at chase speed
        transform.position = Vector3.MoveTowards(
            transform.position,
            new Vector3(player.position.x, transform.position.y, player.position.z),
            chaseSpeed * Time.deltaTime
        );

        if (normDirection.magnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(normDirection), 12f * Time.deltaTime);

        animator?.SetBool(AnimChasing, true);
        animator?.SetBool(AnimWalking, false);
    }

    // ------------------------------------------------------------------
    // CURRICULUM: FSM transitions (lecture 5)
    // ------------------------------------------------------------------
    private void CheckForPlayer()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) < detectionRadius)
            ChangeState(EnemyState.Chase);
    }

    private void CheckGiveUp()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) > giveUpRadius)
            ChangeState(EnemyState.Patrol);
    }

    /// <summary>
    /// CURRICULUM: FSM state transition (lecture 5)
    /// "Transitions are edges between states"
    /// </summary>
    private void ChangeState(EnemyState newState)
    {
        currentState = newState;
    }

    // CURRICULUM: OnCollisionEnter — physics collision (ch. 6)
    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        collision.gameObject.GetComponent<PlayerHealth>()?.TakeDamage(damageToPlayer);
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        animator?.SetTrigger(AnimHit);

        if (currentHealth <= 0)
            Die();
        else
        {
            ChangeState(EnemyState.Stunned);
            stunTimer = stunDuration;
        }
    }

    private void Die()
    {
        isDead = true;
        ChangeState(EnemyState.Dead);
        animator?.SetTrigger(AnimDie);

        // CURRICULUM: ParticleSystem.Play (ch. 16)
        if (deathEffect != null)
        {
            deathEffect.transform.parent = null;
            deathEffect.Play();
            Destroy(deathEffect.gameObject, 2f);
        }

        // CURRICULUM: AudioSource (lecture 10)
        AudioManager.Instance?.PlayEnemyDeath();
        GameManager.Instance?.RegisterEnemyDefeated();

        GetComponent<Collider>().enabled = false;
        Destroy(gameObject, 0.5f);
    }

    // CURRICULUM: Gizmos for visualizing AI radii in Scene view (ch. 7)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, giveUpRadius);
    }
}
