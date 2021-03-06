﻿using ColossalFramework.UI;
using UnityEngine;

namespace FPSCamera
{
    public class GamePanelExtender : MonoBehaviour
    {

        private bool initialized = false;
        private CitizenVehicleWorldInfoPanel citizenVehicleInfoPanel;
        private UIButton citizenVehicleCameraButton;

        private CityServiceVehicleWorldInfoPanel cityServiceVehicleInfoPanel;
        private UIButton cityServiceVehicleCameraButton;

        private PublicTransportVehicleWorldInfoPanel publicTransportVehicleInfoPanel;
        private UIButton publicTransportCameraButton;

        private CitizenWorldInfoPanel citizenInfoPanel;
        private UIButton citizenCameraButton;

        private UIView uiView;

        private Vector3 cameraButtonOffset = new Vector3(-72.0f, 8.0f, 0.0f);
        private int cameraButtonSize = 24;

        void Awake()
        {
            uiView = FindObjectOfType<UIView>();
        }

        void OnDestroy()
        {
            Destroy(citizenVehicleCameraButton);
            Destroy(cityServiceVehicleCameraButton);
            Destroy(publicTransportCameraButton);
            Destroy(citizenCameraButton);
        }

        void Update()
        {
            if (!initialized)
            {
                citizenVehicleInfoPanel = GameObject.Find("(Library) CitizenVehicleWorldInfoPanel").GetComponent<CitizenVehicleWorldInfoPanel>();

                citizenVehicleInfoPanel.Find<UITextField>("VehicleName").width = 200;

                citizenVehicleCameraButton = CreateCameraButton
                (
                    citizenVehicleInfoPanel.component,
                    (component, param) =>
                    {
                        InstanceID instance = Util.ReadPrivate<CitizenVehicleWorldInfoPanel, InstanceID>(citizenVehicleInfoPanel, "m_InstanceID");
                        FPSCamera.instance.vehicleCamera.SetFollowInstance(instance.Vehicle);

                        if (FPSCamera.instance.hideUIComponent != null && FPSCamera.instance.config.integrateHideUI)
                        {
                            FPSCamera.instance.hideUIComponent.SendMessage("Hide");
                        }
                    }
                );

                //

                cityServiceVehicleInfoPanel = GameObject.Find("(Library) CityServiceVehicleWorldInfoPanel").GetComponent<CityServiceVehicleWorldInfoPanel>();
                cityServiceVehicleInfoPanel.Find<UITextField>("VehicleName").width = 200;

                cityServiceVehicleCameraButton = CreateCameraButton
                (
                    cityServiceVehicleInfoPanel.component,
                    (component, param) =>
                    {
                        InstanceID instance = Util.ReadPrivate<CityServiceVehicleWorldInfoPanel, InstanceID>(cityServiceVehicleInfoPanel, "m_InstanceID");
                        FPSCamera.instance.vehicleCamera.SetFollowInstance(instance.Vehicle);

                        if (FPSCamera.instance.hideUIComponent != null && FPSCamera.instance.config.integrateHideUI)
                        {
                            FPSCamera.instance.hideUIComponent.SendMessage("Hide");
                        }
                    }
                );

                //

                publicTransportVehicleInfoPanel = GameObject.Find("(Library) PublicTransportVehicleWorldInfoPanel").GetComponent<PublicTransportVehicleWorldInfoPanel>();
                publicTransportVehicleInfoPanel.Find<UITextField>("VehicleName").width = 200;

                publicTransportCameraButton = CreateCameraButton
                (
                    publicTransportVehicleInfoPanel.component,
                    (component, param) =>
                    {
                        InstanceID instance = Util.ReadPrivate<PublicTransportVehicleWorldInfoPanel, InstanceID>(publicTransportVehicleInfoPanel, "m_InstanceID");
                        FPSCamera.instance.vehicleCamera.SetFollowInstance(instance.Vehicle);

                        if (FPSCamera.instance.hideUIComponent != null && FPSCamera.instance.config.integrateHideUI)
                        {
                            FPSCamera.instance.hideUIComponent.SendMessage("Hide");
                        }
                    }
                );


                //

                citizenInfoPanel = GameObject.Find("(Library) CitizenWorldInfoPanel").GetComponent<CitizenWorldInfoPanel>();
                citizenInfoPanel.Find<UITextField>("PersonName").width = 180;

                citizenCameraButton = CreateCameraButton
                (
                    citizenInfoPanel.component,
                    (component, param) =>
                    {
                        InstanceID instance = Util.ReadPrivate<CitizenWorldInfoPanel, InstanceID>(citizenInfoPanel, "m_InstanceID");
                        FPSCamera.instance.citizenCamera.SetFollowInstance(instance.Citizen);

                        if (FPSCamera.instance.hideUIComponent != null && FPSCamera.instance.config.integrateHideUI)
                        {
                            FPSCamera.instance.hideUIComponent.SendMessage("Hide");
                        }
                    }
                );

                //

                initialized = true;
            }
        }

        UIButton CreateCameraButton(UIComponent parentComponent, MouseEventHandler handler)
        {
            var button = uiView.AddUIComponent(typeof(UIButton)) as UIButton;
            button.name = "ModTools Button";
            button.width = cameraButtonSize;
            button.height = cameraButtonSize;
            button.scaleFactor = 1.0f;
            button.pressedBgSprite = "OptionBasePressed";
            button.normalBgSprite = "OptionBase";
            button.hoveredBgSprite = "OptionBaseHovered";
            button.disabledBgSprite = "OptionBaseDisabled";
            button.normalFgSprite = "InfoPanelIconFreecamera";
            button.textColor = new Color32(255, 255, 255, 255);
            button.disabledTextColor = new Color32(7, 7, 7, 255);
            button.hoveredTextColor = new Color32(255, 255, 255, 255);
            button.focusedTextColor = new Color32(255, 255, 255, 255);
            button.pressedTextColor = new Color32(30, 30, 44, 255);
            button.eventClick += handler;
            button.AlignTo(parentComponent, UIAlignAnchor.TopRight);
            button.relativePosition += cameraButtonOffset;
            return button;
        }

    }

}
