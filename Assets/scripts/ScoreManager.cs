using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Controla la partida en la GameScene: tiempo, puntuación y high score.
// Ponlo en un GameObject vacío "ScoreManager" dentro de [MANAGERS] y arrastra la UI en el Inspector.
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // Clave de PlayerPrefs (la usa también el Lobby para mostrar el best score)
    public const string HighScoreKey = "HighScore";

    [Header("UI_ScoreCurrent")]
    public GameObject uiScoreCurrent;
    public TMP_Text textCurrentScore;

    [Header("UI_Timer")]
    public GameObject uiTimer;
    public TMP_Text textTime;
    [Tooltip("Imagen de la barra (PorgressBar_Fill) con Image Type = Filled")]
    public Image timerFillImage;
    [Tooltip("Opcional: si usas un Slider en vez de imagen de relleno")]
    public Slider timerSlider;

    [Header("UI_ScoreFinal")]
    public GameObject uiScoreFinal;
    public TMP_Text textFinalScore;
    public TMP_Text textHighScore;

    [Header("Partida")]
    [Tooltip("Duración de la partida en segundos")]
    public float gameDuration = 60f;
    public int pointsPerHit = 100;
    public int pointsPerMiss = -50;
    [Tooltip("Generador de cubos, para pararlo al acabar")]
    public blockGeneratorScript cubeGenerator;
    [Tooltip("Nombre de la escena del lobby (para el botón Back to Lobby)")]
    public string lobbySceneName = "LobbyScene";

    [Header("Waves")]
    [Tooltip("Si està assignat, la partida es divideix en waves i s'acaba després de l'última")]
    public WaveManager waveManager;
    [Tooltip("Segons de pausa entre waves")]
    public float waveBreakDuration = 5f;

    [Header("Ronda superada")]
    [Tooltip("Missatge que surt en acabar cada ronda/wave")]
    public string roundClearedMessage = "RONDA SUPERADA!";
    [Tooltip("Opcional: text gran només per al missatge. Si està buit, el missatge surt al text de la wave (Text_Wave)")]
    public TMP_Text textRoundCleared;

    [Header("Rondas (només si no hi ha WaveManager)")]
    [Tooltip("Segundos de pausa (sin cubos) entre rondas")]
    public float breakDuration = 10f;
    [Tooltip("Cuánto aumenta la velocidad de los cubos en cada ronda (0.2 = +20%)")]
    public float speedIncreasePerRound = 0.2f;
    [Tooltip("Cuánto aumenta la frecuencia de aparición en cada ronda (0.25 = +25%)")]
    public float spawnRateIncreasePerRound = 0.25f;
    [Tooltip("Opcional: texto donde mostrar la cuenta atrás de la pausa (ej. 'Siguiente ronda en 10')")]
    public TMP_Text textNextRound;

    [Header("Sons de cubs destruïts")]
    [Tooltip("So quan es destrueix un cub de pistola (ex. VR-Game-Assets/Audio/gunSound.wav)")]
    public AudioClip gunCubeClip;
    [Tooltip("So quan es talla un cub d'espasa (ex. VR-Game-Assets/Audio/SliceSound.wav)")]
    public AudioClip swordCubeClip;
    [Range(0f, 1f)] public float cubeSoundVolume = 1f;

    private AudioSource audioSource;

    public int Score { get; private set; }        // score de la ronda actual (vuelve a 0 cada ronda)
    public int TotalScore { get; private set; }   // suma de todas las rondas de la partida
    public float TimeLeft { get; private set; }
    public bool IsGameOver { get; private set; }   // true durante la pausa entre rondas
    public int Round { get; private set; } = 1;

    private float breakTimeLeft;
    private float baseSpawnsPerSecond;
    private bool UseWaves => waveManager != null && waveManager.Count > 0;
    private bool inWaveBreak;
    private float roundDuration;

    private void Awake()
    {
        Instance = this;

        // AudioSource en 2D: se sent igual estigui on estigui el jugador
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void Start()
    {
        if (timerSlider != null)
        {
            timerSlider.minValue = 0f;
            timerSlider.maxValue = 1f;
        }

        if (cubeGenerator != null) baseSpawnsPerSecond = cubeGenerator.SpawnsPerSecond;

        TotalScore = 0;
        GameSpeed.Reset();
        if (UseWaves) StartWave(0);
        else StartRound(1);
    }

    private void StartWave(int index)
    {
        Round = index + 1;
        inWaveBreak = false;
        IsGameOver = false;
        roundDuration = waveManager.Get(index).duration;
        TimeLeft = roundDuration;

        waveManager.ApplyWave(index);
        if (cubeGenerator != null) cubeGenerator.StartSpawning();

        if (uiScoreCurrent != null) uiScoreCurrent.SetActive(true);
        if (uiTimer != null) uiTimer.SetActive(true);
        if (uiScoreFinal != null) uiScoreFinal.SetActive(false);
        if (textNextRound != null) textNextRound.text = waveManager.Get(index).name;
        ShowRoundCleared(false);

        UpdateScoreUI();
        UpdateTimerUI();
    }

    private void UpdateWaves()
    {
        if (IsGameOver) return;

        if (inWaveBreak)
        {
            breakTimeLeft -= Time.deltaTime;
            SetBreakText($"{waveManager.Get(Round).name} en {Mathf.CeilToInt(Mathf.Max(0f, breakTimeLeft))}");
            if (breakTimeLeft <= 0f) StartWave(Round);
            return;
        }

        TimeLeft -= Time.deltaTime;
        UpdateTimerUI();
        if (TimeLeft > 0f) return;

        TimeLeft = 0f;
        UpdateTimerUI();
        if (cubeGenerator != null) cubeGenerator.StopSpawning();

        if (Round >= waveManager.Count)
            EndGame();
        else
        {
            inWaveBreak = true;
            breakTimeLeft = waveBreakDuration;
            ShowRoundCleared(true);
        }
    }

    private void ShowRoundCleared(bool show)
    {
        if (textRoundCleared == null) return;
        textRoundCleared.gameObject.SetActive(show);
        if (show) textRoundCleared.text = roundClearedMessage;
    }

    private void SetBreakText(string countdown)
    {
        if (textNextRound == null) return;
        textNextRound.text = textRoundCleared != null ? countdown : roundClearedMessage + "\n" + countdown;
    }

    // Empieza una ronda: reinicia el tiempo, aplica la dificultad y vuelve a lanzar cubos
    private void StartRound(int round)
    {
        roundDuration = gameDuration;
        Round = round;
        TimeLeft = gameDuration;
        IsGameOver = false;
        Score = 0;

        if (cubeGenerator != null)
        {
            int extra = Round - 1;
            cubeGenerator.speedMultiplier = 1f + speedIncreasePerRound * extra;
            cubeGenerator.SpawnsPerSecond = baseSpawnsPerSecond * (1f + spawnRateIncreasePerRound * extra);
            cubeGenerator.StartSpawning();
        }

        if (uiScoreCurrent != null) uiScoreCurrent.SetActive(true);
        if (uiTimer != null) uiTimer.SetActive(true);
        if (uiScoreFinal != null) uiScoreFinal.SetActive(false);
        if (textNextRound != null) textNextRound.text = "";
        ShowRoundCleared(false);

        UpdateScoreUI();
        UpdateTimerUI();
    }

    private void Update()
    {
        if (UseWaves) { UpdateWaves(); return; }

        if (IsGameOver)
        {
            // Pausa entre rondas: no salen cubos durante breakDuration segundos
            breakTimeLeft -= Time.deltaTime;
            SetBreakText($"Siguiente ronda en {Mathf.CeilToInt(Mathf.Max(0f, breakTimeLeft))}");

            if (breakTimeLeft <= 0f)
                StartRound(Round + 1);
            return;
        }

        TimeLeft -= Time.deltaTime;
        if (TimeLeft <= 0f)
        {
            TimeLeft = 0f;
            UpdateTimerUI();
            EndGame();
            return;
        }

        UpdateTimerUI();
    }

    // ---- Llamadas desde los cubos ----
    public enum CubeType { Gun, Sword }

    // Un cub ha estat destruït pel jugador: suma punts i fa sonar el seu clip
    public void CubeDestroyed(CubeType type)
    {
        AddHit();
        PlayClip(type == CubeType.Gun ? gunCubeClip : swordCubeClip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[ScoreManager] Falta assignar el clip a l'Inspector", this);
            return;
        }
        audioSource.PlayOneShot(clip, cubeSoundVolume);
        Debug.Log("[ScoreManager] So: " + clip.name);
    }

    public void AddHit() => AddPoints(pointsPerHit);
    public void AddMiss() => AddPoints(pointsPerMiss);

    public void AddPoints(int amount)
    {
        if (IsGameOver) return;
        Score = Mathf.Max(0, Score + amount);
        UpdateScoreUI();
    }

    // ---- UI ----
    private void UpdateScoreUI()
    {
        if (textCurrentScore != null) textCurrentScore.text = Score.ToString();
    }

    private void UpdateTimerUI()
    {
        float t = Mathf.Max(0f, TimeLeft);
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.CeilToInt(t % 60f);
        if (seconds == 60) { minutes++; seconds = 0; }

        if (textTime != null) textTime.text = $"{minutes:00}:{seconds:00}";

        float progress = roundDuration > 0f ? t / roundDuration : 0f;
        if (timerFillImage != null) timerFillImage.fillAmount = progress;
        if (timerSlider != null) timerSlider.value = progress;
    }

    // ---- Fin de partida ----
    // Se acaba el tiempo de la ronda: panel final + pausa antes de la siguiente
    private void EndGame()
    {
        IsGameOver = true;
        breakTimeLeft = breakDuration;
        if (!UseWaves) ShowRoundCleared(true);

        if (cubeGenerator != null) cubeGenerator.StopSpawning();

        // El score de la ronda se suma al total, y el high score se compara con el total
        TotalScore += Score;

        int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        if (TotalScore > highScore)
        {
            highScore = TotalScore;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        // Final score = total acumulado de todas las rondas
        if (textFinalScore != null) textFinalScore.text = TotalScore.ToString();
        if (textHighScore != null) textHighScore.text = highScore.ToString();

        if (uiScoreCurrent != null) uiScoreCurrent.SetActive(false);
        if (uiTimer != null) uiTimer.SetActive(false);
        if (uiScoreFinal != null) uiScoreFinal.SetActive(true);
    }

    // Para el botón "Back to Lobby" del panel final (OnClick / OnPressed)
    public void BackToLobby()
    {
        SceneManager.LoadScene(lobbySceneName);
    }

    public static int GetHighScore() => PlayerPrefs.GetInt(HighScoreKey, 0);
}
