using UnityEngine;
using UnityEngine.SceneManagement;

// =====================================================================
// CURRICULUM: C# scripting (ch. 7-8), SceneManagement (ch. 23)
// Singleton pattern, DontDestroyOnLoad
// =====================================================================

/// <summary>
/// Central game manager. Singleton that persists across scenes.
/// Tracks score, orbs, enemies defeated and game state.
/// CURRICULUM: C# scripting (ch. 7-8), SceneManagement (ch. 23)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    private int score = 0;
    private int orbsCollected = 0;
    private int enemiesDefeated = 0;
    private bool isPaused = false;

    // C# properties — CURRICULUM: ch. 8
    public bool IsPaused => isPaused;
    public int Score => score;
    public int OrbsCollected => orbsCollected;

    [Header("Selected Character")]
    // NOTE: ScriptableObject is NOT in curriculum — used as bonus architecture
    public CharacterData SelectedCharacter { get; private set; }

    // CURRICULUM: Awake is a Unity lifecycle method (ch. 7)
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // CURRICULUM: DontDestroyOnLoad keeps object alive between scenes (ch. 23)
        DontDestroyOnLoad(gameObject);
    }

    public void AddOrb()
    {
        orbsCollected++;
        score += 10;
        Debug.Log($"Orbs: {orbsCollected} | Score: {score}");
    }

    public void AddScore(int amount) => score += amount;

    public void RegisterEnemyDefeated()
    {
        enemiesDefeated++;
        score += 25;
    }

    public void ResetLevelStats()
    {
        score = 0;
        orbsCollected = 0;
        enemiesDefeated = 0;
    }

    public int GetEnemiesDefeated() => enemiesDefeated;

    // CURRICULUM: Time.timeScale pauses physics and Update (ch. 6)
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
    }

    // CURRICULUM: SceneManager.LoadScene (ch. 23)
    public void LoadScene(string sceneName)
    {
        ResumeGame();
        SceneManager.LoadScene(sceneName);
    }

    public void RestartLevel()
    {
        ResumeGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // CURRICULUM: Application.Quit — REQUIRED by exam! (lecture 12)
    // "The executable must have a clear way to close"
    public void QuitGame()
    {
        ResumeGame();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetSelectedCharacter(CharacterData data)
    {
        SelectedCharacter = data;
        // CURRICULUM: PlayerPrefs saves simple data (ch. 23)
        if (data != null)
            PlayerPrefs.SetString("LastCharacter", data.characterName);
    }
}
