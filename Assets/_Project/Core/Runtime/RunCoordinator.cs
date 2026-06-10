using UnityEngine;
using UnityEngine.SceneManagement;

namespace AF.Core
{
    /// <summary>
    /// Unity glue for the run lifecycle.
    /// Put on a DontDestroyOnLoad object in the boot scene
    /// </summary>
    public sealed class RunCoordinator : MonoBehaviour
    {
        public static RunCoordinator Instance { get; private set; }

        [SerializeField] string mainMenuScene = "";
        [SerializeField] string dungeonScene = "";

        readonly RunStateMachine stateMachine = new();
        readonly RunSession session = new();

        public RunState State => stateMachine.State;
        public RunSession Session => session;

        [SerializeField] bool autoPlay = false;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            stateMachine.StateEntered += OnStateEntered;

            if (autoPlay)
            {
                stateMachine.GoTo(RunState.FloorActive);
            }
        }

        void OnDestroy()
        {
            stateMachine.StateEntered -= OnStateEntered;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Start()
        {
            if (stateMachine.State == RunState.Boot)
            {
                stateMachine.GoTo(RunState.MainMenu);
            }
        }

        public void NewRun()
        {
            session.Begin(Random.Range(1, int.MaxValue));
            stateMachine.GoTo(RunState.RunStarting);
            stateMachine.GoTo(RunState.FloorActive);
        }

        public void NotifyFloorCleared()
        {
            stateMachine.GoTo(RunState.FloorCleared);
        }

        public void NotifyPlayerDied()
        {
            stateMachine.GoTo(RunState.PlayerDead);
        }

        public void FinishRun()
        {
            stateMachine.GoTo(RunState.RunEnded);
            ReturnToMenu();
        }

        public void ReturnToMenu()
        {
            stateMachine.GoTo(RunState.MainMenu);
        }

        void OnStateEntered(RunState state)
        {
            switch (state)
            {
                case RunState.MainMenu:
                    LoadSceneIfSet(mainMenuScene);
                    break;
                case RunState.FloorActive:
                    LoadSceneIfSet(dungeonScene);
                    break;
            }
        }

        void LoadSceneIfSet(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            if (SceneManager.GetActiveScene().name == sceneName)
            {
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
