using Assets.Script.Neural;
using Assets.Script.Neural.Interfaces;
using System;
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
	private IGeneticManager Manager;
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
	private TMP_Dropdown NetworkTypeDropdown;
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
	private List<string> NetworkTypes = new List<string>() {
		nameof(NeuralNetwork),
		nameof(NeuralNetworkNEAT)
	};

	private void OnEnable() {
		//InitialMousePosition = Mouse.transform.position;
		//InitialMouseRotation = Mouse.transform.rotation;
		MainCamera = GameObject.Find("Main Camera");
		SwitchCamera("Main Camera");

		ActivateButton = gameObject.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == "Activate");
		MarkersVisibilityToggle = gameObject.GetComponentsInChildren<Toggle>().FirstOrDefault(b => b.name == "MarkersVisibility");
		ControlModeDropdown = gameObject.GetComponentsInChildren<TMP_Dropdown>().FirstOrDefault(b => b.name == "ControlMode");
		SavedPopulationsDropdown = gameObject.GetComponentsInChildren<TMP_Dropdown>().FirstOrDefault(b => b.name == "SavedPopulations");
		NetworkTypeDropdown = gameObject.GetComponentsInChildren<TMP_Dropdown>().FirstOrDefault(b => b.name == "NetworkTypeSelection");
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
		UpdateNetworkTypes();
		UpdateNetworkTypeAccessibility();
		ToggleMarkersVisibility();

		//!!!
		//Manager = new GeneticManager(MouseController);
		//Manager = new GeneticManagerNEAT(MouseController);

		//Manager.OnTrainingComplete += (IGeneticManager m) => NeuralNetworkSerialization.SaveToJson(m.PopulationInterface, m.CurrentGeneration);
		//Manager.OnTrainingComplete += (IGeneticManager m) => ResetMouse();
		//Manager.OnNextAgentStart += (IGeneticManager m) => UpdateGenerationText(m.CurrentGeneration, m.CurrentGenome, m.PopulationSize);
		//Manager.OnRepopulated += (IGeneticManager m) => BestAgents.UpdateAgentFitness(m.TopFitnesses);
		InitiateGeneticManager();
		RunResults.MouseController = MouseController;
		MouseController.OnActivationChanged += RunResults.MouseActivationChangedEventHandler;
		MouseController.OnFinalTargetReached += RunResults.MouseFinalTargetReachedEventhandler;
	}

	private void InitiateGeneticManager(string networkType = null) {
		networkType = networkType ?? NetworkTypes[NetworkTypeDropdown.value];
		Manager?.ClearSubscriptions();
		switch (networkType) {
			case nameof(NeuralNetwork):
				Manager = new GeneticManager(MouseController);
				break;
			case nameof(NeuralNetworkNEAT):
				Manager = new GeneticManagerNEAT(MouseController);
				break;
			default:
				throw new ArgumentException("Unknown network population type.");
				break;
		}
		Manager.OnTrainingComplete += (IGeneticManager m) => NeuralNetworkSerialization.SaveToJson(m.PopulationInterface, m.CurrentGeneration);
		Manager.OnTrainingComplete += (IGeneticManager m) => ResetMouse();
		Manager.OnNextAgentStart += (IGeneticManager m) => UpdateGenerationText(m.CurrentGeneration, m.CurrentGenome, m.PopulationSize);
		Manager.OnNextAgentStart += (IGeneticManager m) => UpdateLayersParamsText(m.PopulationInterface[m.CurrentGenome]);
		Manager.OnRepopulated += (IGeneticManager m) => BestAgents.UpdateAgentFitness(m.TopFitnesses);
	}

	private void UpdateSavedPopulationsAccessibility() {
		SavedPopulationsDropdown.interactable = ControlModeDropdown.value == (int)Enums.ControlMode.Neural || ControlModeDropdown.value == (int)Enums.ControlMode.NeuralTraining;
	}

	private void UpdateNetworkTypeAccessibility() {
		NetworkTypeDropdown.interactable = SavedPopulationsDropdown.value == 0 && (ControlModeDropdown.value == (int)Enums.ControlMode.Neural || ControlModeDropdown.value == (int)Enums.ControlMode.NeuralTraining);
	}
	
	private void UpdateSavedPopulations() {
		SavedPopulations = new List<string>() { "None" };
		SavedPopulations.AddRange(NeuralNetworkSerialization.GetSavedPopulations());
		SavedPopulationsDropdown.options = SavedPopulations.Select(p => new TMP_Dropdown.OptionData(p.Split('\\').LastOrDefault().Split('.').FirstOrDefault())).ToList();
	}
	
	private void UpdateNetworkTypes() {
		NetworkTypeDropdown.options = NetworkTypes.Select(p => new TMP_Dropdown.OptionData(p)).ToList();
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
			&& NetworkTypes[NetworkTypeDropdown.value] == nameof(NeuralNetwork)
			&& MouseController.CurrentControlMode == Enums.ControlMode.NeuralTraining
			&& SavedPopulationsDropdown.value == 0;
	}

	public void UpdateLayersParamsText(INeuralNetwork network) {
		HiddenLayers.text = NeuralNetworkSerialization.GetHiddenLayersString(network);
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
		UpdateNetworkTypeAccessibility();
		UpdateAgentSelection();
		ResetTargetCell();
		UpdateTargetCellParamsAcessibility();
	}

	public void ActivateMouse() {
		MouseController.IsActive = true;
		ActivateButton.interactable = false;
		ControlModeDropdown.interactable = false;
		SavedPopulationsDropdown.interactable = false;
		NetworkTypeDropdown.interactable = false;
		AgentSelectionSlider.interactable = false;
		MouseController.CenterCell = GetTargetCell();
		UpdateTargetCellParamsAcessibility();

		if (MouseController.CurrentControlMode == Enums.ControlMode.NeuralTraining || MouseController.CurrentControlMode == Enums.ControlMode.Neural) {
			(string, int, List<INeuralNetwork>) population = (null, 0, null);
			if (SavedPopulationsDropdown.value != 0) {
				population = NeuralNetworkSerialization.LoadFromJson(SavedPopulations[SavedPopulationsDropdown.value]);
			}
			var networkType = population.Item1 ?? NetworkTypes[NetworkTypeDropdown.value];
			if (MouseController.CurrentControlMode == Enums.ControlMode.NeuralTraining) {
				
				InitiateGeneticManager(networkType);
				if (networkType == nameof(NeuralNetwork)) {
					var layersStructure = NeuralNetworkSerialization.ParseHiddenLayersString(HiddenLayers.text);
					((GeneticManager)Manager).StartTraining(population.Item3?.Select(n => (NeuralNetwork)n).ToList(), population.Item2, layersStructure, MutationSelectionSlider.value / 200);
					//HiddenLayers.text = NeuralNetworkSerialization.GetHiddenLayersString((NeuralNetwork)Manager.PopulationInterface.FirstOrDefault());
				} else if (networkType == nameof(NeuralNetworkNEAT)) {
					//HiddenLayers.text = "";f
					((GeneticManagerNEAT)Manager).StartTraining(population.Item3?.Select(n => (NeuralNetworkNEAT)n).ToList(), population.Item2, MutationSelectionSlider.value / 200);
				} else {
					throw new ArgumentException("Unknown network population type.");
				}
				UpdateLayersParamsText(Manager.PopulationInterface.FirstOrDefault());
				SaveButton.interactable = true;
			} else {
				INeuralNetwork network = population.Item3?[(int)AgentSelectionSlider.value];
				if (network == null) {
					switch (networkType) {
						case nameof(NeuralNetwork):
							network = new NeuralNetwork(3, 2, 9, 2);
							break;
						case nameof(NeuralNetworkNEAT):
							network = new NeuralNetworkNEAT(3, 2);
							break;
						default:
							throw new ArgumentException("Unknown network population type.");
							break;
					}
				}
				MouseController.Reset(network);
				UpdateGenerationText(population.Item2, (int)AgentSelectionSlider.value + 1, population.Item3?.Count ?? 1);
				UpdateLayersParamsText(network);
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
		UpdateNetworkTypeAccessibility();
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
		UpdateNetworkTypeAccessibility();
		UpdateGenerationText();
		HiddenLayers.text = "";
		if (SavedPopulationsDropdown.value == 0 || MouseController.CurrentControlMode != Enums.ControlMode.Neural && MouseController.CurrentControlMode != Enums.ControlMode.NeuralTraining) {
			return;
		}
		var population = NeuralNetworkSerialization.LoadFromJson(SavedPopulations[SavedPopulationsDropdown.value]);
		if(population.Item3 == null) {
			return;
		}
		AgentSelectionSlider.interactable = MouseController.CurrentControlMode == Enums.ControlMode.Neural;
		AgentSelectionSlider.maxValue = population.Item3.Count - 1;
		if (population.Item1 == nameof(NeuralNetwork)) {
			//HiddenLayers.text = NeuralNetworkSerialization.GetHiddenLayersString((NeuralNetwork)population.Item3.FirstOrDefault());
			UpdateLayersParamsText(population.Item3.FirstOrDefault());
		} else if (population.Item1 == nameof(NeuralNetworkNEAT)) {
			//do stuff
		} else {
			throw new ArgumentException("Unknown network population type.");
		}
		UpdateGenerationText(Mathf.Max(population.Item2, 1), (int)AgentSelectionSlider.value + 1, population.Item3.Count);
		BestAgents.UpdateAgentFitness(population.Item3.GetRange(0, Manager.BestAgents).Select(n => n.Fitness));
		NetworkTypeDropdown.value = NetworkTypes.IndexOf(population.Item1);
	}

	public void UpdateNetworkTypeSelection() {
		UpdateLayersParamsAccessibility();
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
