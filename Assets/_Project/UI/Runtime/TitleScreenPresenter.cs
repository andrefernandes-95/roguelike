using AF.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace AF.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class TitleScreenPresenter : MonoBehaviour
    {
        RunCoordinator runCoordinator;

        UIDocument document;
        VisualElement root;
        Button newRunButton;
        Button quitButton;

        void Awake()
        {
            document = GetComponent<UIDocument>();

            if (runCoordinator == null)
            {
                runCoordinator = RunCoordinator.Instance;
            }
        }

        void OnEnable()
        {
            root = document.rootVisualElement.Q<VisualElement>("TitleScreenRoot");
            newRunButton = root.Q<Button>("NewRunButton");
            quitButton = root.Q<Button>("QuitButton");

            newRunButton.clicked += OnNewRunClicked;
            quitButton.clicked += OnQuitClicked;
            newRunButton.Focus();
        }

        void OnDisable()
        {
            if (newRunButton != null)
            {
                newRunButton.clicked -= OnNewRunClicked;
            }

            if (quitButton != null)
            {
                quitButton.clicked -= OnQuitClicked;
            }
        }

        void OnNewRunClicked()
        {
            if (runCoordinator != null)
            {
                runCoordinator.NewRun();
            }

            if (root != null)
            {
                root.style.display = DisplayStyle.None;
            }
        }

        void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        }
    }
}
