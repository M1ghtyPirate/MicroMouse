using Assets.Script.Neural.Interfaces;
using Assets.Script.Neural.Models.NEAT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.StandaloneInputModule;

namespace Assets.Script.Neural {
	public class NeuralNetworkNEAT : INeuralNetwork {
		public float Fitness { get; set; }
		public float[] InputLayer => InputNodes?.Select(n => n.Value).ToArray();
		public float[] OutputLayer => OutputNodes?.Select(n => n.Value).ToArray();

		public List<Node> HiddenNodes { get; set; }
		public List<Node> InputNodes { get; set; }
		public List<Node> OutputNodes { get; set; }
		public List<Node> AllNodes => InputNodes.Union(HiddenNodes).Union(OutputNodes).ToList();

		public List<Link> Links { get; set; }

		public float WeightValue { get; set; } = 1f;

		public NeuralNetworkNEAT() {
			HiddenNodes = new List<Node>();
			InputNodes = new List<Node>();
			OutputNodes = new List<Node>();
			Links = new List<Link>();
		}

		public NeuralNetworkNEAT(int inputLayerNeuronCount, int outputLayerNeuronCount) : this() {
			if (inputLayerNeuronCount < 1 || outputLayerNeuronCount < 1) {
				throw new ArgumentException($"Invalid input ({inputLayerNeuronCount}) or output ({outputLayerNeuronCount}) layer neuron count.");
			}
			var innovationCounter = 0;

			for(var i = 0; i < inputLayerNeuronCount; i++) {
				var inputNode = new Node() { LayerNumber = int.MinValue, Index = i };
				InputNodes.Add(inputNode);
			}
			for (var i = 0; i < outputLayerNeuronCount; i++) {
				var outputNode = new Node() { LayerNumber = int.MaxValue, Index = inputLayerNeuronCount + i };
				OutputNodes.Add(outputNode);
				foreach(var node in InputNodes) {
					AddLink(node, outputNode, innovationCounter++);
				}
			}
			//Debug.LogWarning($"Network created: <{string.Join(", ", AllNodes.Select(n => $"{n.Index}"))}>.");
			//Debug.Log($"Network created: <{string.Join(", ", Links.Select(l => $"{l.From.Index} - {l.To.Index}"))}>.");
		}

		public void CalculateLayers(IEnumerable<float> input) {
			//var inputArr = input?.ToArray();
			var inputArr = Matrices.Tanh(input?.ToArray());
			if (inputArr == null || inputArr.Length != InputLayer.Length) {
				throw new ArgumentException($"Invalid input length ({inputArr?.Length + ""}).");
			}

			for (var i = 0; i < inputArr.Length; i++) {
				InputNodes[i].Value = inputArr[i];
			}

			var orderedNodes = HiddenNodes.Union(OutputNodes).OrderBy(n => n.LayerNumber);
			foreach (var node in orderedNodes) {
				//node.Value = Links.Where(l => l.Enabled && l.To == node).Sum(l => l.From.Value * l.Weight);
				node.Value = Matrices.Tanh(Links.Where(l => l.Enabled && l.To == node).Sum(l => l.From.Value * l.Weight));
			}
		}

		public INeuralNetwork Clone(bool cloneCurrentValues = false) {
			var clone = new NeuralNetworkNEAT();
			var replacementNodes = new Dictionary<Node, Node>();
			foreach (var node in InputNodes) {
				var cloneNode = new Node() {
					Index = node.Index,
					LayerNumber = node.LayerNumber,
					Value = cloneCurrentValues ? node.Value : 0
				};
				clone.InputNodes.Add(cloneNode);
				replacementNodes[node] = cloneNode;
			}
			foreach (var node in OutputNodes) {
				var cloneNode = new Node() {
					Index = node.Index,
					LayerNumber = node.LayerNumber,
					Value = cloneCurrentValues ? node.Value : 0
				};
				clone.OutputNodes.Add(cloneNode);
				replacementNodes[node] = cloneNode;
			}
			foreach (var node in HiddenNodes) {
				var cloneNode = new Node() {
					LayerNumber = node.LayerNumber,
					Value = cloneCurrentValues ? node.Value : 0,
					Index = node.Index
				};
				clone.HiddenNodes.Add(cloneNode);
				replacementNodes[node] = cloneNode;
			}
			foreach (var link in Links) {
				clone.AddLink(replacementNodes[link.From], replacementNodes[link.To], link.Innovation, link.Weight, link.Enabled);
			}
			clone.Fitness = cloneCurrentValues ? Fitness : 0;
			//Debug.Log($"Network cloned: <{string.Join(", ", Links.Select(l => $"{l.From.Index} - {l.To.Index}"))}> / <{string.Join(", ", clone.Links.Select(l => $"{l.From.Index} - {l.To.Index}"))}>.");
			return clone;
		}

		public List<(int, int)> GetHiddenLayersStructure() {
			var structure = new List<(int, int)>();
			if (!HiddenNodes.Any()) {
				structure.Add((0, 0));
				return structure;
			}

			var hiddenLayers = HiddenNodes
				.OrderBy(n => n.LayerNumber)
				.GroupBy(n => n.LayerNumber);
			var neurons = hiddenLayers.FirstOrDefault().Count();
			var layers = 0;
			foreach (var layer in hiddenLayers) {
				if (neurons == layer.Count()) {
					layers++;
				} else {
					structure.Add((neurons, layers));
					neurons = layer.Count();
					layers = 1;
				}
			}
			structure.Add((neurons, layers));
			return structure;
		}

		public (Link, Link) AddNode(Link link, int nodeIndex, int linkInnovationIndex) {
			if (!Links.Contains(link)) {
				throw new InvalidOperationException("Link is not a part of the network.");
			}

			link.Enabled = false;
			var node = new Node() {
				Index = nodeIndex,
				LayerNumber = link.From.LayerNumber / 2 + link.To.LayerNumber / 2
			};
			HiddenNodes.Add(node);
			var newLinkWeighted = AddLink(link.From, node, linkInnovationIndex, 1);
			var newLinkPassthrough = AddLink(node, link.To, ++linkInnovationIndex, link.Weight);

			return (newLinkWeighted, newLinkPassthrough);
		}

		public Link AddLink(Node fromNode, Node toNode, int linkInnovationIndex, float? weight = null, bool? enabled = null) {
			var nodePool = AllNodes;
			if (!nodePool.Contains(fromNode) || !nodePool.Contains(toNode)) {
				throw new InvalidOperationException("Nodes are not a part of the network.");
			}
			if (Links.Any(l => l.From == fromNode && l.To == toNode)) {
				throw new InvalidOperationException("Link already exists.");
			}

			var newLink = new Link() {
				Enabled = enabled ?? true,
				From = fromNode,
				To = toNode,
				Weight = weight ?? UnityEngine.Random.Range(-WeightValue, WeightValue),
				Innovation = linkInnovationIndex
			};
			Links.Add(newLink);
			return newLink;
		}
	}
}
