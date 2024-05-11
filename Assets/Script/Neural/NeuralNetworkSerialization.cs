using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

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

    public static void SaveToJson(List<NeuralNetwork> population, int generation) {
        if (population == null) {
            Debug.LogError("Empty population for serialization.");
            return;
        }
        var wrap = new CollectionWrapper<NeuralNetworkWrapper>(population.Select(n => new NeuralNetworkWrapper(n)));
        var filePath = $"p_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm")}_{generation:0}_{population.FirstOrDefault().Fitness:0}.nnet";
        File.WriteAllText(filePath, JsonUtility.ToJson((generation, wrap), true));
        Debug.Log($"Saved neural network population: {filePath}");
    }

    public static void SaveToJson(List<NeuralNetwork> population) => SaveToJson(population, 1);

    public static void SaveToJson(NeuralNetwork nnet) => SaveToJson(new List<NeuralNetwork>() { nnet });

    public static (int, List<NeuralNetwork>) LoadFromJson(string filePath) {
        if(!File.Exists(filePath)) {
            Debug.LogError($"Invalid file path: {filePath}");
            return (0, null);
        }

        var wrap = JsonUtility.FromJson<(int, CollectionWrapper<NeuralNetworkWrapper>)>(File.ReadAllText(filePath));
        var unwrap = wrap.Item2?.list?.Select(n => n.Unwrap()).ToList();
        if (wrap.Item2 != null) {
            Debug.Log($"Loaded neural network population: {filePath}");
        } else {
            Debug.LogError($"Unable to load neural network population: {filePath}");
        }
        return (wrap.Item1, unwrap);
    }

    public static string[] GetSavedPopulations() {
        return Directory.GetFiles(".", "*.nnet");
    }

    public static string GetHiddenLayersString(NeuralNetwork nnet) {
        var str = "";
        var neurons = nnet.HiddenLayers.FirstOrDefault().Length;
        var layers = 0;
        foreach (var layer in nnet.HiddenLayers) {
            if (neurons == layer.Length) {
                layers++;
            } else {
                str += $"{neurons}*{layers};";
                neurons = layer.Length;
                layers = 1;
            }
        }
        str += $"{neurons}*{layers};";
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
