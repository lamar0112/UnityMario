using UnityEngine;
using UnityEngine.Events;
using System.Collections;

// =====================================================================
// FILE: PlayerHealth.cs
// CURRICULUM: C# scripting (ch. 7-8), Coroutines (ch. 8), Events
// =====================================================================

/// <summary>
/// Manages player health, damage and invincibility frames.
/// CURRICULUM: Coroutines (ch. 8), UnityEvent (ch. 8)
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Header("Invincibility After Damage")]
    [SerializeField] private float invincibilityDuration = 1.5f;
    private bool isInvincible = false;
    private bool hasShield = false;

    [Header("Visual Feedback")]
    // CURRICULUM: Renderer is a standard Unity component (ch. 4)
    [SerializeField] private Renderer[] renderers;

    // CURRICULUM: UnityEvent — C# event system (ch. 8)
    public UnityEvent<int, int> OnHealthChanged; // (current, max)
    public UnityEvent OnPlayerDied;

    private PlayerRespawn respawn;

    private void Awake() => respawn = GetComponent<PlayerRespawn>();

    private void Start()
    {
        if (GameManager.Instance?.SelectedCharacter != null)
            maxHealth = GameManager.Instance.SelectedCharacter.maxHealth;

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (isInvincible) return;

        if (hasShield)
        {
            hasShield = false;
            StartCoroutine(InvincibilityFrames());
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        AudioManager.Instance?.PlayDamage();

        // CURRICULUM: StartCoroutine (ch. 8)
        StartCoroutine(InvincibilityFrames());

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        OnPlayerDied?.Invoke();
        respawn?.Respawn();
    }

    // CURRICULUM: IEnumerator / yield return = Coroutine (ch. 8)
    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        float elapsed = 0f;
        while (elapsed < invincibilityDuration)
        {
            foreach (var r in renderers)
                if (r != null) r.enabled = !r.enabled;
            // CURRICULUM: yield return WaitForSeconds (ch. 8)
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        foreach (var r in renderers)
            if (r != null) r.enabled = true;
        isInvincible = false;
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetShield(bool active) => hasShield = active;
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
}


// =====================================================================
// FILE: PlayerRespawn.cs
// CURRICULUM: Triggers (ch. 6), Transform, CharacterController
// =====================================================================

/// <summary>
/// Handles respawn at checkpoint or after falling.
/// CURRICULUM: CharacterController (ch. 5), Transform (ch. 7)
/// </summary>
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private float killHeight = -20f;
    private Vector3 respawnPoint;
    private CharacterController cc;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        respawnPoint = transform.position;
    }

    // CURRICULUM: Update checks fall death every frame (ch. 7)
    private void Update()
    {
        if (transform.position.y < killHeight)
        {
            Respawn();
            GetComponent<PlayerHealth>()?.TakeDamage(1);
        }
    }

    public void SetCheckpoint(Vector3 position)
    {
        respawnPoint = position;
        Debug.Log($"Checkpoint set: {position}");
    }

    public void Respawn()
    {
        // CharacterController must be disabled to move transform
        if (cc != null) cc.enabled = false;
        transform.position = respawnPoint;
        if (cc != null) cc.enabled = true;
    }
}


// =====================================================================
// FILE: Collectible.cs
// CURRICULUM: Triggers (ch. 6), Particle System (ch. 16)
// =====================================================================

/// <summary>
/// Chaos Orb collectible. Rotates and bobs. Trigger collision with player.
/// CURRICULUM: OnTriggerEnter (ch. 6), ParticleSystem (ch. 16)
/// </summary>
public class Collectible : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float bobHeight = 0.25f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Effects")]
    // CURRICULUM: ParticleSystem (ch. 16)
    [SerializeField] private ParticleSystem collectEffect;

    private Vector3 startPosition;
    private bool collected = false;

    private void Start() => startPosition = transform.position;

    // CURRICULUM: Update runs every frame (ch. 7)
    private void Update()
    {
        if (collected) return;
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    // CURRICULUM: OnTriggerEnter — trigger collision (ch. 6)
    // Requires: Collider with "Is Trigger" checked + Rigidbody on one party
    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        collected = true;
        AudioManager.Instance?.PlayCollectOrb();

        // CURRICULUM: ParticleSystem.Play (ch. 16)
        if (collectEffect != null)
        {
            collectEffect.transform.parent = null;
            collectEffect.Play();
            Destroy(collectEffect.gameObject, 2f);
        }

        GameManager.Instance?.AddOrb();
        gameObject.SetActive(false);
        Destroy(gameObject, 0.1f);
    }
}


// =====================================================================
// FILE: Checkpoint.cs + CheckpointManager.cs
// CURRICULUM: Triggers (ch. 6), Renderer/Material (ch. 4), Singleton
// =====================================================================

/// <summary>
/// Checkpoint that saves the player's respawn position.
/// CURRICULUM: Trigger (ch. 6), Renderer color (ch. 4)
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("Visual State")]
    // CURRICULUM: Renderer and Material are standard components (ch. 4)
    [SerializeField] private Renderer flagRenderer;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor   = Color.yellow;

    // CURRICULUM: ParticleSystem (ch. 16)
    [SerializeField] private ParticleSystem activateEffect;

    private bool isActivated = false;

    private void Start() => SetColor(inactiveColor);

    // CURRICULUM: OnTriggerEnter (ch. 6)
    private void OnTriggerEnter(Collider other)
    {
        if (isActivated) return;
        if (!other.CompareTag("Player")) return;

        isActivated = true;
        SetColor(activeColor);
        activateEffect?.Play();

        other.GetComponent<PlayerRespawn>()?.SetCheckpoint(transform.position + Vector3.up);
        CheckpointManager.Instance?.SetActiveCheckpoint(this);
        AudioManager.Instance?.PlayCheckpoint();
    }

    public void Deactivate()
    {
        isActivated = false;
        SetColor(inactiveColor);
    }

    // CURRICULUM: Renderer.material.color (ch. 4)
    private void SetColor(Color color)
    {
        if (flagRenderer != null)
            flagRenderer.material.color = color;
    }
}

/// <summary>
/// Ensures only one checkpoint is active at a time.
/// CURRICULUM: Singleton pattern (ch. 8)
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }
    private Checkpoint activeCheckpoint;

    private void Awake() => Instance = this;

    public void SetActiveCheckpoint(Checkpoint newCheckpoint)
    {
        if (activeCheckpoint != null && activeCheckpoint != newCheckpoint)
            activeCheckpoint.Deactivate();
        activeCheckpoint = newCheckpoint;
    }
}


// =====================================================================
// FILE: Hazard.cs
// CURRICULUM: Triggers (ch. 6)
// =====================================================================

/// <summary>
/// Hazard zone — damages player on trigger contact.
/// Use on lava, spikes, void zones, water.
/// CURRICULUM: OnTriggerEnter (ch. 6)
/// </summary>
public class Hazard : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private bool instantRespawn = false;

    // CURRICULUM: OnTriggerEnter (ch. 6)
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (instantRespawn)
        {
            other.GetComponent<PlayerRespawn>()?.Respawn();
            other.GetComponent<PlayerHealth>()?.TakeDamage(1);
        }
        else
        {
            other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
        }
    }
}


// =====================================================================
// FILE: MovingPlatform.cs
// CURRICULUM: Transform, Vector3, Collider (ch. 6)
// =====================================================================

/// <summary>
/// Platform that moves between two points.
/// CURRICULUM: Vector3.MoveTowards, Transform, Trigger (ch. 6)
/// </summary>
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float waitTime = 0.5f;

    private Vector3 target;
    private bool waiting = false;

    private void Start()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning($"MovingPlatform {name}: Missing point A or B!");
            enabled = false;
            return;
        }
        target = pointB.position;
    }

    private void Update()
    {
        if (waiting) return;
        // CURRICULUM: Vector3.MoveTowards for smooth movement (ch. 6)
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, target) < 0.05f)
            StartCoroutine(WaitAndSwitch());
    }

    private System.Collections.IEnumerator WaitAndSwitch()
    {
        waiting = true;
        yield return new WaitForSeconds(waitTime);
        target = (target == pointA.position) ? pointB.position : pointA.position;
        waiting = false;
    }

    // CURRICULUM: Player parented to platform to follow it (ch. 6)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) other.transform.SetParent(transform);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) other.transform.SetParent(null);
    }
}


// =====================================================================
// FILE: FallingPlatform.cs
// CURRICULUM: Rigidbody (ch. 6), Coroutines (ch. 8)
// =====================================================================

/// <summary>
/// Platform that falls when player steps on it.
/// CURRICULUM: Rigidbody physics (ch. 6), Coroutines (ch. 8)
/// </summary>
public class FallingPlatform : MonoBehaviour
{
    [SerializeField] private float fallDelay = 0.8f;
    [SerializeField] private float respawnTime = 4f;
    private Rigidbody rb;
    private Vector3 startPos;
    private bool isFalling = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        startPos = transform.position;
    }

    // CURRICULUM: OnCollisionEnter — physics collision (ch. 6)
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player") || isFalling) return;
        StartCoroutine(Fall());
    }

    private System.Collections.IEnumerator Fall()
    {
        isFalling = true;
        yield return new WaitForSeconds(fallDelay);
        rb.isKinematic = false;
        yield return new WaitForSeconds(respawnTime);
        rb.isKinematic = true;
        rb.linearVelocity = rb.angularVelocity = Vector3.zero;
        transform.position = startPos;
        isFalling = false;
    }
}


// =====================================================================
// FILE: JumpPad.cs
// CURRICULUM: Triggers (ch. 6)
// =====================================================================

/// <summary>
/// Launches player upward on contact.
/// CURRICULUM: OnTriggerEnter (ch. 6)
/// </summary>
public class JumpPad : MonoBehaviour
{
    [SerializeField] private float launchForce = 18f;
    [SerializeField] private ParticleSystem bounceEffect;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        other.GetComponent<PlayerController>()?.ApplyJumpPadForce(launchForce);
        bounceEffect?.Play();
        AudioManager.Instance?.PlayJump();
    }
}


// =====================================================================
// FILE: FinishPortal.cs
// CURRICULUM: Triggers (ch. 6), SceneManagement (ch. 23),
//             Particle System (ch. 16), PlayerPrefs (ch. 23)
// =====================================================================

/// <summary>
/// Finish portal that completes the level when player enters.
/// CURRICULUM: Trigger (ch. 6), SceneManagement (ch. 23)
/// </summary>
public class FinishPortal : MonoBehaviour
{
    [Header("Level Info")]
    [SerializeField] private int levelIndex = 1;
    [SerializeField] private int nextLevelIndex = 2;

    [Header("Effects")]
    // CURRICULUM: ParticleSystem (ch. 16)
    [SerializeField] private ParticleSystem portalEffect;

    [Header("UI")]
    [SerializeField] private LevelCompleteUI levelCompleteUI;

    private bool triggered = false;

    private void Start() => portalEffect?.Play();

    // CURRICULUM: OnTriggerEnter (ch. 6)
    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        int score = GameManager.Instance?.Score ?? 0;
        int orbs  = GameManager.Instance?.OrbsCollected ?? 0;
        float time = LevelTimer.Instance?.GetElapsedTime() ?? 0f;

        // CURRICULUM: PlayerPrefs saves highscore (ch. 23)
        int bestScore = PlayerPrefs.GetInt($"BestScore_{levelIndex}", 0);
        if (score > bestScore) PlayerPrefs.SetInt($"BestScore_{levelIndex}", score);
        PlayerPrefs.SetInt($"LevelUnlocked_{nextLevelIndex}", 1);
        PlayerPrefs.Save();

        // Disable player input
        var pc = other.GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;

        AudioManager.Instance?.PlayLevelComplete();

        if (levelCompleteUI != null)
            levelCompleteUI.Show(score, orbs, time);
        else
            Debug.LogWarning("FinishPortal: No LevelCompleteUI assigned!");
    }
}


// =====================================================================
// FILE: LevelTimer.cs
// CURRICULUM: Time.deltaTime (ch. 7), Singleton
// =====================================================================

/// <summary>
/// Tracks elapsed time in a level. Singleton per scene.
/// CURRICULUM: Time.deltaTime (ch. 7)
/// </summary>
public class LevelTimer : MonoBehaviour
{
    public static LevelTimer Instance { get; private set; }
    private float elapsed = 0f;
    private bool running = true;

    private void Awake() => Instance = this;

    // CURRICULUM: Time.deltaTime for frame-independent timing (ch. 7)
    private void Update() { if (running) elapsed += Time.deltaTime; }

    public float GetElapsedTime() => elapsed;
    public void StopTimer() => running = false;
    public void ResetTimer() { elapsed = 0f; running = true; }

    public string GetFormattedTime()
    {
        int min = (int)(elapsed / 60);
        int sec = (int)(elapsed % 60);
        return $"{min:00}:{sec:00}";
    }
}


// =====================================================================
// FILE: CameraFollow.cs
// CURRICULUM: Camera (ch. 5), Transform, Input.GetAxis (ch. 7)
// NOTE: Cinemachine is NOT curriculum — this script replaces it
// =====================================================================

/// <summary>
/// Simple third-person follow camera.
/// CURRICULUM: Camera (ch. 5), Transform, Input.GetAxis (ch. 7)
/// NOTE: Cinemachine is NOT curriculum for PG2202.
///       This script provides the same functionality within curriculum scope.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    // CURRICULUM: Transform reference (ch. 5)
    [SerializeField] private Transform target;

    [Header("Settings")]
    [SerializeField] private float distance     = 6f;
    [SerializeField] private float height       = 2.5f;
    [SerializeField] private float smoothSpeed  = 8f;
    [SerializeField] private float mouseSensitivity = 2f;

    private float yaw   = 0f;
    private float pitch = 20f;

    private void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
            else Debug.LogWarning("CameraFollow: No 'Player' tag found!");
        }
        // CURRICULUM: Cursor lock (ch. 5)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // CURRICULUM: LateUpdate runs after Update — ideal for camera (ch. 7)
    private void LateUpdate()
    {
        if (target == null) return;

        // CURRICULUM: Input.GetAxis for mouse movement (ch. 7)
        yaw   += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch  = Mathf.Clamp(pitch, -10f, 60f);

        // CURRICULUM: Quaternion.Euler for rotation (ch. 5)
        Quaternion rotation     = Quaternion.Euler(pitch, yaw, 0);
        Vector3 offset          = rotation * new Vector3(0, 0, -distance);
        Vector3 desiredPosition = target.position + Vector3.up * height + offset;

        // CURRICULUM: Vector3.Lerp for smooth interpolation (ch. 5)
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1f);
    }

    public float GetYaw() => yaw;
    public void SetTarget(Transform t) => target = t;
}
