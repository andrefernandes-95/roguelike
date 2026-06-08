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
        [SerializeField] string mainMenuScene = "";
        [SerializeField] string dungeonScene = "";

        readonly RunStateMachine stateMachine = new();
        readonly RunSession session = new();

        public RunState State => stateMachine.State;
        public RunSession Session => session;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            stateMachine.StateEntered += OnStateEntered;
        }

        void Oestroy()
        {
            stateMachine.StateEntered -= OnStateEntered;
        }

        void Start()
        {
            stateMachine.GoTo(RunState.MainMenu);
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