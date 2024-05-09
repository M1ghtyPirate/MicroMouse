using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField]
    private GameObject Mouse;
    private MouseController MouseController { get => Mouse.GetComponent<MouseController>(); }
    [SerializeField]
    private GeneticManager Manager;
    [SerializeField]
    private GameObject MousePrefab;

    private Vector3 InitialMousePosition;
    private Quaternion InitialMouseRotation;

    private Button ActivateButton;
    private Toggle MarkersVisibilityToggle;
    private GameObject MainCamera;
    private GameObject MouseCamera { get => Mouse.GetComponentInChildren<Camera>(true).gameObject; }
    private TMP_Dropdown ControlModeDropdown;
    private TMP_Dropdown SavedPopulationsDropdown;
    private Button SaveButton;

    private List<string> SavedPopulations;

    private void OnEnable() {
        InitialMousePosition = Mouse.transform.position;
        InitialMouseRotation = Mouse.transform.rotation;
        MainCamera = GameObject.Find("Main Camera");
        SwitchCamera("Main Camera");
        ActivateButton = gameObject.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == "Activate");
        MarkersVisibilityToggle = gameObject.GetComponentsInChildren<Toggle>().FirstOrDefault(b => b.name == "MarkersVisibility");
        ControlModeDropdown = gameObject.GetComponentsInChildren<TMP_Dropdown>().FirstOrDefault(b => b.name == "ControlMode");
        SavedPopulationsDropdown = gameObject.GetComponentsInChildren<TMP_Dropdown>().FirstOrDefault(b => b.name == "SavedPopulations");
        SaveButton = gameObject.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == "Save");
        SaveButton.interactable = false;
        UpdateSavedPopulations();
        UpdateSavedPopulationsAccessibility();
        Manager.OnTrainingComplete += NeuralNetworkSerialization.SaveToJson;
        Manager.OnTrainingComplete += (List<NeuralNetwork> arg) => ResetMouse();
    }

    private void UpdateSavedPopulationsAccessibility() {
        SavedPopulationsDropdown.interactable = ControlModeDropdown.value == (int)Enums.ControlMode.Neural || ControlModeDropdown.value == (int)Enums.ControlMode.NeuralTraining;
    }
    
    private void UpdateSavedPopulations() {
        SavedPopulations = new List<string>() { "None" };
        SavedPopulations.AddRange(NeuralNetworkSerialization.GetSavedPopulations());
        SavedPopulationsDropdown.options = SavedPopulations.Select(p => new TMP_Dropdown.OptionData(p.Split('\\').LastOrDefault().Split('.').FirstOrDefault())).ToList();
        SavedPopulationsDropdown.value = 0;
    }

    public static void Quit() {
        Application.Quit();
    }

    public void ActivateMouse() {
        MouseController.IsActive = true;
        ActivateButton.interactable = false;
        ControlModeDropdown.interactable = false;
        SavedPopulationsDropdown.interactable = false;
        if (MouseController.CurrentControlMode == Enums.ControlMode.NeuralTraining || MouseController.CurrentControlMode == Enums.ControlMode.Neural) {
            List<NeuralNetwork> population = null;
            if (SavedPopulationsDropdown.value != 0) {
                population = NeuralNetworkSerialization.LoadFromJson(SavedPopulations[SavedPopulationsDropdown.value]);
            }
            if (MouseController.CurrentControlMode == Enums.ControlMode.NeuralTraining) {
                MouseController.CenterCell = new Point(1, 0);
                Manager.StartTraining(population);
                SaveButton.interactable = true;
            } else {
                MouseController.ResetNeural(population?.FirstOrDefault() ?? new NeuralNetwork(3, 2, 10, 2));
            }
        }
    }

    public void ToggleMarkersVisibility() {
        MouseController.ShowPathMarkers = MarkersVisibilityToggle.isOn;
    }

    public void ResetMouse() {
        var name = Mouse.name;
        var cameraState = MouseCamera.activeSelf;
        var controlMode = MouseController.CurrentControlMode;
        //Activate object to get in the new mouse instance, which will deactivate it
        MarkersVisibilityToggle.isOn = false;
        MouseController.ShowPathMarkers = true;
        GameObject.Destroy(Mouse);
        Mouse = GameObject.Instantiate(MousePrefab, InitialMousePosition, InitialMouseRotation);
        Mouse.name = name;
        MouseCamera.SetActive(cameraState);
        ActivateButton.interactable = true;
        SaveButton.interactable = false;
        ControlModeDropdown.interactable = true;
        UpdateSavedPopulationsAccessibility();
        MouseController.CurrentControlMode = controlMode;
        Manager.MouseController = MouseController;
        //ToggleManualControl();
        SetTimeScale(1f);
        UpdateSavedPopulations();
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

    public void SwitchControlMode() {
        //Debug.Log($"Current dropdown value: {ControlModeDropdown.value}");
        MouseController.CurrentControlMode = (Enums.ControlMode)ControlModeDropdown.value;
        UpdateSavedPopulationsAccessibility();
    }

    public void SavePopulation() {
        Manager.TargetFitness = int.MinValue;
        SaveButton.interactable = false;
    }
}
