using Assets.Script.Neural.Interfaces;
using Assets.Script.Neural.Models.NEAT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;

namespace Assets.Script.Neural {
	public class NeuralNetworkNEAT : INeuralNetwork {
		public float Fitness { get; set; }
		public float[] InputLayer => InputNodes?.Select(n => n.Value).ToArray();
		public float[] OutputLayer => OutputNodes?.Select(n => n.Value).ToArray();

		public List<Node> HiddenNodes { get; set; }
		public List<Node> InputNodes { get; set; }
		public List<Node> OutputNodes { get; set; }
		public List<Link> Links { get; set; }

		public NeuralNetworkNEAT(int inputLayerNeuronCount, int outputLayerNeuronCount) {
			if (inputLayerNeuronCount < 1 || outputLayerNeuronCount < 1) {
				throw new ArgumentException($"Invalid input ({inputLayerNeuronCount}) or output ({outputLayerNeuronCount}) layer neuron count.");
			}

			HiddenNodes = new List<Node>();
			InputNodes =  new List<Node>();
			OutputNodes = new List<Node>();
			var innovationCounter = 0;

			for(var i = 0; i < inputLayerNeuronCount; i++) {
				var inputNode = new Node() { LayerNumber = int.MinValue, Index = i };
				InputNodes.Add(inputNode);
				for (var j = 0; j < outputLayerNeuronCount; j++) {
					var outputNode = new Node() { LayerNumber = int.MaxValue, Index = inputLayerNeuronCount + j };
					OutputNodes.Add(outputNode);
					Links.Add(new Link() {
						From = inputNode,
						To = outputNode,
						Weight = 1,
						Enabled = true, 
						Innovation = innovationCounter++
					});
				}
			}
		}

		public void CalculateLayers(IEnumerable<float> input) {
			var inputArr = input?.ToArray();
			if (inputArr == null || inputArr.Length != InputLayer.Length) {
				throw new ArgumentException($"Invalid input length ({inputArr?.Length + ""}).");
			}

			for (var i = 0; i < inputArr.Length; i++) {
				InputNodes[i].Value = inputArr[i];
			}

			var orderedNodes = HiddenNodes.Union(OutputNodes).OrderBy(n => n.LayerNumber);
			foreach (var node in orderedNodes) {
				node.Value = Links.Where(l => l.Enabled && l.To == node).Sum(l => l.From.Value * l.Weight);
			}
		}

		public INeuralNetwork Clone(bool cloneCurrentValues = false) {
			var clone = new NeuralNetworkNEAT(InputLayer.Length, OutputLayer.Length);
			var replacementNodes = new Dictionary<Node, Node>();
			for (var i = 0; i < InputNodes.Count; i++) {
				replacementNodes[InputNodes[i]] = clone.InputNodes[i];
				clone.InputNodes[i].Value = cloneCurrentValues ? InputNodes[i].Value : 0;
			}
			for (var i = 0; i < OutputNodes.Count; i++) {
				replacementNodes[OutputNodes[i]] = clone.OutputNodes[i];
				clone.OutputNodes[i].Value = cloneCurrentValues ? OutputNodes[i].Value : 0;
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
				clone.Links.Add(new Link() { 
					From = replacementNodes[link.From],
					To = replacementNodes[link.To],
					Weight = link.Weight,
					Enabled = link.Enabled,
					Innovation = link.Innovation
				});
			}
			clone.Fitness = cloneCurrentValues ? Fitness : 0;
			return clone;
		}

		public List<(int, int)> GetHiddenLayersStructure() {
			return HiddenNodes
				.OrderBy(n => n.LayerNumber)
				.GroupBy(n => n.LayerNumber)
				.Select(l => (l.Count(), 1))
				.ToList();
		}

		public (Link, Link) AddNode(Link link, ref int nodeIndexCounter, ref int linkInnovationCounter) {
			if (!Links.Contains(link)) {
				throw new InvalidOperationException("Link is not a part of the network.");
			}

			link.Enabled = false;
			var node = new Node() {
				Index = ++nodeIndexCounter,
				LayerNumber = link.From.LayerNumber / 2 + link.To.LayerNumber / 2
			};
			HiddenNodes.Add(node);
			var newLinkWeighted = new Link() {
				Enabled = true,
				From = link.From,
				To = node,
				Weight = link.Weight,
				Innovation = ++linkInnovationCounter
			};
			var newLinkPassthrough = new Link() {
				Enabled = true,
				From = node,
				To = link.To,
				Weight = 1,
				Innovation = ++linkInnovationCounter
			};
			Links.Add(newLinkWeighted);
			Links.Add(newLinkPassthrough);

			return (newLinkWeighted, newLinkPassthrough);
		}

		public Link AddLink(Node fromNode, Node toNode, ref int linkInnovationCounter) {
			if (!HiddenNodes.Contains(fromNode) || !HiddenNodes.Contains(toNode)) {
				throw new InvalidOperationException("Nodes are not a part of the network.");
			}
			if (Links.Any(l => l.From == fromNode && l.To == toNode)) {
				throw new InvalidOperationException("Link already exists.");
			}

			var newLink = new Link() {
				Enabled = true,
				From = fromNode,
				To = toNode,
				Weight = 1,
				Innovation = ++linkInnovationCounter
			};
			return newLink;
		}
	}
}
