using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GeneticManager : MonoBehaviour
{
    [SerializeField]
    public bool IsActive;
    [SerializeField]
    public MouseController MouseController;

    [Header("Controls")]
    public int PopulationSize = 85;
    [Range(0.0f, 1.0f)]
    public float mutationChance = 0.055f;

    [Header("Crossover Controls")]
    public int bestAgentSelection = 20;
    public int worstAgentSelection = 6;
    public int numberToCrossover = 20;

    private List<NeuralNetwork> genePool;

    private List<NeuralNetwork> population;

    [Header("Public View")]
    public int currentGeneration;
    public int currentGenome;

    public int InputLayerNeuronCount = 3;
    public int OutputLayerNeuronCount = 2;
    public int HiddenLayersNeuronCount = 9;
    public int HiddenLayersCount = 10;

    public string TopFitness;

    public float TargetFitness;
    public Action<List<NeuralNetwork>> OnTrainingComplete;

    public void StartTraining(List<NeuralNetwork> existingPopulation = null) {
        MouseController.OnNeuralDeath += OnNeuralDeath;
        currentGeneration = 0;
        currentGenome = 0;
        TargetFitness = float.MaxValue;
        population = existingPopulation ?? new List<NeuralNetwork>();
        GrowPopulation(population, PopulationSize);
        genePool = new List<NeuralNetwork>();
        OnNeuralDeath();
    }

    private void GrowPopulation(List<NeuralNetwork> population, int targetSize) {
        while(population.Count < targetSize) {
            var nnet = new NeuralNetwork(InputLayerNeuronCount, OutputLayerNeuronCount, HiddenLayersNeuronCount, HiddenLayersCount, true);
            population.Add(nnet);
        }
    }

    private void OnNeuralDeath() {
        if (currentGenome == PopulationSize) {
            RePopulate();
        }
        if (MouseController.IsActive) {
            MouseController.ResetNeural(population[currentGenome++]);
        }
    }

    private void AddNetworksToGenePool(IEnumerable<NeuralNetwork> nnetworks) {
        foreach (var nnet in nnetworks) {
            //var genePoolEntries = Mathf.RoundToInt(nnet.Fitness * 10);
            var genePoolEntries = Mathf.Ceil(nnet.Fitness);
            for (var j = 0; j < genePoolEntries; j++) {
                genePool.Add(nnet);
            }
        }
    }

    private void Crossover(List<NeuralNetwork> newPpopulation) {
        if (genePool.Distinct().Count() < 2) {
            return;
        }
        for (var i = 0; i < numberToCrossover; i += 2) {
            var index = Random.Range(0, genePool.Count);
            //Debug.LogWarning($"genePool Count: {genePool.Count} / Index: {index}");
            var parent1 = genePool[index];
            var remainingGenePool = genePool.Where(g => g != parent1).ToList();
            var parent2 = remainingGenePool[Random.Range(0, remainingGenePool.Count)];
            var child1 = new NeuralNetwork(InputLayerNeuronCount, OutputLayerNeuronCount, HiddenLayersNeuronCount, HiddenLayersCount, true);
            var child2 = new NeuralNetwork(InputLayerNeuronCount, OutputLayerNeuronCount, HiddenLayersNeuronCount, HiddenLayersCount, true);
            for (var j = 0; j < parent1.Weights.Count; j++) {
                //Debug.Log($"Weights: {parent1.Weights.Count} / {child1.Weights.Count}");
                child1.Weights[j] = (float[,])parent1.Weights[j].Clone();
                child2.Weights[j] = (float[,])parent2.Weights[j].Clone();
                /*
                for (var k = 0; k < child1.Weights[j].GetLength(0); k++) {
                    for (var l = 0; l < child1.Weights[j].GetLength(1); l++) {
                        if (Random.Range(0, 2) == 0) {
                            child1.Weights[j][k, l] = parent1.Weights[j][k, l];
                            child2.Weights[j][k, l] = parent2.Weights[j][k, l];
                        } else {
                            child1.Weights[j][k, l] = parent2.Weights[j][k, l];
                            child2.Weights[j][k, l] = parent1.Weights[j][k, l];
                        }
                    }
                }
                */
                
                if (Random.Range(0, 2) == 0) {
                    child1.Weights[j] = (float[,])parent1.Weights[j].Clone();
                    child2.Weights[j] = (float[,])parent2.Weights[j].Clone();
                } else {
                    child1.Weights[j] = (float[,])parent2.Weights[j].Clone();
                    child2.Weights[j] = (float[,])parent1.Weights[j].Clone();
                }
                
            }
            for (var j = 0; j < parent1.Biases.Length; j++) {
                if (Random.Range(0, 2) == 0) {
                    child1.Biases[j] = parent1.Biases[j];
                    child2.Biases[j] = parent2.Biases[j];
                } else {
                    child1.Biases[j] = parent2.Biases[j];
                    child2.Biases[j] = parent1.Biases[j];
                }
            }
            newPpopulation.Add(child1);
            newPpopulation.Add(child2);
        }
    }

    private void MutateArray(float[,] arr) {
        var mutations = Random.Range(0, arr.Length / 2 + 1);
        for(var i = 0; i < mutations; i++) {
            ref var val = ref arr[Random.Range(0, arr.GetLength(0)), Random.Range(0, arr.GetLength(1))];
            val = Mathf.Clamp(val + Random.Range(-1f, 1f), -1f, 1f);
        }
    }

    private void Mutate(List<NeuralNetwork> population) {
        var mutants = new List<NeuralNetwork>();
        foreach(var nnet in population) {
            for(var i = 0; i < nnet.Weights.Count && population.Count < PopulationSize; i++) {
                if(Random.Range(0f, 1f) < mutationChance) {
                    var mutant = new NeuralNetwork(InputLayerNeuronCount, OutputLayerNeuronCount, HiddenLayersNeuronCount, HiddenLayersCount, true);
                    for (var j = 0; j < nnet.Weights.Count; j++) {
                        //Debug.Log($"Weights: {parent1.Weights.Count} / {child1.Weights.Count}");
                        mutant.Weights[j] = (float[,])nnet.Weights[j].Clone();
                    }
                    for (var j = 0; j < nnet.Biases.Length; j++) {
                        mutant.Biases[j] = nnet.Biases[j];
                    }
                    MutateArray(mutant.Weights[i]);
                    mutants.Add(mutant);
                }
            }
            for(var i = 0; i < nnet.Biases.Length && population.Count < PopulationSize; i++) {
                if (Random.Range(0f, 1f) < mutationChance) {
                    var mutant = new NeuralNetwork(InputLayerNeuronCount, OutputLayerNeuronCount, HiddenLayersNeuronCount, HiddenLayersCount, true);
                    for (var j = 0; j < nnet.Weights.Count; j++) {
                        //Debug.Log($"Weights: {parent1.Weights.Count} / {child1.Weights.Count}");
                        mutant.Weights[j] = (float[,])nnet.Weights[j].Clone();
                    }
                    for (var j = 0; j < nnet.Biases.Length; j++) {
                        mutant.Biases[j] = nnet.Biases[j];
                    }
                    mutant.Biases[i] = Mathf.Clamp(nnet.Biases[i] + Random.Range(-1f, 1f), -1f, 1f);
                    mutants.Add(mutant);
                }
            }
        }
        population.AddRange(mutants);
        //Debug.Log($"{mutants.Count} / {population.Count} mutants added.");
    }

    private void RePopulate() {
        population = population.OrderByDescending(n => n.Fitness).ToList();
        var newPopulation = population.GetRange(0, bestAgentSelection).ToList();
        var topFitness = "";
        foreach(var nnet in newPopulation) {
            topFitness += nnet.Fitness + " / ";
        }
        TopFitness = topFitness;
        if (!newPopulation.Any(n => n.Fitness < TargetFitness)) {
            Debug.LogWarning($"Training complete!");
            OnTrainingComplete?.Invoke(population);
        }
        currentGenome = 0;
        genePool.Clear();
        currentGeneration++;
        //avoid plato?
        /*
        if (1f - (newPopulation.LastOrDefault().Fitness / newPopulation.FirstOrDefault().Fitness) < 0.05f && currentGeneration % 10 == 0) {
            Debug.LogWarning($"Purging population.");
            newPopulation = newPopulation.GetRange(0, 1).ToList();
        }
        */
        //
        AddNetworksToGenePool(newPopulation);
        foreach (var nnet in newPopulation) {
            nnet.Fitness = 0;
        }
        
        var worstAgents = population.GetRange(PopulationSize - worstAgentSelection, worstAgentSelection);
        //AddNetworksToGenePool(worstAgents);
        Crossover(newPopulation);
        Mutate(newPopulation);
        GrowPopulation(newPopulation, PopulationSize);
        population = newPopulation;
        
    }
}
