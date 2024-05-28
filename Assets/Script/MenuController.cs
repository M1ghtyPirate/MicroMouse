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
    [SerializeField]
    private PathMarkersController PathMarkers;
    private MouseController MouseController { get => Mouse.GetComponent<MouseController>(); }    
    private GeneticManager Manager;
    //[SerializeField]
    //private GameObject MousePrefab;

    //private Vector3 InitialMousePosition;
    //private Quaternion InitialMouseRotation;

    private Button ActivateButton;
    private Toggle MarkersVisibilityToggle;
    private GameObject MainCamera;
    private GameObject MouseCamera { get => Mouse.GetComponentInChildren<Camera>(true).gameObject; }
    private TMP_Dropdown ControlModeDropdown;
    private TMP_Dropdown SavedPopulationsDropdown;
    private Button SaveButton;
    private Text GenerationText;
    private Slider AgentSelectionSlider;
    private Text AgentText;
    private TMP_InputField HiddenLayers;
    private Slider MutationSelectionSlider;
    private Text MutationText;
    private Slider TargetCellXSlider;
    private Text TargetCellXText;
    private Slider TargetCellYSlider;
    private Text TargetCellYText;
    private RunResultsController RunResults;
    private BestAgentsController BestAgents;

    private List<string> SavedPopulations;

    private void OnEnable() {
        //InitialMousePosition = Mouse.transform.position;
        //InitialMouseRotation = Mouse.transform.rotation;
        MainCamera = GameObject.Find("Main Camera");
        SwitchCamera("Main Camera");

        ActivateButton = gameObject.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == "Activate");
        MarkersVisibilityToggle = gameObject.GetComponentsInChildren<Toggle>().FirstOrDefault(b => b.name == "MarkersVisibility");
        ControlModeDropdown = gameObject.GetComponentsInChildren<TMP_Dropdown>().FirstOrDefault(b => b.name == "ControlMode");
        SavedPopulationsDropdown = gameObject.GetComponentsInChildren<TMP_Dropdown>().FirstOrDefault(b => b.name == "SavedPopulations");
        SaveButton = gameObject.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == "Save");
        AgentSelectionSlider = gameObject.GetComponentsInChildren<Slider>().FirstOrDefault(b => b.name == "AgentSelection");
        AgentText = gameObject.GetComponentsInChildren<Text>().FirstOrDefault(b => b.name == "Agent");
        GenerationText = gameObject.GetComponentsInChildren<Text>().FirstOrDefault(b => b.name == "Generation");
        HiddenLayers = gameObject.GetComponentsInChildren<TMP_InputField>().FirstOrDefault(b => b.name == "HiddenLayers");
        MutationSelectionSlider = gameObject.GetComponentsInChildren<Slider>().FirstOrDefault(b => b.name == "MutationSelection");
        MutationText = gameObject.GetComponentsInChildren<Text>().FirstOrDefault(b => b.name == "Mutation");
        TargetCellXSlider = gameObject.GetComponentsInChildren<Slider>().FirstOrDefault(b => b.name == "TargetCellXSelection");
        TargetCellXText = gameObject.GetComponentsInChildren<Text>().FirstOrDefault(b => b.name == "TargetCellX");
        TargetCellYSlider = gameObject.GetComponentsInChildren<Slider>().FirstOrDefault(b => b.name == "TargetCellYSelection");
        TargetCellYText = gameObject.GetComponentsInChildren<Text>().FirstOrDefault(b => b.name == "TargetCellY");
        RunResults = gameObject.GetComponentsInChildren<RunResultsController>().FirstOrDefault(b => b.name == "Runs");
        BestAgents = gameObject.GetComponentsInChildren<BestAgentsController>().FirstOrDefault(b => b.name == "BestAgents");

        SaveButton.interactable = false;
        UpdateAgentSelection();
        UpdateGenerationText();
        BestAgents.UpdateAgentFitness();
        UpdateAgentText();
        UpdateLayersParamsAccessibility();
        UpdateMutationSelectionAccessibility();
        ResetTargetCell();
        UpdateTargetCellParamsAcessibility();
        UpdateSavedPopulations();
        UpdateSavedPopulationsAccessibility();
        ToggleMarkersVisibility();

        Manager = new GeneticManager(MouseController);
        Manager.OnTrainingComplete += (GeneticManager m) => NeuralNetworkSerialization.SaveToJson(m.Population, m.currentGeneration);
        Manager.OnTrainingComplete += (GeneticManager m) => ResetMouse();
        Manager.OnNextAgentStart += (GeneticManager m) => UpdateGenerationText(m.currentGeneration, m.currentGenome, m.PopulationSize);
        Manager.OnRepopulated += (GeneticManager m) => BestAgents.UpdateAgentFitness(m.TopFitnesses);
        RunResults.MouseController = MouseController;
        MouseController.OnActivationChanged += RunResults.MouseActivationChangedEventHandler;
        MouseController.OnFinalTargetReached += RunResults.MouseFinalTargetReachedEventhandler;
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

    public void ResetTargetCell() {
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
        //SetTargetCellText(x, y);
        TargetCellXSlider.value = x;
        TargetCellYSlider.value = y;
        UpdateTargetCell();
    }

    private Point GetTargetCell() {
        return new Point((int)TargetCellXSlider.value, (int)TargetCellYSlider.value);
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
        TargetCellYSlider.interactable = TargetCellYSlider.interactable = !MouseController.IsActive
            && MouseController.CurrentControlMode != Enums.ControlMode.Manual;
    }

    #region MainControlEvendHandlers

    public static void SetTimeScale(float scale) {
        if (scale < 0) {
            return;
        }

        Time.timeScale = scale;
    }

    public void ToggleMarkersVisibility() {
        //MouseController.ShowPathMarkers = MarkersVisibilityToggle.isOn;
        if (PathMarkers == null) {
            Debug.LogError($"PathMarkers Controller not found");
            return;
        }
        PathMarkers.ShowPathMarkers = MarkersVisibilityToggle.isOn;
    }

    public void SwitchControlMode() {
        //Debug.Log($"Current dropdown value: {ControlModeDropdown.value}");
        MouseController.CurrentControlMode = (Enums.ControlMode)ControlModeDropdown.value;
        UpdateSavedPopulationsAccessibility();
        UpdateAgentSelection();
        ResetTargetCell();
        UpdateTargetCellParamsAcessibility();
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
                MouseController.Reset(population.Item2?[(int)AgentSelectionSlider.value] ?? new NeuralNetwork(3, 2, 9, 2));
                UpdateGenerationText(population.Item1, (int)AgentSelectionSlider.value + 1, population.Item2?.Count ?? Manager.PopulationSize);
            }
            UpdateLayersParamsAccessibility();
            UpdateMutationSelectionAccessibility();
        } else {
            MouseController.Reset();
            UpdateGenerationText();
            BestAgents.UpdateAgentFitness();
        }
    }

    public void ResetMouse() {
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

    public static void Quit() {
        Application.Quit();
    }

    #endregion

    #region NeuralControlsEventHandlers

    public void UpdateAgentSelection() {
        AgentSelectionSlider.value = 0;
        AgentSelectionSlider.interactable = false;
        UpdateAgentText();
        BestAgents.UpdateAgentFitness();
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
        BestAgents.UpdateAgentFitness(population.Item2.GetRange(0, Manager.BestAgents).Select(n => n.Fitness));
    }

    public void UpdateAgentText() {
        AgentText.text = (int)AgentSelectionSlider.value + 1 + "";
    }

    public void UpdateMutationText() {
        MutationText.text = $"{MutationSelectionSlider.value / 2:0.0}%";
    }

    public void UpdateTargetCell() {
        var x = (int)TargetCellXSlider.value;
        var y = (int)TargetCellYSlider.value;
        TargetCellXText.text = (int)TargetCellXSlider.value + 1 + "";
        TargetCellYText.text = (int)TargetCellYSlider.value + 1 + "";
        MouseController.InitializeMazePaths(new Point(x, y));
    }

    public void SavePopulation() {
        Manager.TargetFitness = int.MinValue;
        SaveButton.interactable = false;
    }

    #endregion
}
