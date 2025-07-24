using Assets.Script.Neural;
using Assets.Script.Neural.Interfaces;
using Assets.Script.Neural.Models.NEAT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public static class NeuralNetworkSerialization
{
	[Serializable]
	private class CollectionWrapper<T> {
		public List<T> list;

		public CollectionWrapper(IEnumerable<T> collection) {
			list = collection.ToList();
		}
	}

	[Serializable]
	private class NeuralNetworkWrapper {
		public CollectionWrapper<float> InputLayer;
		public CollectionWrapper<CollectionWrapper<float>> HiddenLayers;
		public CollectionWrapper<float> OutputLayer;
		public CollectionWrapper<CollectionWrapper<CollectionWrapper<float>>> Weights;
		public CollectionWrapper<float> Biases;
		public float Fitness;

		public NeuralNetworkWrapper(NeuralNetwork nnet) {
			InputLayer = new CollectionWrapper<float>(nnet.InputLayer);
			HiddenLayers = new CollectionWrapper<CollectionWrapper<float>>(nnet.HiddenLayers.Select(l => new CollectionWrapper<float>(l)));
			OutputLayer = new CollectionWrapper<float>(nnet.OutputLayer);
			Weights = new CollectionWrapper<CollectionWrapper<CollectionWrapper<float>>>(nnet.Weights.Select(w => WrapSquareArray(w)));
			Biases = new CollectionWrapper<float>(nnet.Biases);
			Fitness = nnet.Fitness;
		}

		public NeuralNetwork Unwrap() {
			var nnet = new NeuralNetwork(InputLayer.list.Count, OutputLayer.list.Count);
			nnet.InputLayer = InputLayer.list.ToArray();
			nnet.HiddenLayers = HiddenLayers.list.Select(l => l.list.ToArray()).ToList();
			nnet.Weights = Weights.list.Select(w => UnwrapSquareArray(w)).ToList();
			nnet.OutputLayer = OutputLayer.list.ToArray();
			nnet.Biases = Biases.list.ToArray();
			nnet.Fitness = Fitness;
			return nnet;
		}
	}

	[Serializable]
	private class LinkWrapper {
		public int From;
		public int To;
		public float Weight;
		public int Innovation;
		public bool Enabled;

		public LinkWrapper(Link link) {
			From = link.From.Index;
			To = link.To.Index;
			Weight = link.Weight;
			Innovation = link.Innovation;
			Enabled = link.Enabled;
		}

		public Link Unwrap(IEnumerable<Node> nodes) {
			var link = new Link();
			link.From = nodes.FirstOrDefault(n => n.Index == From);
			link.To = nodes.FirstOrDefault(n => n.Index == To);
			link.Weight = Weight;
			link.Innovation = Innovation;
			link.Enabled = Enabled;
			return link;
		}
	}

	[Serializable]
	private class NeuralNetworkNEATWrapper {
		public CollectionWrapper<Node> HiddenNodes;
		public CollectionWrapper<Node> InputNodes;
		public CollectionWrapper<Node> OutputNodes;
		public CollectionWrapper<LinkWrapper> Links;
		public float Fitness;

		public NeuralNetworkNEATWrapper(NeuralNetworkNEAT nnet) {
			HiddenNodes = new CollectionWrapper<Node>(nnet.HiddenNodes);
			InputNodes = new CollectionWrapper<Node>(nnet.InputNodes);
			OutputNodes = new CollectionWrapper<Node>(nnet.OutputNodes);
			Links = new CollectionWrapper<LinkWrapper>(nnet.Links.Select(l => new LinkWrapper(l)));
			Fitness = nnet.Fitness;
		}

		public NeuralNetworkNEAT Unwrap() {
			var nnet = new NeuralNetworkNEAT();
			nnet.HiddenNodes = HiddenNodes.list;
			nnet.InputNodes = InputNodes.list;
			nnet.OutputNodes = OutputNodes.list;
			var nodes = nnet.AllNodes;
			nnet.Links = Links.list.Select(l => l.Unwrap(nodes)).ToList();
			nnet.Fitness = Fitness;
			return nnet;
		}

	}

	private static CollectionWrapper<CollectionWrapper<T>> WrapSquareArray<T>(T[,] arr) {
		var rows = new List<CollectionWrapper<T>>();
		for (var i = 0; i < arr.GetLength(0); i++) {
			var row = new List<T>();
			for (var j = 0; j< arr.GetLength(1); j++) {
				row.Add(arr[i, j]);
			}
			rows.Add(new CollectionWrapper<T>(row));
		}
		return new CollectionWrapper<CollectionWrapper<T>>(rows);
	}

	private static T[,] UnwrapSquareArray<T>(CollectionWrapper<CollectionWrapper<T>> arr) {
		var rows = arr.list.Count;
		var cols = arr.list.FirstOrDefault().list.Count;
		var unwrap = new T[rows, cols];
		for (var i = 0; i < rows; i++) {
			for (var j = 0; j < cols; j++) {
				unwrap[i, j] = arr.list[i].list[j];
			};
		}
		return unwrap;
	}

	public static void SaveToJson(List<INeuralNetwork> population, int generation) {
		if (population == null) {
			Debug.LogError("Empty population for serialization.");
			return;
		}
		string filePath;
		string jsonString;
		if (population.FirstOrDefault() is NeuralNetwork) {
			var nnWrap = new CollectionWrapper<NeuralNetworkWrapper>(population.Select(n => new NeuralNetworkWrapper((NeuralNetwork)n)));
			filePath = "nn";
			jsonString = JsonUtility.ToJson((population.FirstOrDefault().GetType().Name, generation, nnWrap), true);
		} else if (population.FirstOrDefault() is NeuralNetworkNEAT) {
			var neatWrap = new CollectionWrapper<NeuralNetworkNEATWrapper>(population.Select(n => new NeuralNetworkNEATWrapper((NeuralNetworkNEAT)n)));
			filePath = "neat";
			jsonString = JsonUtility.ToJson((population.FirstOrDefault().GetType().Name, generation, neatWrap), true);
		} else {
			throw new ArgumentException("Unknown network population type.");
		}
		filePath = $"{filePath}_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm")}_{generation:0}_{population.FirstOrDefault().Fitness:0}.nnet";

		File.WriteAllText(filePath, jsonString);
		Debug.Log($"Saved neural network population: {filePath}");
	}

	public static void SaveToJson(List<INeuralNetwork> population) => SaveToJson(population, 1);

	public static (string, int, List<INeuralNetwork>) LoadFromJson(string filePath) {
		if(!File.Exists(filePath)) {
			Debug.LogError($"Invalid file path: {filePath}");
			return (null, 0, null);
		}

		var jsonString = File.ReadAllText(filePath);
		var wrap = JsonUtility.FromJson<(string, int, object)>(jsonString);
		List<INeuralNetwork> unwrap;
		if(wrap.Item1 == nameof(NeuralNetwork)) {
			unwrap = JsonUtility.FromJson<(string, int, CollectionWrapper<NeuralNetworkWrapper>)>(jsonString).Item3?.list?.Select(n => (INeuralNetwork)n.Unwrap()).ToList();
		} else if (wrap.Item1 == nameof(NeuralNetworkNEAT)) {
			unwrap = JsonUtility.FromJson<(string, int, CollectionWrapper<NeuralNetworkNEATWrapper>)>(jsonString).Item3?.list?.Select(n => (INeuralNetwork)n.Unwrap()).ToList();
		} else {
			throw new ArgumentException("Unknown network population type.");
		}
		if (unwrap != null) {
			Debug.Log($"Loaded neural network population: {filePath}");
		} else {
			Debug.LogError($"Unable to load neural network population: {filePath}");
		}
		return (wrap.Item1, wrap.Item2, unwrap);
	}

	public static string[] GetSavedPopulations() {
		return Directory.GetFiles(".", "*.nnet");
	}

	public static string GetHiddenLayersString(INeuralNetwork nnet) {
		var str = "";
		var structure = nnet?.GetHiddenLayersStructure() ?? new List<(int, int)>();
		foreach(var layerBatch in structure) {
			str += $"{layerBatch.Item1}*{layerBatch.Item2};";
		}
		return str;
	}

	public static  List<(int, int)> ParseHiddenLayersString(string str) {
		if (string.IsNullOrEmpty(str)) {
			return null;
		}
		var layers = str
			.Split(';')
			.Where(l => !string.IsNullOrEmpty(l))
			.Select(l => l.Split('*')
			.Where(v => !string.IsNullOrEmpty(l))
			.Select(v => int.Parse(v)));
		if (layers.Any(l => l.Count() != 2 || l.Any(v => v < 1))) {
			Debug.LogError($"Unable to parse hidden layers structure: {str}");
			return null;
		}
		return layers.Select(l => (l.FirstOrDefault(), l.LastOrDefault())).ToList();
	}
}
