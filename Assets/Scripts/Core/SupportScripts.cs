using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =====================================================================
// FILE: AudioManager.cs
// CURRICULUM: Audio — AudioSource, AudioClip (lecture 10)
// =====================================================================

/// <summary>
/// Central audio manager. Singleton.
/// CURRICULUM: AudioSource and AudioClip (lecture 10)
/// Exam requires: "The project must include sound — GUI effects,
/// background music and/or in-game sound effects"
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    // CURRICULUM: AudioSource plays audio (lecture 10)
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Sound Effects")]
    // CURRICULUM: AudioClip is the audio file (lecture 10)
    public AudioClip jumpClip;
    public AudioClip collectOrbClip;
    public AudioClip damageClip;
    public AudioClip powerupClip;
    public AudioClip checkpointClip;
    public AudioClip levelCompleteClip;
    public AudioClip enemyDeathClip;
    public AudioClip menuClickClip;
    public AudioClip portalClip;

    [Header("Music")]
    public AudioClip backgroundMusic;

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 0.4f;
    [Range(0f, 1f)] public float sfxVolume   = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.volume = musicVolume;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.volume = sfxVolume;
        }
    }

    private void Start()
    {
        // CURRICULUM: AudioSource.Play() starts playback (lecture 10)
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    // CURRICULUM: PlayOneShot plays a single sound without interrupting (lecture 10)
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayJump()         => PlaySFX(jumpClip);
    public void PlayCollectOrb()   => PlaySFX(collectOrbClip);
    public void PlayDamage()       => PlaySFX(damageClip);
    public void PlayPowerup()      => PlaySFX(powerupClip);
    public void PlayCheckpoint()   => PlaySFX(checkpointClip);
    public void PlayLevelComplete()=> PlaySFX(levelCompleteClip);
    public void PlayEnemyDeath()   => PlaySFX(enemyDeathClip);
    public void PlayMenuClick()    => PlaySFX(menuClickClip);
    public void PlayPortal()       => PlaySFX(portalClip);
}


// =====================================================================
// FILE: CharacterData.cs (ScriptableObject)
// NOTE: ScriptableObject is NOT in curriculum — used as bonus architecture
// =====================================================================

/// <summary>
/// NOTE: ScriptableObject is NOT curriculum for PG2202.
/// Used as bonus architecture — mention in report as "additional features".
/// Create: Right-click > Create > ChaosQuest > CharacterData
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "ChaosQuest/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string characterName = "Character";
    [TextArea] public string description = "";
    public Sprite icon;

    [Header("Stats")]
    public int maxHealth = 3;
    [Range(0.5f, 2f)] public float speedMultiplier = 1f;
    [Range(0.5f, 2f)] public float jumpMultiplier  = 1f;

    [Header("Prefab")]
    public GameObject characterPrefab;
}


// =====================================================================
// FILE: HUD.cs
// CURRICULUM: UI/Canvas (ch. 14), TextMeshPro, Image
// =====================================================================

/// <summary>
/// In-game HUD — shows health, score, orbs and timer.
/// CURRICULUM: UI Canvas (ch. 14)
/// Exam requires: "GUI elements in-game (e.g. lives, health, score)"
/// </summary>
public class HUD : MonoBehaviour
{
    [Header("Score & Orbs")]
    // CURRICULUM: TextMeshProUGUI is UI text (ch. 14)
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI orbsText;

    [Header("Health")]
    [SerializeField] private TextMeshProUGUI healthText;
    // CURRICULUM: Image is a UI component (ch. 14)
    [SerializeField] private Image[] heartIcons;

    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Powerup")]
    [SerializeField] private Image powerupIcon;
    [SerializeField] private TextMeshProUGUI powerupNameText;

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;

    private void Start()
    {
        if (playerHealth != null)
            // CURRICULUM: UnityEvent.AddListener (ch. 8)
            playerHealth.OnHealthChanged.AddListener(UpdateHealth);

        UpdateScore(0);
        UpdateOrbs(0);
        HidePowerup();
    }

    // CURRICULUM: Update refreshes timer every frame (ch. 7)
    private void Update()
    {
        if (timerText != null && LevelTimer.Instance != null)
            timerText.text = LevelTimer.Instance.GetFormattedTime();

        UpdateScore(GameManager.Instance?.Score ?? 0);
        UpdateOrbs(GameManager.Instance?.OrbsCollected ?? 0);
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = $"Score: {score}";
    }

    private void UpdateOrbs(int orbs)
    {
        if (orbsText != null) orbsText.text = $"Orbs: {orbs}";
    }

    private void UpdateHealth(int current, int max)
    {
        if (healthText != null) healthText.text = $"HP: {current}/{max}";

        // CURRICULUM: Loop over UI elements (ch. 14)
        for (int i = 0; i < heartIcons.Length; i++)
            if (heartIcons[i] != null)
                heartIcons[i].enabled = i < current;
    }

    public void ShowPowerup(string name, Sprite icon)
    {
        if (powerupIcon != null) { powerupIcon.gameObject.SetActive(true); if (icon != null) powerupIcon.sprite = icon; }
        if (powerupNameText != null) { powerupNameText.gameObject.SetActive(true); powerupNameText.text = name; }
    }

    public void HidePowerup()
    {
        powerupIcon?.gameObject.SetActive(false);
        powerupNameText?.gameObject.SetActive(false);
    }
}


// =====================================================================
// FILE: MainMenu.cs
// CURRICULUM: UI (ch. 14), SceneManagement (ch. 23)
// =====================================================================

/// <summary>
/// Controls the main menu.
/// CURRICULUM: UI Canvas (ch. 14), SceneManagement (ch. 23)
/// Exam requires: "Start menu + quit button in executable"
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    // CURRICULUM: GameObject.SetActive shows/hides UI (ch. 14)
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject controlsPanel;

    public void OnStartGame()
    {
        AudioManager.Instance?.PlayMenuClick();
        GameManager.Instance?.LoadScene("CharacterSelect");
    }

    public void OnShowControls()
    {
        AudioManager.Instance?.PlayMenuClick();
        mainPanel?.SetActive(false);
        controlsPanel?.SetActive(true);
    }

    public void OnHideControls()
    {
        AudioManager.Instance?.PlayMenuClick();
        controlsPanel?.SetActive(false);
        mainPanel?.SetActive(true);
    }

    // CURRICULUM: Application.Quit — REQUIRED by exam (lecture 12)
    // "The executable must have a clear and understandable way to close"
    public void OnQuit()
    {
        AudioManager.Instance?.PlayMenuClick();
        GameManager.Instance?.QuitGame();
    }
}


// =====================================================================
// FILE: PauseMenu.cs
// CURRICULUM: UI (ch. 14), Time.timeScale (ch. 6), Input (ch. 7)
// =====================================================================

/// <summary>
/// Pause menu. Press Escape to toggle.
/// CURRICULUM: Time.timeScale (ch. 6), UI (ch. 14), Input (ch. 7)
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    private void Update()
    {
        // CURRICULUM: Input.GetKeyDown (ch. 7)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance?.IsPaused == true)
                OnResume();
            else
                OnPause();
        }
    }

    private void OnPause()
    {
        GameManager.Instance?.PauseGame();
        pausePanel?.SetActive(true);
    }

    public void OnResume()
    {
        GameManager.Instance?.ResumeGame();
        pausePanel?.SetActive(false);
    }

    public void OnRestartLevel()
    {
        GameManager.Instance?.ResumeGame();
        GameManager.Instance?.RestartLevel();
    }

    public void OnMainMenu()
    {
        GameManager.Instance?.ResumeGame();
        GameManager.Instance?.LoadScene("MainMenu");
    }

    // CURRICULUM: Application.Quit (lecture 12)
    public void OnQuit() => GameManager.Instance?.QuitGame();
}


// =====================================================================
// FILE: LevelCompleteUI.cs
// CURRICULUM: UI (ch. 14), PlayerPrefs (ch. 23)
// =====================================================================

/// <summary>
/// Level complete screen with score, orbs and time.
/// CURRICULUM: UI Canvas (ch. 14)
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI orbsText;
    [SerializeField] private TextMeshProUGUI timeText;

    private void Start() => panel?.SetActive(false);

    public void Show(int score, int orbs, float time)
    {
        panel?.SetActive(true);
        // CURRICULUM: Time.timeScale = 0 pauses game (ch. 6)
        Time.timeScale = 0f;

        if (scoreText != null) scoreText.text = $"Score: {score}";
        if (orbsText  != null) orbsText.text  = $"Chaos Orbs: {orbs}";
        if (timeText  != null)
        {
            int min = (int)(time / 60);
            int sec = (int)(time % 60);
            timeText.text = $"Time: {min:00}:{sec:00}";
        }
    }

    public void OnContinue()
    {
        Time.timeScale = 1f;
        GameManager.Instance?.LoadScene("HubWorld");
    }

    public void OnRestartLevel()
    {
        Time.timeScale = 1f;
        GameManager.Instance?.RestartLevel();
    }

    public void OnMainMenu()
    {
        Time.timeScale = 1f;
        GameManager.Instance?.LoadScene("MainMenu");
    }
}
