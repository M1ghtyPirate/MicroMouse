using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NeuralNetwork
{
    public float[] InputLayer;
    public List<float[]> HiddenLayers;
    public float[] OutputLayer;
    public List<float[,]> Weights;
    public float[] Biases;
    public float Fitness;

    public NeuralNetwork(int inputLayerNeuronCount, int outputLayerNeuronCount) {
        if(inputLayerNeuronCount < 1 || outputLayerNeuronCount < 2) {
            throw new ArgumentException($"Invalid input ({inputLayerNeuronCount}) or output ({outputLayerNeuronCount}) layer neuron count.");
        }

        InputLayer = new float[inputLayerNeuronCount];
        OutputLayer = new float[outputLayerNeuronCount];
        HiddenLayers = new List<float[]>();
    }

    public NeuralNetwork(int inputLayerNeuronCount, int outputLayerNeuronCount, int hiddenLayersNeuronCount, int hiddenLayersCount, bool initialize = false) 
        : this(inputLayerNeuronCount, outputLayerNeuronCount, new List<(int, int)> { (hiddenLayersNeuronCount, hiddenLayersCount) }, initialize) {
    }

    public NeuralNetwork(int inputLayerNeuronCount, int outputLayerNeuronCount, IEnumerable<(int, int)> layersStructure, bool initialize = false) : this(inputLayerNeuronCount, outputLayerNeuronCount) {
        AddHiddenLayers(layersStructure);
        if (initialize) {
            InitializeWeights();
            InitializeBiases();
        }
    }

    public void AddHiddenLayers(int neuronCount, int layerCount) {
        if (neuronCount < 1 || layerCount < 1) {
            throw new ArgumentException($"Invalid hidden layer ({layerCount}) or hidden layer neuron ({neuronCount}) count.");
        }

        for(var i = 0; i < layerCount; i++) {
            HiddenLayers.Add(new float[neuronCount]);
        }
    }

    public void AddHiddenLayers(IEnumerable<(int, int)> layersStructure, bool initialize = false) {
        foreach(var layers in layersStructure) {
            AddHiddenLayers(layers.Item1, layers.Item2);
        }
        if (initialize) {
            InitializeWeights();
            InitializeBiases();
        }
    }

    public bool InitializeWeights(bool randomizeValues = true) {
        if(!HiddenLayers.Any()) {
            Debug.Log($"No hidden layers were added.");
            return false;
        }

        Weights = new List<float[,]>();
        for(var i = 0; i < HiddenLayers.Count + 1; i++) {
            int rows;
            int cols;
            if(i == 0) {
                rows = InputLayer.Length;
                cols = HiddenLayers[i].Length;
            } else if(i == HiddenLayers.Count) {
                rows = HiddenLayers[i - 1].Length;
                cols = OutputLayer.Length;
            } else {
                rows = cols = HiddenLayers[i - 1].Length;
                cols = HiddenLayers[i].Length;
            }
            Weights.Add(new float[rows, cols]);
            if(randomizeValues) {
                var weight = Weights.Last();
                for(var j = 0; j < rows; j++) {
                    for(var k = 0; k < cols; k++) {
                        weight[j, k] = UnityEngine.Random.Range(-1f, 1f);
                    }
                }
            }
        }
        return true;
    }

    public bool InitializeBiases(bool randomizeValues = true) {
        if (!HiddenLayers.Any()) {
            Debug.Log($"No hidden layers were added.");
            return false;
        }

        Biases = new float[HiddenLayers.Count + 1];
        if(randomizeValues) {
            for (var i = 0; i < Biases.Length; i++) {
                Biases[i] = UnityEngine.Random.Range(-1f, 1f);
            }
        }
        return true;
    }

    public void CalculateLayers(IEnumerable<float> input) {
        if(Weights == null || Weights.Count != HiddenLayers.Count + 1) {
            InitializeWeights();
        }
        if (Biases == null || Biases.Length != HiddenLayers.Count + 1) {
            InitializeBiases();
        }
        var inputArr = input?.ToArray();
        if(inputArr == null || inputArr.Length != InputLayer.Length) {
            throw new ArgumentException($"Invalid input length ({inputArr?.Length + ""}).");
        }

        InputLayer = Matrices.Tanh(inputArr);
        for(var i = 0; i < HiddenLayers.Count + 1; i++) {
            float[] inputVector;
            float[] outputVector;
            if (i == 0) {
                inputVector = InputLayer;
                outputVector = HiddenLayers[i];
            } else if(i == HiddenLayers.Count) {
                inputVector = HiddenLayers[i - 1];
                outputVector = OutputLayer;
            } else {
                inputVector = HiddenLayers[i - 1];
                outputVector = HiddenLayers[i];
            }
            var resultVector = Matrices.Multiply(inputVector, Weights[i]);
            if (Biases != null) {
                resultVector = Matrices.Add(resultVector, Biases[i]);
            }
            resultVector = Matrices.Tanh(resultVector);
            for (var j = 0; j < outputVector.Length; j++) {
                outputVector[j] = resultVector[j];
            }
        }
    }

    public List<(int, int)> GetHiddenLayersStructure() {
        var structure = new List<(int, int)>();
        var neurons = HiddenLayers.FirstOrDefault().Length;
        var layers = 0;
        foreach (var layer in HiddenLayers) {
            if (neurons == layer.Length) {
                layers++;
            } else {
                structure.Add((neurons, layers));
                neurons = layer.Length;
                layers = 1;
            }
        }
        structure.Add((neurons, layers));
        return structure;
    }

    public NeuralNetwork Clone(bool cloneCurrentValues = false) {
        var clone = new NeuralNetwork(InputLayer.Length, OutputLayer.Length, GetHiddenLayersStructure(), true);
        for (var i = 0; i < Weights.Count; i++) {
            //Debug.Log($"Weights: {parent1.Weights.Count} / {child1.Weights.Count}");
            clone.Weights[i] = (float[,])Weights[i].Clone();
        }
        clone.Biases = (float[])Biases.Clone();
        if(cloneCurrentValues) {
            clone.Fitness = Fitness;
            clone.InputLayer = (float[])InputLayer.Clone();
            for (var i = 0; i < HiddenLayers.Count; i++) {
                //Debug.Log($"Weights: {parent1.Weights.Count} / {child1.Weights.Count}");
                clone.HiddenLayers[i] = (float[])HiddenLayers[i].Clone();
            }
            clone.OutputLayer = (float[])OutputLayer.Clone();
        }
        return clone;
    }
}
