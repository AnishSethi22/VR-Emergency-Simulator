using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyRescue.Core
{
    public enum PlayModeType
    {
        Desktop = 0,
        VR = 1
    }

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Victory,
        GameOver
    }

    /// <summary>
    /// Central singleton managing game state, play mode selection (VR vs Desktop),
    /// player comfort/control settings, and scene transitions.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public const string MainMenuSceneName = "MainMenu";
        public const string GameplaySceneName = "RescueBuilding";

        [Header("Mode & State")]
        [SerializeField] private PlayModeType selectedMode = PlayModeType.Desktop;
        [SerializeField] private GameState currentState = GameState.MainMenu;

        [Header("Player Comfort & Controls Settings")]
        [Range(2f, 8f)] public float moveSpeed = 4.2f;
        [Range(0.2f, 5f)] public float mouseSensitivity = 1.8f;
        public bool useSnapTurn = true;
        [Range(15f, 90f)] public float snapTurnAngle = 45f;
        [Range(0f, 1f)] public float masterVolume = 0.85f;

        public PlayModeType SelectedMode => selectedMode;
        public GameState CurrentState => currentState;

        public event Action<GameState> OnGameStateChanged;
        public event Action<PlayModeType> OnPlayModeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();

            string activeScene = SceneManager.GetActiveScene().name;
            if (activeScene == GameplaySceneName)
            {
                SetGameState(GameState.Playing);
            }
            else
            {
                SetGameState(GameState.MainMenu);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == GameplaySceneName)
            {
                SetGameState(GameState.Playing);
            }
            else if (scene.name == MainMenuSceneName)
            {
                SetGameState(GameState.MainMenu);
            }
        }

        public void SetPlayMode(PlayModeType mode)
        {
            selectedMode = mode;
            PlayerPrefs.SetInt("ERS_PlayMode", (int)mode);
            PlayerPrefs.Save();
            OnPlayModeChanged?.Invoke(mode);
        }

        public void SetGameState(GameState newState)
        {
            currentState = newState;
            Time.timeScale = (newState == GameState.Paused) ? 0f : 1f;
            UpdateCursorState(newState);
            OnGameStateChanged?.Invoke(newState);
        }

        public void UpdateCursorState(GameState state)
        {
            if (selectedMode == PlayModeType.VR)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            bool lockCursor = (state == GameState.Playing);
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;
        }

        public void StartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameplaySceneName);
        }

        public void StartGameInMode(PlayModeType mode)
        {
            SetPlayMode(mode);
            StartGame();
        }

        public void TogglePause()
        {
            if (currentState == GameState.Playing)
            {
                SetGameState(GameState.Paused);
            }
            else if (currentState == GameState.Paused)
            {
                SetGameState(GameState.Playing);
            }
        }

        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                SetGameState(GameState.Playing);
            }
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameplaySceneName);
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SetGameState(GameState.MainMenu);
            SceneManager.LoadScene(MainMenuSceneName);
        }

        public void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetInt("ERS_PlayMode", (int)selectedMode);
            PlayerPrefs.SetFloat("ERS_MoveSpeed", moveSpeed);
            PlayerPrefs.SetFloat("ERS_MouseSens", mouseSensitivity);
            PlayerPrefs.SetInt("ERS_SnapTurn", useSnapTurn ? 1 : 0);
            PlayerPrefs.SetFloat("ERS_SnapAngle", snapTurnAngle);
            PlayerPrefs.SetFloat("ERS_Volume", masterVolume);
            PlayerPrefs.Save();
            AudioListener.volume = masterVolume;
        }

        public void LoadSettings()
        {
            selectedMode = (PlayModeType)PlayerPrefs.GetInt("ERS_PlayMode", (int)PlayModeType.Desktop);
            moveSpeed = PlayerPrefs.GetFloat("ERS_MoveSpeed", 4.2f);
            mouseSensitivity = PlayerPrefs.GetFloat("ERS_MouseSens", 1.8f);
            useSnapTurn = PlayerPrefs.GetInt("ERS_SnapTurn", 1) == 1;
            snapTurnAngle = PlayerPrefs.GetFloat("ERS_SnapAngle", 45f);
            masterVolume = PlayerPrefs.GetFloat("ERS_Volume", 0.85f);
            AudioListener.volume = masterVolume;
        }
    }
}
