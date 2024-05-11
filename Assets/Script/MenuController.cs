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
    private Text GenerationText;
    private Text FitnessText;
    private Slider AgentSelectionSlider;
    private Text AgentText;
    private TMP_InputField HiddenLayers;
    private Slider MutationSelectionSlider;
    private Text MutationText;
    private TMP_InputField TargetCellXInput;
    private TMP_InputField TargetCellYInput;

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
        AgentSelectionSlider = gameObject.GetComponentsInChildren<Slider>().FirstOrDefault(b => b.name == "AgentSelection");
        AgentText = gameObject.GetComponentsInChildren<Text>().FirstOrDefault(b => b.name == "Agent");
        SaveButton.interactable = false;
        GenerationText = gameObject.GetComponentsInChildren<Text>().FirstOrDefault(b => b.name == "Generation");
        FitnessText = gameObject.GetComponentsInChildren<Text>().FirstOrDefault(b => b.name == "Fitness");
        HiddenLayers = gameObject.GetComponentsInChildren<TMP_InputField>().FirstOrDefault(b => b.name == "HiddenLayers");
        MutationSelectionSlider = gameObject.GetComponentsInChildren<Slider>().FirstOrDefault(b => b.name == "MutationSelection");
        MutationText = gameObject.GetComponentsInChildren<Text>().FirstOrDefault(b => b.name == "Mutation");
        TargetCellXInput = gameObject.GetComponentsInChildren<TMP_InputField>().FirstOrDefault(b => b.name == "TargetCellX");
        TargetCellYInput = gameObject.GetComponentsInChildren<TMP_InputField>().FirstOrDefault(b => b.name == "TargetCellY");
        UpdateAgentSelection();
        UpdateGenerationText();
        UpdateFitnessText();
        UpdateAgentText();
        UpdateLayersParamsAccessibility();
        UpdateMutationSelectionAccessibility();
        ResetTargetCellText();
        UpdateTargetCellParamsAcessibility();

        UpdateSavedPopulations();
        UpdateSavedPopulationsAccessibility();
        Manager.OnTrainingComplete += (GeneticManager m) => NeuralNetworkSerialization.SaveToJson(m.Population, m.currentGeneration);
        Manager.OnTrainingComplete += (GeneticManager m) => ResetMouse();
        Manager.OnNextAgentStart += (GeneticManager m) => UpdateGenerationText(m.currentGeneration, m.currentGenome, m.PopulationSize);
        Manager.OnRepopulated += (GeneticManager m) => UpdateFitnessText(m.TopFitnesses);
    }

    private void UpdateSavedPopulationsAccessibility() {
        SavedPopulationsDropdown.interactable = ControlModeDropdown.value == (int)Enums.ControlMode.Neural || ControlModeDropdown.value == (int)Enums.ControlMode.NeuralTraining;
    }
    
    private void UpdateSavedPopulations() {
        SavedPopulations = new List<string>() { "None" };
        SavedPopulations.AddRange(NeuralNetworkSerialization.GetSavedPopulations());
        SavedPopulationsDropdown.options = SavedPopulations.Select(p => new TMP_Dropdown.OptionData(p.Split('\\').LastOrDefault().Split('.').FirstOrDefault())).ToList();
    }

    private void UpdateGenerationText(int generation = 0, int agent = 0, int population = 0) {
        GenerationText.text = generation + agent + population == 0 ? "" : $"{generation} - {agent} / {population}";
    }

    private void UpdateFitnessText(IEnumerable<float> fitnesses = null) {
        FitnessText.text = string.Join(" / ", fitnesses?.Select(f => f.ToString("0")) ?? new []{ "" });
    }

    public void UpdateAgentText() {
        AgentText.text = (int)AgentSelectionSlider.value + 1 + "";
    }

    public void UpdateMutationText() {
        MutationText.text = $"{MutationSelectionSlider.value / 2:0.0}%";
    }

    public void ResetTargetCellText() {
        int x;
        int y;
        if (MouseController.CurrentControlMode == Enums.ControlMode.NeuralTraining) {
            x = 1;
            y = 0;
        }
        else {
            x = 7;
            y = 7;
        }
        SetTargetCellText(x, y);
    }

    private void SetTargetCellText(int x, int y) {
        TargetCellXInput.text = x + "";
        TargetCellYInput.text = y + "";
        MouseController.InitializeMazePaths(new Point(x, y));
    }

    private Point GetTargetCell() {
        if (string.IsNullOrEmpty(TargetCellXInput.text) || string.IsNullOrEmpty(TargetCellYInput.text)) {
            ResetTargetCellText();
        }
        var x = Mathf.Min(int.Parse(TargetCellXInput.text), 15);
        var y = Mathf.Min(int.Parse(TargetCellYInput.text), 15);
        SetTargetCellText(x, y);
        return new Point(x, y);
    }

    public void UpdateTargetCell() {
        GetTargetCell();
    }

    public static void Quit() {
        Application.Quit();
    }

    public void ActivateMouse() {
        MouseController.IsActive = true;
        ActivateButton.interactable = false;
        ControlModeDropdown.interactable = false;
        SavedPopulationsDropdown.interactable = false;
        AgentSelectionSlider.interactable = false;
        MouseController.CenterCell = GetTargetCell();
        UpdateTargetCellParamsAcessibility();
        if (MouseController.CurrentControlMode == Enums.ControlMode.NeuralTraining || MouseController.CurrentControlMode == Enums.ControlMode.Neural) {
            (int, List<NeuralNetwork>) population = (0, null);
            if (SavedPopulationsDropdown.value != 0) {
                population = NeuralNetworkSerialization.LoadFromJson(SavedPopulations[SavedPopulationsDropdown.value]);
            }
            if (MouseController.CurrentControlMode == Enums.ControlMode.NeuralTraining) {
                var layersStructure = NeuralNetworkSerialization.ParseHiddenLayersString(HiddenLayers.text);
                Manager.StartTraining(population.Item2, population.Item1, layersStructure, MutationSelectionSlider.value / 200);
                HiddenLayers.text = NeuralNetworkSerialization.GetHiddenLayersString(Manager.Population.FirstOrDefault());
                SaveButton.interactable = true;
            } else {
                MouseController.Reset(population.Item2?[(int)AgentSelectionSlider.value] ?? new NeuralNetwork(3, 2, 10, 2));
            }
            UpdateLayersParamsAccessibility();
            UpdateMutationSelectionAccessibility();
        } else {
            MouseController.Reset();
            UpdateGenerationText();
            UpdateFitnessText();
        }
    }

    public void ToggleMarkersVisibility() {
        MouseController.ShowPathMarkers = MarkersVisibilityToggle.isOn;
    }

    public void ResetMouse() {
        var name = Mouse.name;
        var cameraState = MouseCamera.activeSelf;
        var controlMode = MouseController.CurrentControlMode;
        MouseController.IsActive = false;
        MouseController.Reset();
        ActivateButton.interactable = true;
        SaveButton.interactable = false;
        ControlModeDropdown.interactable = true;
        var selectedAgent = AgentSelectionSlider.value;
        UpdateAgentSelection();
        AgentSelectionSlider.value = selectedAgent;
        UpdateAgentText();
        UpdateSavedPopulationsAccessibility();
        UpdateTargetCellParamsAcessibility();
        SetTimeScale(1f);
        UpdateSavedPopulations();
        GetTargetCell();
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
        UpdateAgentSelection();
        ResetTargetCellText();
        UpdateTargetCellParamsAcessibility();
    }

    public void SavePopulation() {
        Manager.TargetFitness = int.MinValue;
        SaveButton.interactable = false;
    }

    public void UpdateAgentSelection() {
        AgentSelectionSlider.value = 0;
        AgentSelectionSlider.interactable = false;
        UpdateAgentText();
        MutationSelectionSlider.value = (int)(5.5f * 2);
        UpdateMutationText();
        UpdateMutationSelectionAccessibility();
        UpdateLayersParamsAccessibility();
        HiddenLayers.text = "";
        if (SavedPopulationsDropdown.value == 0 || MouseController.CurrentControlMode != Enums.ControlMode.Neural && MouseController.CurrentControlMode != Enums.ControlMode.NeuralTraining) {
            return;
        }
        var population = NeuralNetworkSerialization.LoadFromJson(SavedPopulations[SavedPopulationsDropdown.value]);
        if(population.Item2 == null) {
            return;
        }
        AgentSelectionSlider.interactable = MouseController.CurrentControlMode == Enums.ControlMode.Neural;
        AgentSelectionSlider.maxValue = population.Item2.Count - 1;
        HiddenLayers.text = NeuralNetworkSerialization.GetHiddenLayersString(population.Item2.FirstOrDefault());
        UpdateGenerationText(Mathf.Max(population.Item1, 1), (int)AgentSelectionSlider.value + 1, population.Item2?.Count ?? Manager.PopulationSize);
        UpdateFitnessText(population.Item2?.GetRange(0, Manager.BestAgents).Select(n => n.Fitness));
    }

    public void UpdateMutationSelectionAccessibility() {
        MutationSelectionSlider.interactable = !MouseController.IsActive 
            && MouseController.CurrentControlMode == Enums.ControlMode.NeuralTraining;
    }

    public void UpdateLayersParamsAccessibility() {
        HiddenLayers.interactable = !MouseController.IsActive 
            && MouseController.CurrentControlMode == Enums.ControlMode.NeuralTraining 
            && SavedPopulationsDropdown.value == 0;
    }

    public void UpdateTargetCellParamsAcessibility() {
        TargetCellYInput.interactable = TargetCellXInput.interactable = !MouseController.IsActive
            && MouseController.CurrentControlMode != Enums.ControlMode.Manual;
    }
}
