using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField]
    private GameObject Mouse;
    [SerializeField]
    private GameObject MousePrefab;

    private Vector3 InitialMousePosition;
    private Quaternion InitialMouseRotation;

    private Button ActivateButton;
    private Toggle ManualControlToggle;
    private Toggle MarkersVisibilityToggle;
    private GameObject MainCamera;
    private GameObject MouseCamera { get => Mouse.GetComponentInChildren<Camera>(true).gameObject; }

    private void OnEnable() {
        InitialMousePosition = Mouse.transform.position;
        InitialMouseRotation = Mouse.transform.rotation;
        MainCamera = GameObject.Find("Main Camera");
        SwitchCamera("Main Camera");
        ActivateButton = gameObject.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == "Activate");
        ManualControlToggle = gameObject.GetComponentsInChildren<Toggle>().FirstOrDefault(b => b.name == "ManualControl");
        MarkersVisibilityToggle = gameObject.GetComponentsInChildren<Toggle>().FirstOrDefault(b => b.name == "MarkersVisibility");
    }

    public static void Quit() {
        Application.Quit();
    }

    public void ActivateMouse() {
        Mouse.GetComponent<MouseController>().IsActive = true;
        ActivateButton.interactable = false;
        ManualControlToggle.interactable = false;
    }

    public void ToggleManualControl() {
        Mouse.GetComponent<MouseController>().UseManualControl = ManualControlToggle.isOn;
    }

    public void ToggleMarkersVisibility() {
        Mouse.GetComponent<MouseController>().ShowPathMarkers = MarkersVisibilityToggle.isOn;
    }

    public void ResetMouse() {
        var name = Mouse.name;
        var cameraState = MouseCamera.activeSelf;
        //Activate object to get in the new mouse instance, which will deactivate it
        MarkersVisibilityToggle.isOn = false;
        Mouse.GetComponent<MouseController>().ShowPathMarkers = true;
        GameObject.Destroy(Mouse);
        Mouse = GameObject.Instantiate(MousePrefab, InitialMousePosition, InitialMouseRotation);
        Mouse.name = name;
        MouseCamera.SetActive(cameraState);
        ActivateButton.interactable = true;
        ManualControlToggle.interactable = true;
        ToggleManualControl();
        SetTimeScale(1f);
    }

    public static void SetTimeScale (float scale) {
        if (scale < 0) {
            return;
        }

        Time.timeScale = scale;
    }

    public void SwitchCamera(string cameraName) {
        switch(cameraName) {
            case "Main Camera":
                MainCamera.SetActive(true);
                MouseCamera.SetActive(false);
                break;
            case "Mouse Camera":
                MainCamera.SetActive(false);
                MouseCamera.SetActive(true);
                break;
        }
    }
}
