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

    public NeuralNetwork(int inputLayerNeuronCount, int outputLayerNeuronCount, int hiddenLayersNeuronCount, int hiddenLayersCount, bool initialize = false) : this(inputLayerNeuronCount, outputLayerNeuronCount) {
        AddHiddenLayers(hiddenLayersNeuronCount, hiddenLayersCount);
        if(initialize) {
            InitializeWeights();
            InitializeBiases();
        }
    }

    public void AddHiddenLayers(int neuronCount, int layerCount) {
        if (neuronCount < 2 || layerCount < 1) {
            throw new ArgumentException($"Invalid hidden layer ({layerCount}) or hidden layer neuron ({neuronCount}) count.");
        }

        for(var i = 0; i < layerCount; i++) {
            HiddenLayers.Add(new float[neuronCount]);
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
                cols = HiddenLayers[0].Length;
            } else if(i == HiddenLayers.Count) {
                rows = HiddenLayers.Last().Length;
                cols = OutputLayer.Length;
            } else {
                rows = cols = HiddenLayers[i].Length;
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
}
