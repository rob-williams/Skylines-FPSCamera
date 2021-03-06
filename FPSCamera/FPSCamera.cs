﻿using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace FPSCamera
{
    public class FPSCamera : MonoBehaviour
    {

        public delegate void OnCameraModeChanged(bool state);

        public static OnCameraModeChanged onCameraModeChanged;

        public delegate void OnUpdate();

        public static OnUpdate onUpdate;

        public static void Initialize()
        {
            var controller = GameObject.FindObjectOfType<CameraController>();
            instance = controller.gameObject.AddComponent<FPSCamera>();
            instance.controller = controller;
            instance.camera = controller.GetComponent<Camera>();
            instance.gameObject.AddComponent<GamePanelExtender>();
            instance.vehicleCamera = instance.gameObject.AddComponent<VehicleCamera>();
            instance.citizenCamera = instance.gameObject.AddComponent<CitizenCamera>();
        }

        public static void Deinitialize()
        {
            Destroy(instance);
        }

        public static FPSCamera instance;

        public static readonly string configPath = "FPSCameraConfig.xml";
        public Configuration config;

        private bool fpsModeEnabled = false;
        private CameraController controller;
        private Camera camera;
        float rotationY = 0f;

        private bool showUI = false;
        private Rect configWindowRect = new Rect(Screen.width - 400 - 128, 100, 400, 400);

        private bool waitingForChangeCameraHotkey = false;
        private bool waitingForShowMouseHotkey = false;
        private bool waitingForGoFasterHotkey = false;

        private Vector3 mainCameraPosition;
        private Quaternion mainCameraOrientation;

        private TerrainManager terrainManager;
        private NetManager netManager;

        private bool initPositions = false;

        private Texture2D bgTexture;
        private GUISkin skin;

        private SavedInputKey cameraMoveLeft;
        private SavedInputKey cameraMoveRight;
        private SavedInputKey cameraMoveForward;
        private SavedInputKey cameraMoveBackward;
        private SavedInputKey cameraZoomCloser;
        private SavedInputKey cameraZoomAway;

        public Component hideUIComponent = null;
        public bool checkedForHideUI = false;

        public VehicleCamera vehicleCamera;
        public CitizenCamera citizenCamera;
        
        public bool cityWalkthroughMode = false;
        private float cityWalkthroughNextChangeTimer = 0.0f;

        void Awake()
        {
            config = Configuration.Deserialize(configPath);
            if (config == null)
            {
                config = new Configuration();
            }

            SaveConfig();

            mainCameraPosition = gameObject.transform.position;
            mainCameraOrientation = gameObject.transform.rotation;

            terrainManager = Singleton<TerrainManager>.instance;
            netManager = Singleton<NetManager>.instance;

            bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, Color.grey);
            bgTexture.Apply();

            cameraMoveLeft = new SavedInputKey(Settings.cameraMoveLeft, Settings.gameSettingsFile, DefaultSettings.cameraMoveLeft, true);
            cameraMoveRight = new SavedInputKey(Settings.cameraMoveRight, Settings.gameSettingsFile, DefaultSettings.cameraMoveRight, true);
            cameraMoveForward = new SavedInputKey(Settings.cameraMoveForward, Settings.gameSettingsFile, DefaultSettings.cameraMoveForward, true);
            cameraMoveBackward = new SavedInputKey(Settings.cameraMoveBackward, Settings.gameSettingsFile, DefaultSettings.cameraMoveBackward, true);
            cameraZoomCloser = new SavedInputKey(Settings.cameraZoomCloser, Settings.gameSettingsFile, DefaultSettings.cameraZoomCloser, true);
            cameraZoomAway = new SavedInputKey(Settings.cameraZoomAway, Settings.gameSettingsFile, DefaultSettings.cameraZoomAway, true);
        }

        void SaveConfig()
        {
            Configuration.Serialize(configPath, config);
        }

        void OnGUI()
        {
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<GUISkin>();
                skin.box = new GUIStyle(GUI.skin.box);
                skin.button = new GUIStyle(GUI.skin.button);
                skin.horizontalScrollbar = new GUIStyle(GUI.skin.horizontalScrollbar);
                skin.horizontalScrollbarLeftButton = new GUIStyle(GUI.skin.horizontalScrollbarLeftButton);
                skin.horizontalScrollbarRightButton = new GUIStyle(GUI.skin.horizontalScrollbarRightButton);
                skin.horizontalScrollbarThumb = new GUIStyle(GUI.skin.horizontalScrollbarThumb);
                skin.horizontalSlider = new GUIStyle(GUI.skin.horizontalSlider);
                skin.horizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb);
                skin.label = new GUIStyle(GUI.skin.label);
                skin.scrollView = new GUIStyle(GUI.skin.scrollView);
                skin.textArea = new GUIStyle(GUI.skin.textArea);
                skin.textField = new GUIStyle(GUI.skin.textField);
                skin.toggle = new GUIStyle(GUI.skin.toggle);
                skin.verticalScrollbar = new GUIStyle(GUI.skin.verticalScrollbar);
                skin.verticalScrollbarDownButton = new GUIStyle(GUI.skin.verticalScrollbarDownButton);
                skin.verticalScrollbarThumb = new GUIStyle(GUI.skin.verticalScrollbarThumb);
                skin.verticalScrollbarUpButton = new GUIStyle(GUI.skin.verticalScrollbarUpButton);
                skin.verticalSlider = new GUIStyle(GUI.skin.verticalSlider);
                skin.verticalSliderThumb = new GUIStyle(GUI.skin.verticalSliderThumb);
                skin.window = new GUIStyle(GUI.skin.window);
                skin.window.normal.background = bgTexture;
                skin.window.onNormal.background = bgTexture;
            }

            if (showUI)
            {
                var oldSkin = GUI.skin;
                GUI.skin = skin;
                configWindowRect = GUI.Window(21521, configWindowRect, DoConfigWindow, "FPS Camera configuration");
                GUI.skin = oldSkin;
            }
        }

        void DoConfigWindow(int wnd)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hotkey to toggle first-person:");
            GUILayout.FlexibleSpace();

            string label = config.toggleFPSCameraHotkey.ToString();
            if (waitingForChangeCameraHotkey)
            {
                label = "Waiting";

                if (Event.current.type == EventType.KeyDown)
                {
                    waitingForChangeCameraHotkey = false;
                    config.toggleFPSCameraHotkey = Event.current.keyCode;
                }
            }

            if (GUILayout.Button(label, GUILayout.Width(128)))
            {
                if (!waitingForChangeCameraHotkey)
                {
                    waitingForChangeCameraHotkey = true;
                    waitingForShowMouseHotkey = false;
                    waitingForGoFasterHotkey = false;
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hotkey to show cursor (hold):");
            GUILayout.FlexibleSpace();
            label = config.showCodeFPSMouseHotkey.ToString();
            if (waitingForShowMouseHotkey)
            {
                label = "Waiting";

                if (Event.current.type == EventType.KeyDown)
                {
                    waitingForShowMouseHotkey = false;
                    config.showCodeFPSMouseHotkey = Event.current.keyCode;
                }
            }

            if (GUILayout.Button(label, GUILayout.Width(128)))
            {
                if (!waitingForShowMouseHotkey)
                {
                    waitingForShowMouseHotkey = true;
                    waitingForChangeCameraHotkey = false;
                    waitingForGoFasterHotkey = false;
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hotkey to go faster (hold):");
            GUILayout.FlexibleSpace();
            label = config.goFasterHotKey.ToString();
            if (waitingForGoFasterHotkey)
            {
                label = "Waiting";

                if (Event.current.type == EventType.KeyDown)
                {
                    waitingForGoFasterHotkey = false;
                    config.goFasterHotKey = Event.current.keyCode;
                }
            }

            if (GUILayout.Button(label, GUILayout.Width(128)))
            {
                if (!waitingForGoFasterHotkey)
                {
                    waitingForGoFasterHotkey = true;
                    waitingForChangeCameraHotkey = false;
                    waitingForShowMouseHotkey = false;
                }
            }

            GUILayout.EndHorizontal();

            if (hideUIComponent != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("HideUI integration: ");
                config.integrateHideUI = GUILayout.Toggle(config.integrateHideUI, "");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Field of view: ");
            config.fieldOfView = GUILayout.HorizontalSlider(config.fieldOfView, 30.0f, 120.0f, GUILayout.Width(200));
            Camera.main.fieldOfView = config.fieldOfView;
            GUILayout.Label(config.fieldOfView.ToString("0.00"));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Movement speed: ");
            config.cameraMoveSpeed = GUILayout.HorizontalSlider(config.cameraMoveSpeed, 1.0f, 128.0f, GUILayout.Width(200));
            GUILayout.Label(config.cameraMoveSpeed.ToString("0.00"));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Sensitivity: ");
            config.cameraRotationSensitivity = GUILayout.HorizontalSlider(config.cameraRotationSensitivity, 0.1f, 2.0f, GUILayout.Width(200));
            GUILayout.Label(config.cameraRotationSensitivity.ToString("0.00"));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Invert Y-axis: ");
            config.invertYAxis = GUILayout.Toggle(config.invertYAxis, "");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Snap to ground: ");
            config.snapToGround = GUILayout.Toggle(config.snapToGround, "");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Offset from ground: ");
            config.groundOffset = GUILayout.HorizontalSlider(config.groundOffset, 0.25f, 32.0f, GUILayout.Width(200));
            GUILayout.Label(config.groundOffset.ToString("0.00"));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Prevent ground clipping: ");
            config.preventClipGround = GUILayout.Toggle(config.preventClipGround, "");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Animated transitions: ");
            config.animateTransitions = GUILayout.Toggle(config.animateTransitions, "");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Animation speed: ");
            config.animationSpeed = GUILayout.HorizontalSlider(config.animationSpeed, 0.1f, 4.0f, GUILayout.Width(200));
            GUILayout.Label(config.animationSpeed.ToString("0.00"));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if(GUILayout.Button("Save configuration"))
            {
                SaveConfig();
            }

            if (GUILayout.Button("Reset configuration"))
            {
                config = new Configuration();
                SaveConfig();
            }

            if (GUILayout.Button("City walkthrough mode"))
            {
                cityWalkthroughMode = true;
                cityWalkthroughNextChangeTimer = 0.0f;

                if (hideUIComponent != null && config.integrateHideUI)
                {
                    hideUIComponent.SendMessage("Hide");
                }

                showUI = false;
            }
        }

        private bool inModeTransition = false;
        private Vector3 transitionTargetPosition = Vector3.zero;
        private Quaternion transitionTargetOrientation = Quaternion.identity;
        private Vector3 transitionStartPosition = Vector3.zero;
        private Quaternion transitionStartOrientation = Quaternion.identity;
        private float transitionT = 0.0f;

        public void SetMode(bool fpsMode)
        {
            instance.fpsModeEnabled = fpsMode;

            if (instance.fpsModeEnabled)
            {
                instance.controller.enabled = false;
                Cursor.visible = false;
                instance.rotationY = -instance.transform.localEulerAngles.x;
            }
            else
            {
                if (!config.animateTransitions)
                {
                    instance.controller.enabled = true;
                }
                
                Cursor.visible = true;
            }

            if (hideUIComponent != null && config.integrateHideUI)
            {
                if (instance.fpsModeEnabled)
                {
                    hideUIComponent.SendMessage("Hide");
                }
                else
                {
                    hideUIComponent.SendMessage("Show");
                }
            }

            if (onCameraModeChanged != null)
            {
                onCameraModeChanged(fpsMode);
            }
        }

        public static void ToggleUI()
        {
            instance.showUI = !instance.showUI;
        }

        public static KeyCode GetToggleUIKey()
        {
            return instance.config.toggleFPSCameraHotkey;
        }

        public static bool IsEnabled()
        {
            return instance.fpsModeEnabled;
        }

        public ushort GetRandomVehicle()
        {
            var vmanager = VehicleManager.instance;
            int skip = Random.Range(0, vmanager.m_vehicleCount - 1);

            for (ushort i = 0; i < vmanager.m_vehicles.m_buffer.Length; i++)
            {
                if ((vmanager.m_vehicles.m_buffer[i].m_flags & (Vehicle.Flags.Created | Vehicle.Flags.Deleted)) != Vehicle.Flags.Created)
                {
                    continue;
                }

                if (vmanager.m_vehicles.m_buffer[i].Info.m_vehicleAI is CarTrailerAI)
                {
                    continue;
                }

                if(skip > 0)
                {
                    skip--;
                    continue;
                }

                return i;
            }

            for (ushort i = 0; i < vmanager.m_vehicles.m_buffer.Length; i++)
            {
                if ((vmanager.m_vehicles.m_buffer[i].m_flags & (Vehicle.Flags.Created | Vehicle.Flags.Deleted)) !=
                    Vehicle.Flags.Created)
                {
                    continue;
                }

                if (vmanager.m_vehicles.m_buffer[i].Info.m_vehicleAI is CarTrailerAI)
                {
                    continue;
                }

                return i;
            }

            return 0;
        }
        
        public uint GetRandomCitizenInstance()
        {
            var cmanager = CitizenManager.instance;
            int skip = Random.Range(0, cmanager.m_instanceCount - 1);

            for (uint i = 0; i < cmanager.m_instances.m_buffer.Length; i++)
            {
                if ((cmanager.m_instances.m_buffer[i].m_flags & (CitizenInstance.Flags.Created | CitizenInstance.Flags.Deleted)) != CitizenInstance.Flags.Created)
                {
                    continue;
                }

                if (skip > 0)
                {
                    skip--;
                    continue;
                }

                return cmanager.m_instances.m_buffer[i].m_citizen;
            }

            for (uint i = 0; i < cmanager.m_instances.m_buffer.Length; i++)
            {
                if ((cmanager.m_instances.m_buffer[i].m_flags & (CitizenInstance.Flags.Created | CitizenInstance.Flags.Deleted)) != CitizenInstance.Flags.Created)
                {
                    continue;
                }

                return cmanager.m_instances.m_buffer[i].m_citizen;
            }

            return 0;
        }

        void Update()
        {
            if (onUpdate != null)
            {
                onUpdate();
            }

            if (!initPositions)
            {
                mainCameraPosition = gameObject.transform.position;
                mainCameraOrientation = gameObject.transform.rotation;
                rotationY = -instance.transform.localEulerAngles.x;
                initPositions = true;
            }

            if (!checkedForHideUI)
            {
                var gameObjects = FindObjectsOfType<GameObject>();
                foreach (var go in gameObjects)
                {
                    var tmp = go.GetComponent("HideUI");
                    if (tmp != null)
                    {
                        hideUIComponent = tmp;
                        break;
                    }
                }

                checkedForHideUI = true;
            }

            if (cityWalkthroughMode)
            {
                cityWalkthroughNextChangeTimer -= Time.deltaTime;
                if (cityWalkthroughNextChangeTimer <= 0.0f || !(citizenCamera.following || vehicleCamera.following))
                {
                    cityWalkthroughNextChangeTimer = Random.Range(5.0f, 10.0f);
                    bool vehicleOrCitizen = Random.Range(0, 3) == 0;
                    if (!vehicleOrCitizen)
                    {
                        if (citizenCamera.following)
                        {
                            citizenCamera.StopFollowing();
                        }

                        var vehicle = GetRandomVehicle();
                        if (vehicle != 0)
                        {
                            vehicleCamera.SetFollowInstance(vehicle);
                        }
                    }
                    else
                    {
                        if (vehicleCamera.following)
                        {
                            vehicleCamera.StopFollowing();
                        }

                        var citizen = GetRandomCitizenInstance();
                        if (citizen != 0)
                        {
                            citizenCamera.SetFollowInstance(citizen); 
                        }
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (cityWalkthroughMode)
                {
                    cityWalkthroughMode = false;
                    if (vehicleCamera.following)
                    {
                        vehicleCamera.StopFollowing();
                    }
                    if (citizenCamera.following)
                    {
                        citizenCamera.StopFollowing();
                    }

                    if (hideUIComponent != null && config.integrateHideUI)
                    {
                        hideUIComponent.SendMessage("Show");
                    }
                }
                else if (vehicleCamera.following)
                {
                    vehicleCamera.StopFollowing();

                    if (hideUIComponent != null && config.integrateHideUI)
                    {
                        hideUIComponent.SendMessage("Show");
                    }
                }
                else if (citizenCamera.following)
                {
                    citizenCamera.StopFollowing();

                    if (hideUIComponent != null && config.integrateHideUI)
                    {
                        hideUIComponent.SendMessage("Show");
                    }
                }
                else if(fpsModeEnabled)
                {
                    if (config.animateTransitions && fpsModeEnabled)
                    {
                        inModeTransition = true;
                        transitionT = 0.0f;

                        if ((gameObject.transform.position - mainCameraPosition).magnitude <= 1.0f)
                        {
                            transitionT = 1.0f;
                            mainCameraOrientation = gameObject.transform.rotation;
                        }

                        transitionStartPosition = gameObject.transform.position;
                        transitionStartOrientation = gameObject.transform.rotation;

                        transitionTargetPosition = mainCameraPosition;
                        transitionTargetOrientation = mainCameraOrientation;
                    }

                    SetMode(!fpsModeEnabled);
                }
            }

            if (Input.GetKeyDown(config.toggleFPSCameraHotkey))
            {
                if (cityWalkthroughMode)
                {
                    cityWalkthroughMode = false;
                    if (vehicleCamera.following)
                    {
                        vehicleCamera.StopFollowing();
                    }
                    if (citizenCamera.following)
                    {
                        citizenCamera.StopFollowing();
                    }

                    if (hideUIComponent != null && config.integrateHideUI)
                    {
                        hideUIComponent.SendMessage("Show");
                    }
                }
                else if (vehicleCamera.following)
                {
                    vehicleCamera.StopFollowing();
                }
                else if (citizenCamera.following)
                {
                    citizenCamera.StopFollowing();
                }
                else
                {
                    if (config.animateTransitions && fpsModeEnabled)
                    {
                        inModeTransition = true;
                        transitionT = 0.0f;

                        if ((gameObject.transform.position - mainCameraPosition).magnitude <= 1.0f)
                        {
                            transitionT = 1.0f;
                            mainCameraOrientation = gameObject.transform.rotation;
                        }

                        transitionStartPosition = gameObject.transform.position;
                        transitionStartOrientation = gameObject.transform.rotation;

                        transitionTargetPosition = mainCameraPosition;
                        transitionTargetOrientation = mainCameraOrientation;
                    }

                    SetMode(!fpsModeEnabled);
                }
            }

            if (config.animateTransitions && inModeTransition)
            {
                transitionT += Time.deltaTime * config.animationSpeed;

                gameObject.transform.position = Vector3.Slerp(transitionStartPosition, transitionTargetPosition, transitionT);
                gameObject.transform.rotation = Quaternion.Slerp(transitionStartOrientation, transitionTargetOrientation, transitionT);

                if (transitionT >= 1.0f)
                {
                    inModeTransition = false;

                    if (!fpsModeEnabled)
                    {
                        instance.controller.enabled = true;
                    }
                }
            }
            else if (fpsModeEnabled)
            {
                var pos = gameObject.transform.position;
                float terrainY = terrainManager.SampleDetailHeight(gameObject.transform.position);
                float waterY = terrainManager.WaterLevel(new Vector2(gameObject.transform.position.x, gameObject.transform.position.z));
                terrainY = Mathf.Max(terrainY, waterY);

                if (config.snapToGround)
                {
                    Segment3 ray = new Segment3(gameObject.transform.position + new Vector3(0f, 1.5f, 0f), gameObject.transform.position + new Vector3(0f, -1000f, 0f));

                    Vector3 hitPos;
                    ushort nodeIndex;
                    ushort segmentIndex;
                    Vector3 hitPos2;

                    if (netManager.RayCast(ray, 0f, ItemClass.Service.Road, ItemClass.Service.PublicTransport, ItemClass.SubService.None, ItemClass.SubService.None, ItemClass.Layer.Default, ItemClass.Layer.None, NetNode.Flags.None, NetSegment.Flags.None, out hitPos, out nodeIndex, out segmentIndex)
                        | netManager.RayCast(ray, 0f, ItemClass.Service.Beautification, ItemClass.Service.Water, ItemClass.SubService.None, ItemClass.SubService.None, ItemClass.Layer.Default, ItemClass.Layer.None, NetNode.Flags.None, NetSegment.Flags.None, out hitPos2, out nodeIndex, out segmentIndex))
                    {
                        terrainY = Mathf.Max(terrainY, Mathf.Max(hitPos.y, hitPos2.y));
                    }

                    gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, new Vector3(pos.x, terrainY + config.groundOffset, pos.z), 0.9f);
                }

                float speedFactor = 1.0f;
                if (config.limitSpeedGround)
                {
                    speedFactor *= Mathf.Sqrt(terrainY);
                    speedFactor = Mathf.Clamp(speedFactor, 1.0f, 256.0f);
                }

                if (Input.GetKey(config.goFasterHotKey))
                {
                    speedFactor *= 10.0f;
                }

                if (cameraMoveForward.IsPressed())
                {
                    gameObject.transform.position += gameObject.transform.forward * config.cameraMoveSpeed * speedFactor * Time.deltaTime;
                }
                else if (cameraMoveBackward.IsPressed())
                {
                    gameObject.transform.position -= gameObject.transform.forward * config.cameraMoveSpeed * speedFactor * Time.deltaTime;
                }

                if (cameraMoveLeft.IsPressed())
                {
                    gameObject.transform.position -= gameObject.transform.right * config.cameraMoveSpeed * speedFactor * Time.deltaTime;
                }
                else if (cameraMoveRight.IsPressed())
                {
                    gameObject.transform.position += gameObject.transform.right * config.cameraMoveSpeed * speedFactor * Time.deltaTime;
                }

                if (cameraZoomAway.IsPressed())
                {
                    gameObject.transform.position -= gameObject.transform.up * config.cameraMoveSpeed * speedFactor * Time.deltaTime;
                }
                else if (cameraZoomCloser.IsPressed())
                {
                    gameObject.transform.position += gameObject.transform.up * config.cameraMoveSpeed * speedFactor * Time.deltaTime;
                }

                if (Input.GetKey(config.showCodeFPSMouseHotkey))
                {
                    Cursor.visible = true;
                }
                else
                {
                    float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * config.cameraRotationSensitivity;
                    rotationY += Input.GetAxis("Mouse Y") * config.cameraRotationSensitivity * (config.invertYAxis ? -1.0f : 1.0f);
                    transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
                    Cursor.visible = false;
                }

                if (config.preventClipGround)
                {
                    if (transform.position.y < terrainY + config.groundOffset)
                    {
                        transform.position = new Vector3(transform.position.x, terrainY + config.groundOffset, transform.position.z);
                    }
                }

                camera.fieldOfView = config.fieldOfView;
                camera.nearClipPlane = 1.0f;
            }
            else
            {
                mainCameraPosition = gameObject.transform.position;
                mainCameraOrientation = gameObject.transform.rotation;
            }
        }

    }

}
