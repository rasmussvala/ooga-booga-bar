using System.IO;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Johans.RelayTest.Editor
{
    public static class RelayTestSceneBuilder
    {
        private const string Root = "Assets/Johans Mapp/RelayTest";
        private const string ScenePath = Root + "/Relay Test.unity";
        private static Font font;

        [MenuItem("Tools/Johans/Skapa Relay-testscen")]
        public static void Create()
        {
            if (File.Exists(ScenePath))
            {
                ValidatePrefab();
                Debug.Log("Relay-testscenen finns redan: " + ScenePath);
                return;
            }
            // Additive creation preserves the currently open scene and its unsaved work.
            var previousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Relay Test Player";
            player.AddComponent<NetworkObject>();
            player.AddComponent<NetworkTransform>().AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            player.AddComponent<RelayTestPlayer>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(player, Root + "/Relay Test Player.prefab");
            Object.DestroyImmediate(player);
            ValidatePrefab();

            var network = new GameObject("NetworkManager", typeof(UnityTransport), typeof(NetworkManager));
            var manager = network.GetComponent<NetworkManager>();
            manager.NetworkConfig.NetworkTransport = network.GetComponent<UnityTransport>();
            manager.NetworkConfig.PlayerPrefab = prefab;
            manager.NetworkConfig.EnableSceneManagement = false;
            manager.NetworkConfig.ConnectionApproval = false;
            manager.NetworkConfig.Prefabs = new NetworkPrefabs();
            var prefabs = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            prefabs.Add(new NetworkPrefab { Prefab = prefab });
            AssetDatabase.CreateAsset(prefabs, Root + "/Relay Test Network Prefabs.asset");
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabs);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Test Floor";
            floor.transform.position = new Vector3(0, -.25f, 0);
            floor.transform.localScale = new Vector3(18, .5f, 14);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 15, -12);
            camera.transform.LookAt(Vector3.zero);
            camera.GetComponent<Camera>().orthographic = true;
            camera.GetComponent<Camera>().orthographicSize = 12;
            var light = new GameObject("Directional Light", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var canvas = new GameObject("Relay UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            var panel = Rect("Host / Join", canvas.transform, 20, 20, 380, 325);
            panel.gameObject.AddComponent<Image>().color = new Color(.06f, .08f, .12f, .95f);
            Label(panel, "RELAY MULTIPLAYER TEST", 15, 10, 350, 28, 22);
            Label(panel, "Spelarnamn", 15, 45, 350, 22);
            var name = Input(panel, "Player", 15, 70);
            name.characterLimit = 24; // At most 96 UTF-8 bytes, fits FixedString128Bytes.
            Label(panel, "Joinkod (visas här när du hostar)", 15, 112, 350, 22);
            var code = Input(panel, "", 15, 137);
            code.characterLimit = 16;
            var menu = canvas.AddComponent<RelayTestMenu>();
            menu.Manager = manager;
            menu.PlayerNameInput = name;
            menu.JoinCodeInput = code;
            menu.HostButton = Button(panel, "Host", 15, 181, 80);
            menu.JoinButton = Button(panel, "Join", 105, 181, 80);
            menu.CopyButton = Button(panel, "Kopiera kod", 195, 181, 170);
            menu.LeaveButton = Button(panel, "Lämna / stoppa host", 15, 223, 350);
            menu.StatusText = Label(panel, "Redo", 15, 266, 350, 54, 14);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);
            if (previousScene.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousScene);
            AssetDatabase.SaveAssets();
            Debug.Log("Relay test scene created: " + ScenePath);
        }

        private static void ValidatePrefab()
        {
            const string path = Root + "/Relay Test Player.prefab";
            // Reimport lets NGO generate the asset's identity after its first save.
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            PrefabUtility.SavePrefabAsset(prefab);
            var serialized = new SerializedObject(prefab.GetComponent<NetworkObject>());
            if (serialized.FindProperty("GlobalObjectIdHash").uintValue == 0)
                throw new System.InvalidOperationException("Relay player prefab has no network identity.");
            Debug.Log("Relay player prefab network identity validated.");
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        private static Text Label(Transform parent, string value, float x, float y, float w, float h, int size = 16)
        {
            var text = Rect(value.Length > 0 ? value : "Text", parent, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.text = value;
            text.supportRichText = false;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static InputField Input(Transform parent, string initial, float x, float y)
        {
            var rect = Rect("Input", parent, x, y, 350, 34);
            rect.gameObject.AddComponent<Image>().color = new Color(.2f, .24f, .3f);
            var text = Label(rect, initial, 8, 5, 334, 24);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var input = rect.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.text = initial;
            return input;
        }

        private static Button Button(Transform parent, string value, float x, float y, float width)
        {
            var rect = Rect(value, parent, x, y, width, 34);
            rect.gameObject.AddComponent<Image>().color = new Color(.15f, .35f, .5f);
            var button = rect.gameObject.AddComponent<Button>();
            Label(rect, value, 0, 0, width, 34).alignment = TextAnchor.MiddleCenter;
            return button;
        }
    }
}
