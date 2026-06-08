# Title screen

## Goal

Minimal main menu: **New Run** calls `RunCoordinator.NewRun()`, **Quit** exits. UI Toolkit, keyboard + gamepad via focusable buttons and `tabindex` order.

**UXML already in project:** `Assets/_Project/UI/UXML/TitleScreen.uxml` (do not recreate).

**Asmdef:** `AF.UI` → references `AF.Core` only.

---

## Prerequisite fix

In `RunCoordinator.cs`, rename `Oestroy` → `OnDestroy` (typo — event subscription never unsubscribes otherwise).

---

## Files

```
Assets/_Project/UI/
├── UXML/
│   └── TitleScreen.uxml          ← already delivered
├── USS/
│   └── TitleScreen.uss
└── Runtime/
    ├── AF.UI.asmdef
    └── TitleScreenPresenter.cs
```

### `Assets/_Project/UI/USS/TitleScreen.uss`

```css
.title-screen {
    flex-grow: 1;
    align-items: center;
    justify-content: center;
    background-color: rgb(12, 12, 16);
}

.title-screen__heading {
    font-size: 48px;
    color: rgb(230, 220, 200);
    margin-bottom: 48px;
    -unity-text-align: middle-center;
}

.title-screen__menu {
    flex-direction: column;
    align-items: stretch;
    min-width: 280px;
}

.title-screen__button {
    height: 44px;
    margin-bottom: 12px;
    font-size: 18px;
    color: rgb(230, 220, 200);
    background-color: rgb(40, 38, 48);
    border-width: 2px;
    border-color: rgb(80, 72, 96);
    border-radius: 4px;
}

.title-screen__button:focus {
    border-color: rgb(200, 168, 72);
    background-color: rgb(56, 52, 64);
}
```

### `Assets/_Project/UI/Runtime/AF.UI.asmdef`

```json
{
    "name": "AF.UI",
    "rootNamespace": "AF.UI",
    "references": [
        "AF.Core"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### `Assets/_Project/UI/Runtime/TitleScreenPresenter.cs`

```csharp
using AF.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace AF.UI
{
    /// <summary>Binds TitleScreen.uxml to RunCoordinator. One job.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TitleScreenPresenter : MonoBehaviour
    {
        [SerializeField] RunCoordinator _runCoordinator;

        UIDocument _document;
        VisualElement _root;
        Button _newRunButton;
        Button _quitButton;

        void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            _root = _document.rootVisualElement.Q<VisualElement>("TitleScreenRoot");
            _newRunButton = _root.Q<Button>("NewRunButton");
            _quitButton = _root.Q<Button>("QuitButton");

            _newRunButton.clicked += OnNewRunClicked;
            _quitButton.clicked += OnQuitClicked;

            _newRunButton.Focus();
        }

        void OnDisable()
        {
            if (_newRunButton != null)
            {
                _newRunButton.clicked -= OnNewRunClicked;
            }

            if (_quitButton != null)
            {
                _quitButton.clicked -= OnQuitClicked;
            }
        }

        void OnNewRunClicked()
        {
            if (_runCoordinator == null)
            {
                Debug.LogError("TitleScreenPresenter: RunCoordinator not assigned.");
                return;
            }

            _runCoordinator.NewRun();

            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
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
```

---

## Unity setup

### 1. Panel Settings (once per project)

1. **Assets → Create → UI Toolkit → Panel Settings Asset** → name `GamePanelSettings`.
2. Save under `Assets/_Project/UI/`.
3. Defaults are fine for jam.

### 2. Gamepad / keyboard submit (once per scene)

1. If the scene has no **Event System**, create one: **GameObject → UI → Event System**.
2. Remove **Standalone Input Module** if present.
3. Add **Input System UI Input Module** (Component menu on Event System).
4. Leave default bindings — Submit/Cancel map to gamepad A/B and Enter/Escape.

UITK buttons use focus navigation: **arrow keys / D-pad** move focus, **Enter / A** activates.

### 3. Title screen GameObject

1. In boot scene (same scene as **Run** / `RunCoordinator`), create empty **TitleScreen**.
2. Add **UI Document**:
   - **Panel Settings** → `GamePanelSettings`
   - **Source Asset** → `TitleScreen.uxml`
3. Add **Title Screen Presenter** (`AF.UI.TitleScreenPresenter`).
4. Drag **Run** (`RunCoordinator`) into **Run Coordinator** field.

### 4. Scenes (optional)

- Leave `RunCoordinator` scene fields empty while testing in one scene.
- Later: set **Main Menu Scene** to boot scene name, **Dungeon Scene** to dungeon scene name in Build Settings.

### 5. Build Settings

When you add a dungeon scene: **File → Build Settings → Add Open Scenes** for both boot and dungeon.

---

## Navigation verify (keyboard + gamepad)

| Step | Keyboard | Gamepad |
|------|----------|---------|
| Focus on load | `New Run` focused (gold border) | same |
| Move down | ↓ or Tab → `Quit` | D-pad down |
| Move up | ↑ → `New Run` | D-pad up |
| Activate | Enter on `New Run` → run starts, menu hides | A button |
| Quit (editor) | Enter on `Quit` → play mode stops | A on `Quit` |

Focus order: `NewRunButton` (tabindex 1) → `QuitButton` (tabindex 2).

---

## Verify

- [ ] `Oestroy` fixed to `OnDestroy` on `RunCoordinator`
- [ ] `AF.UI.asmdef` compiles; references only `AF.Core`
- [ ] Play → title visible, **New Run** auto-focused
- [ ] **New Run** → `RunCoordinator.State` is `FloorActive`, `Session.Seed` ≠ 0
- [ ] Menu root hidden after New Run (single-scene test)
- [ ] **Quit** stops play mode in editor
- [ ] Arrow keys / D-pad swap focus between buttons without mouse

---

## Next delivery

`docs/code/player-graybox.md` — input actions, CharacterController move, basic follow camera.
