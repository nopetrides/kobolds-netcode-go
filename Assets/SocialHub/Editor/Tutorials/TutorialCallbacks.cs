using System;
using System.IO;
using Unity.Multiplayer.Tools.Editor.MultiplayerToolsWindow;
using Unity.Plastic.Newtonsoft.Json.Linq;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEditor;
using Unity.Tutorials.Core.Editor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.Editor.Tutorials
{
    /// <summary>
    /// Implement your Tutorial callbacks here.
    /// </summary>
    [CreateAssetMenu(fileName = KDefaultFileName, menuName = "Tutorials/" + KDefaultFileName + " Instance")]
    public class TutorialCallbacks : ScriptableObject
    {
        [SerializeField] SceneAsset m_BootstrapScene;

        /// <summary>
        /// The default file name used to create asset of this class type.
        /// </summary>
        const string KDefaultFileName = "TutorialCallbacks";

        const string KSystemDataPath = "../Library/VP/SystemData.json";

        bool _mIsEditorWindowFocused;

        const float KQuerySessionsInterval = 5f;

        bool _mIsSessionCreatedByVirtualPlayer;

        bool _mIsSessionJoinedByEditor;

        float _mTimeSinceLastSessionUpdate;

        ISession _mJoinedSession;

        /// <summary>
        /// Creates a TutorialCallbacks asset and shows it in the Project window.
        /// </summary>
        /// <param name="assetPath">
        /// A relative path to the project's root. If not provided, the Project window's currently active folder path is used.
        /// </param>
        /// <returns>The created asset</returns>
        public static ScriptableObject CreateAndShowAsset(string assetPath = null)
        {
            assetPath = assetPath ?? $"{TutorialEditorUtils.GetActiveFolderPath()}/{KDefaultFileName}.asset";
            var asset = CreateInstance<TutorialCallbacks>();
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(assetPath));
            EditorUtility.FocusProjectWindow(); // needed in order to make the selection of newly created asset to really work
            Selection.activeObject = asset;
            return asset;
        }

        public void StartTutorial(Tutorial tutorial)
        {
            TutorialWindow.StartTutorial(tutorial);
        }

        public void OpenURL(string url)
        {
            TutorialEditorUtils.OpenUrl(url);
        }

        public void LoadBootstrapScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(m_BootstrapScene));
        }

        public bool IsConnectedToUgs()
        {
            return CloudProjectSettings.projectBound;
        }

        public void ShowServicesSettings()
        {
            SettingsService.OpenProjectSettings("Project/Services");
        }

        [ContextMenu("Show Vivox Settings")]
        public void ShowVivoxSettings()
        {
            SettingsService.OpenProjectSettings("Project/Services/Vivox");
        }

        public bool IsVirtualPlayerCreated()
        {
            var path = Path.Combine(Application.dataPath, KSystemDataPath);

            if (File.Exists(path))
            {
                string jsonContent = File.ReadAllText(path);

                // Parse the JSON content using JObject
                var jsonObject = JObject.Parse(jsonContent);

                // Access the "Data" property and then the "2" player's "Active" state
                var isPlayer2Active = jsonObject["Data"]["2"]["Active"].Value<bool>();

                return isPlayer2Active;
            }

            return false;
        }

        public void OnOpenMultiplayerToolsWindowTutorialStarted()
        {
            MultiplayerToolsWindow.Open();
            _mIsEditorWindowFocused = false;
        }

        public bool IsSceneViewFocused()
        {
            if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.titleContent.text == "Scene")
            {
                _mIsEditorWindowFocused = true;
            }

            return _mIsEditorWindowFocused;
        }

        VisualElement _mSceneRoot;

        public void OnEnableNetSceneVisTutorialStarted()
        {
            _mSceneRoot = EditorWindow.GetWindow<SceneView>().rootVisualElement;
            while (_mSceneRoot.parent != null)
            {
                _mSceneRoot = _mSceneRoot.parent;
            }
        }

        public bool IsNetworkVisualizationOverlayDisplayed()
        {
            return _mSceneRoot != null && _mSceneRoot.Q<VisualElement>("NetVisToolbarOverlay") != null;
        }

        public void ForceNetworkVisualizationOverlayDisplayed()
        {
            if (_mSceneRoot.Q<VisualElement>("NetVisToolbarOverlay") == null)
            {
                var netSceneVis = _mSceneRoot.Q<VisualElement>("Network Visualization");
                var netSceneVisButton = netSceneVis.Q<Button>();
                using (var e = new NavigationSubmitEvent())
                {
                    e.target = netSceneVisButton;
                    netSceneVisButton.SendEvent(e);
                }
            }
        }

        public bool IsVirtualPlayerSessionCreated()
        {
            return _mIsSessionCreatedByVirtualPlayer;
        }

        public void OnCreatingSessionTutorialStarted()
        {
            _mIsSessionCreatedByVirtualPlayer = false;
            _mTimeSinceLastSessionUpdate = Time.realtimeSinceStartup;
        }

        public void QuerySessions()
        {
            if (UnityServices.Instance == null || AuthenticationService.Instance == null || !AuthenticationService.Instance.IsAuthorized)
            {
                return;
            }

            if (Time.realtimeSinceStartup - _mTimeSinceLastSessionUpdate > KQuerySessionsInterval)
            {
                _mTimeSinceLastSessionUpdate = Time.realtimeSinceStartup;
                QuerySessionsAsync();
            }
        }

        async void QuerySessionsAsync()
        {
            var task = MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());
            await task;
            if (task.IsCompleted)
            {
                // todo: add more criteria here
                _mIsSessionCreatedByVirtualPlayer = task.Result.Sessions.Count > 0;
            }
        }

        public bool IsSessionJoinedByEditor()
        {
            return _mIsSessionJoinedByEditor;
        }

        public void OnJoiningSessionTutorialStarted()
        {
            _mIsSessionJoinedByEditor = false;
            _mTimeSinceLastSessionUpdate = Time.realtimeSinceStartup;
        }

        public void QueryJoinedSessions()
        {
            if (UnityServices.Instance == null || AuthenticationService.Instance == null || !AuthenticationService.Instance.IsAuthorized)
            {
                return;
            }

            if (Time.realtimeSinceStartup - _mTimeSinceLastSessionUpdate > KQuerySessionsInterval)
            {
                _mTimeSinceLastSessionUpdate = Time.realtimeSinceStartup;
                QueryJoinedSessionsAsync();
            }
        }

        async void QueryJoinedSessionsAsync()
        {
            var task = MultiplayerService.Instance.GetJoinedSessionIdsAsync();
            await task;
            if (task.IsCompleted)
            {
                if (task.Result.Count > 0)
                {
                    var joinedSessionId = task.Result[0];
                    _mIsSessionJoinedByEditor = true;
                    foreach (var session in MultiplayerService.Instance.Sessions)
                    {
                        if (session.Value.Id == joinedSessionId)
                        {
                            _mJoinedSession = session.Value;
                            break;
                        }
                    }
                }
            }
        }

        public bool IsOnlyEditorInSession()
        {
            return _mJoinedSession != null && _mJoinedSession.Players.Count == 1 && _mJoinedSession.IsHost;
        }
    }
}
