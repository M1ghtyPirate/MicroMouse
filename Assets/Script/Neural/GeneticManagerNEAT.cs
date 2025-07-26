using Assets.Script.Neural.Interfaces;
using Assets.Script.Neural.Models.NEAT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Script.Neural {
	internal class GeneticManagerNEAT : IGeneticManager {
		public Action<IGeneticManager> OnTrainingComplete { get; set; }
		public Action<IGeneticManager> OnRepopulated { get; set; }
		public Action<IGeneticManager> OnNextAgentStart { get; set; }

		public void ClearSubscriptions() {
			OnTrainingComplete = null;
			OnRepopulated = null;
			OnNextAgentStart = null;
			MouseController.OnNeuralDeath -= OnNeuralDeath;
		}


		public int BestAgents { get; set; } = 20;
		public int CurrentGeneration { get; set; }
		public int CurrentGenome { get; set; }
		public List<float> TopFitnesses { get; set; }
		public float TargetFitness { get; set; }
		//public int PopulationSize { get; set; } = 85;
		public int PopulationSize { get; set; } = 1000;

		public List<NeuralNetworkNEAT> Population { get; set; }
		public List<INeuralNetwork> PopulationInterface => Population?.Select(p => (INeuralNetwork)p).ToList();

		public int InputLayerNeuronCount { get; set; } = 3;
		public int OutputLayerNeuronCount { get; set; } = 2;
		public MouseController MouseController { get; set; }

		#region Mutation

		public float WeightShiftValue { get; set; } = 0.3f;

		public float MutationChance { get; set; }
		public float LinkMutationChance => MutationChance * 0.4f;
		public float RecurrentLinkMutationChance => MutationChance * 0.1f;
		public float NodeMutationChance => MutationChance * 0.2f;
		public float WeightRandomMutationChance => MutationChance * 0.4f;
		public float WeightShiftMutationChance => MutationChance * 0.4f;
		public float LinkToggleMutationChance => MutationChance * 0.02f;

		//public float MutationChance { get; set; }
		//public float LinkMutationChance => MutationChance * 0.01f;
		//public float RecurrentLinkMutationChance => MutationChance * 0.1f;
		//public float NodeMutationChance => MutationChance * 0.003f;
		//public float WeightRandomMutationChance => MutationChance * 0.002f;
		//public float WeightShiftMutationChance => MutationChance * 0.002f;
		//public float LinkToggleMutationChance => MutationChance * 0.001f;

		public int MutationAttempts { get; set; } = 100;

		public float SpeciesSurvivalRate { get; set; } = 0.2f;
		public int SpeciesStagnationLimit { get; set; } = 15;
		//public float SpeciesImprovementThreshold { get; set; } = 200f;
		public float SpeciesImprovementThreshold { get; set; } = float.MinValue;

		#endregion

		public Dictionary<(int, int), int> LinkInnovations { get; set; }
		public int LinkInnovationsCounter { get; set; }
		public int NodeIndexCounter { get; set; }

		public List<Species> Species { get; set; }

		#region NetworkDistanceCalculation

		public float SpeciesDistanceThreshold { get; set; } = 4f;
		public int C1 { get; set; } = 1;
		public int C2 { get; set; } = 1;
		public int C3 { get; set; } = 1;
		public int LinkSizeThreshold { get; set; } = 20;

		#endregion

		public GeneticManagerNEAT(MouseController mouseController) {
			MouseController = mouseController;
			MouseController.OnNeuralDeath += OnNeuralDeath;
		}

		public void StartTraining(List<NeuralNetworkNEAT> existingPopulation = null, int generation = 1, float mutationChance = 1f) {
			CurrentGeneration = generation;
			CurrentGenome = -1;
			MutationChance = mutationChance;
			TargetFitness = float.MaxValue;
			Population = existingPopulation ?? new List<NeuralNetworkNEAT>();
			LinkInnovations = new Dictionary<(int, int), int>();
			LinkInnovationsCounter = 0;
			NodeIndexCounter = 0;
			Species = new List<Species>();
			GrowPopulation(Population, PopulationSize);
			UpdateLinkInnovations();
			UpdateNodeIndexCounter();
			ResetNodeValues();
			ResetPopulationFitness();
			//Speciate();
			OnNeuralDeath(MouseController);
		}

		private void ResetPopulationFitness() {
			foreach (var nnet in Population) {
				nnet.Fitness = 0;
			}
		}

		private void GrowPopulation(List<NeuralNetworkNEAT> population, int targetSize) {
			while (population.Count < targetSize) {
				var nnet = new NeuralNetworkNEAT(InputLayerNeuronCount, OutputLayerNeuronCount);
				population.Add(nnet);
			}
		}

		private void UpdateLinkInnovations() {
			foreach (var network in Population) {
				foreach (var link in network.Links) {
					var key = (link.From.Index, link.To.Index);
					if (LinkInnovations.ContainsKey(key)) {
						continue;
					}
					LinkInnovations[key] = link.Innovation;
					LinkInnovationsCounter = Math.Max(LinkInnovationsCounter, link.Innovation);
				}
			}
		}

		private void UpdateNodeIndexCounter() {
			NodeIndexCounter = Population.SelectMany(n => n.AllNodes).Max(n => n.Index);
		}

		private void ResetNodeValues() {
			foreach (var node in Population.SelectMany(n => n.AllNodes)) {
				node.Value = 0;
			}
		}

		private float CalculateNetworkDistance(NeuralNetworkNEAT network1, NeuralNetworkNEAT network2) {
			if (network1 == null || network2 == null) {
				return float.MaxValue;
			}

			var N = Math.Max(network1.Links.Count, network2.Links.Count);
			N = N < LinkSizeThreshold ? 1 : N;
			var excessGenesThreshhold = Math.Min(network1.Links.Max(l => l.Innovation), network2.Links.Max(l => l.Innovation));
			var E = network1.Links.Union(network2.Links).Where(l => l.Innovation > excessGenesThreshhold).Count();
			var sharedGenes1 = network1.Links.Where(l1 => network2.Links.Any(l2 => l2.Innovation == l1.Innovation));
			var sharedGenes2 = network2.Links.Where(l2 => network1.Links.Any(l1 => l1.Innovation == l2.Innovation));
			var D = network1.Links.Count - sharedGenes1.Count() + network2.Links.Count - sharedGenes2.Count();
			var W = sharedGenes1.Sum(l1 => Math.Abs(l1.Weight - sharedGenes2.FirstOrDefault(l2 => l2.Innovation == l1.Innovation).Weight)) / sharedGenes1.Count();
			var d = C1 * E / N + C2 * D / N + C3 * W;
			return d;
		}

		private void Speciate() {
			//// Single species
			//var species0 = Species.FirstOrDefault();
			//if (species0 == null) {
			//	species0 = new Species() {
			//		Index = 0,
			//		Networks = Population.ToList()
			//	};
			//	Species.Add(species0);
			//}
			//return;

			foreach(var existingSpecies in Species.ToList()) {
				if (existingSpecies.AvgFitness > existingSpecies.BestAvgFitness) {
					existingSpecies.GenerationsSinceLastImprovement = 0;
					existingSpecies.BestAvgFitness = existingSpecies.AvgFitness;
				} else {
					existingSpecies.GenerationsSinceLastImprovement++;
					if (existingSpecies.GenerationsSinceLastImprovement > SpeciesStagnationLimit) {
						ShrinkSpecies(Population, existingSpecies, 0);
						Species.Remove(existingSpecies);
						Debug.LogWarning($"Species removed <{existingSpecies.Index}>");
						continue;
					}
				}
				var minDistance = existingSpecies.Networks
					.Min(n => CalculateNetworkDistance(n, existingSpecies.Representative));
				existingSpecies.Representative = (NeuralNetworkNEAT)existingSpecies.Networks
					.FirstOrDefault(n => CalculateNetworkDistance(n, existingSpecies.Representative) == minDistance)
					.Clone()
					?? existingSpecies.Representative;
				existingSpecies.Networks.Clear();
			}
			foreach (var network in Population) {
				var species = Species.FirstOrDefault(s => CalculateNetworkDistance(network, s?.Representative) < SpeciesDistanceThreshold);
				if (species == null) { 
					species = new Species() {
						Networks = new List<NeuralNetworkNEAT>(),
						Index = (Species.Max(s => s?.Index) ?? -1) + 1,
						Representative = (NeuralNetworkNEAT)network.Clone()
					};
					Species.Add(species);
				}
				species.Networks.Add(network);
			}
			Species = Species.OrderByDescending(s => s.AvgFitness).ToList();
			Debug.LogWarning($"Determined <{Species.Count}: {string.Join(", ", Species.Select(s => s.Networks.Count))}> species.");
		}

		private void OnNeuralDeath(MouseController mouse) {
			CurrentGenome++;
			if (CurrentGenome == Population.Count) {
				RePopulate();
			}
			if (MouseController.IsActive) {
				mouse.Reset(Population[CurrentGenome]);
				OnNextAgentStart?.Invoke(this);
			}
		}

		private NeuralNetworkNEAT Crossover(NeuralNetworkNEAT parent1, NeuralNetworkNEAT parent2) {
			if (parent1 == null || parent2 == null) {
				return null;
			}

			var child = new NeuralNetworkNEAT();
			var fitParent = parent1.Fitness > parent2.Fitness ? parent1 : parent2;
			var unfitParent = fitParent == parent1 ? parent2 : parent1;
			foreach (var inputNode in fitParent.InputNodes) {
				child.InputNodes.Add(new Node() {
					Index = inputNode.Index,
					LayerNumber = inputNode.LayerNumber
				});
			}
			foreach (var outputNode in fitParent.OutputNodes) {
				child.OutputNodes.Add(new Node() {
					Index = outputNode.Index,
					LayerNumber = outputNode.LayerNumber
				});
			}
			var linksToCopy = Math.Floor(fitParent.Fitness) == Math.Floor(unfitParent.Fitness) ?
				fitParent.Links.Union(unfitParent.Links).DistinctBy(l => l.Innovation)
				: fitParent.Links;
			foreach (var link in linksToCopy) {
				var fitLink = fitParent.Links.FirstOrDefault(l => l.Innovation == link.Innovation);
				var unfitLink = unfitParent.Links.FirstOrDefault(l => l.Innovation == link.Innovation);
				var childNodes = child.AllNodes;
				var fromNode = childNodes.FirstOrDefault(n => n.Index == link.From.Index);
				if (fromNode == null) {
					fromNode = new Node() {
						Index = link.From.Index,
						LayerNumber = link.From.LayerNumber
					};
					child.HiddenNodes.Add(fromNode);
				}
				var toNode = childNodes.FirstOrDefault(n => n.Index == link.To.Index);
				if (toNode == null) {
					toNode = new Node() {
						Index = link.To.Index,
						LayerNumber = link.To.LayerNumber
					};
					child.HiddenNodes.Add(toNode);
				}
				var parentLink = (Random.value < 0.5 ? unfitLink : fitLink) ?? link;
				child.AddLink(fromNode, toNode, parentLink.Innovation, parentLink.Weight, parentLink.Enabled);
			}
			//Debug.LogWarning($"Networks crossed over nodes: <{string.Join(", ", parent1.AllNodes.Select(n => $"{n.Index}"))}> / <{string.Join(", ", parent2.AllNodes.Select(n => $"{n.Index}"))}> / <{string.Join(", ", child.AllNodes.Select(n => $"{n.Index}"))}>.");
			//Debug.Log($"Networks crossed over: <{string.Join(", ", parent1.Links.Select(l => $"{l.From.Index} - {l.To.Index}"))}> /" +
			//	$" <{string.Join(", ", parent2.Links.Select(l => $"{l.From.Index} - {l.To.Index}"))}> /" +
			//	$" <{string.Join(", ", child.Links.Select(l => $"{l.From.Index} - {l.To.Index}"))}>.");
			return child;
		}

		private void MutateLink(NeuralNetworkNEAT network) {
			if(network?.HiddenNodes.Count == 0) {
				return;
			}

			//Debug.Log($"Mutating link.");

			var node1Pool = network.AllNodes;
			var tryRecurrentLink = Random.value < RecurrentLinkMutationChance;
			for (var i = 0; i < MutationAttempts; i++) {
				var index = Random.Range(0, node1Pool.Count());
				var node1 = node1Pool[index];
				var recurrentLink = network.HiddenNodes.Any(n => n.LayerNumber < node1.LayerNumber) && tryRecurrentLink;
				var node2Pool = (recurrentLink ?
						network.HiddenNodes.Where(n => n.LayerNumber < node1.LayerNumber) :
						node1Pool.Where(n => n.LayerNumber > node1.LayerNumber))
					.Where(n => !network.Links.Any(l => l.From == node1 && l.To == n))
					.ToList();

				if(!node2Pool.Any()) {
					continue;
				}

				index = Random.Range(0, node2Pool.Count());
				var node2 = node2Pool[index];
				var innovationKey = (node1.Index, node2.Index);
				int innovationIndex;
				if (LinkInnovations.ContainsKey(innovationKey)) {
					innovationIndex = LinkInnovations[innovationKey];
				} else {
					innovationIndex = LinkInnovations.Values.Max();
					LinkInnovations[innovationKey] = ++innovationIndex;
				}
				//Debug.LogWarning($"Nodes before: <{string.Join(", ", network.AllNodes.Select(n => $"{n.Index}"))}>.");
				//Debug.Log($"Links before: <{string.Join(", ", network.Links.Select(l => $"{l.From.Index} - {l.To.Index}"))}>.");
				network.AddLink(node1, node2, innovationIndex);
				Debug.Log($"Link mutated: <{node1.Index} - {node2.Index}, {innovationIndex}>.");
				//Debug.Log($"Links after: <{string.Join(", ", network.Links.Select(l => $"{l.From.Index} - {l.To.Index}"))}>.");
				break;
			}
		}

		private void MutateLinkToggle(NeuralNetworkNEAT network) {
			if (network?.Links.Count == 0) {
				return;
			}

			var index = Random.Range(0, network.Links.Count());
			var link = network.Links[index];
			link.Enabled = !link.Enabled;
			Debug.Log($"Link toggle mutated: <{link.Innovation}: {link.Enabled}>.");
		}

		private void MutateNode(NeuralNetworkNEAT network) {
			if (network?.Links.Count == 0) {
				return;
			}

			var index = Random.Range(0, network.Links.Count());
			var link = network.Links[index];
			var newLinks = network.AddNode(link, ++NodeIndexCounter, ++LinkInnovationsCounter);
			LinkInnovations[(newLinks.Item1.From.Index, newLinks.Item1.To.Index)] = LinkInnovationsCounter;
			LinkInnovations[(newLinks.Item2.From.Index, newLinks.Item2.To.Index)] = ++LinkInnovationsCounter;
			//Debug.Log($"Node mutated: <{link.From.Index} - {link.To.Index}, {link.Innovation}>;" +
			//	$" <{newLinks.Item1.From.Index} - {newLinks.Item1.To.Index}, {newLinks.Item1.Innovation};" +
			//	$" {newLinks.Item2.From.Index} - {newLinks.Item2.To.Index}, {newLinks.Item2.Innovation}>.");
		}

		private void MutateWieghtRandom(NeuralNetworkNEAT network) {
			if (network?.Links.Count == 0) {
				return;
			}

			var index = Random.Range(0, network.Links.Count());
			var link = network.Links[index];
			link.Weight = Random.Range(-network.WeightValue, network.WeightValue);
			Debug.Log($"Weight random mutated: <{link.From.Index} - {link.To.Index}, {link.Innovation}, {link.Weight}>");
		}

		private void MutateWieghtShift(NeuralNetworkNEAT network) {
			if (network?.Links.Count == 0) {
				return;
			}

			var index = Random.Range(0, network.Links.Count());
			var link = network.Links[index];
			link.Weight = Mathf.Clamp(link.Weight + Random.Range(-WeightShiftValue, WeightShiftValue), -network.WeightValue, network.WeightValue);
			Debug.Log($"Weight shift mutated: <{link.From.Index} - {link.To.Index}, {link.Innovation}, {link.Weight}>");
		}

		private void ShrinkSpecies(List<NeuralNetworkNEAT> population, Species species, int targetSize) {
			species.Networks = species.Networks.OrderByDescending(n => n.Fitness).ToList();
			while (species.Networks.Count > targetSize) {
				var networkForRemoval = species.Networks.LastOrDefault();
				species.Networks.Remove(networkForRemoval);
				population.Remove(networkForRemoval);
			}
		}

		private void Mutate(NeuralNetworkNEAT network) {
			if (Random.value < NodeMutationChance) {
				MutateNode(network);
			}
			if (Random.value < LinkMutationChance) {
				MutateLink(network);
			}
			if (Random.value < LinkToggleMutationChance) {
				MutateLinkToggle(network);
			}
			if (Random.value < WeightRandomMutationChance) {
				MutateWieghtRandom(network);
			}
			if (Random.value < WeightShiftMutationChance) {
				MutateWieghtShift(network);
			}
		}

		private void Mutate(IEnumerable<NeuralNetworkNEAT> population) {
			foreach (var network in population) {
				Mutate(network);
			}
		}

		private void AddSpeciesToNextGeneration(List<NeuralNetworkNEAT> population, Species species, int speciesPopulation) {
			if (speciesPopulation == 0 || species.Networks?.Count == 0 || population == null) {
				return;
			}

			var orderedNetworks = species.Networks.OrderByDescending(n => n.Fitness);
			//var newSpecies = new List<NeuralNetworkNEAT>() { orderedNetworks.FirstOrDefault() };
			var genePool = species.Networks.ToList();
			while (species.Networks.Count < speciesPopulation) {
				var index = Random.Range(0, genePool.Count);
				var parent1 = genePool[index];
				index = Random.Range(0, genePool.Count);
				var parent2 = genePool[index];
				var child = Crossover(parent1, parent2);
				//Mutate(child);
				species.Networks.Add(child);
			}

			Mutate(species.Networks);

			population.AddRange(species.Networks);
		}

		private void RePopulate() {
			Population = Population.OrderByDescending(n => n.Fitness).ToList();
			TopFitnesses = Population.Select(n => n.Fitness).ToList().GetRange(0, BestAgents);
			if (Species.Any(s => s.AvgFitness > TargetFitness)) {
				Debug.LogWarning($"Training complete!");
				OnTrainingComplete?.Invoke(this);
			}
			Speciate();
			foreach (var species in Species) {
				var targetSize = (int)Math.Ceiling(species.Networks.Count * SpeciesSurvivalRate);
				ShrinkSpecies(Population, species, targetSize);
			}
			var totalAvgFitness = Species.Sum(s => s.AvgFitness);
			var newPopulation = new List<NeuralNetworkNEAT>();
			foreach (var species in Species) {
				var speciesPopulation = (int)Math.Round(PopulationSize * species.AvgFitness / totalAvgFitness);
				speciesPopulation = Math.Min(PopulationSize - newPopulation.Count, speciesPopulation);
				if (Species.LastOrDefault() == species) {
					speciesPopulation = PopulationSize - newPopulation.Count;
				}
				//Debug.LogWarning($"Species size: <{species.Index} - {speciesPopulation}>");
				ShrinkSpecies(Population, species, speciesPopulation);
				AddSpeciesToNextGeneration(newPopulation, species, speciesPopulation);
			}
			if (newPopulation.Count < PopulationSize) {
				GrowPopulation(newPopulation, PopulationSize);
			}
			Species = Species.Where(s => s.Networks.Any()).ToList();
			Debug.LogWarning($"Current species <{Species.Count}: {string.Join(", ", Species.Select(s => s.Networks.Count))}> / {Species.Sum(s => s.Networks.Count)} / {newPopulation.Count} ");
			//foreach (var species in Species.Where(s => s.Networks.Count <= 1).ToList()) {
			//	ShrinkSpecies(newPopulation, species, 0);
			//	Species.Remove(species);
			//}


			CurrentGenome = 0;
			CurrentGeneration++;
			Population = newPopulation;
			OnRepopulated?.Invoke(this);

			//UpdateLinkInnovations();
			//UpdateNodeIndexCounter();
			ResetNodeValues();
			ResetPopulationFitness();
			//Speciate();
		}
	}
}
