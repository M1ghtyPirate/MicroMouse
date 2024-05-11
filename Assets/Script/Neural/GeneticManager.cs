using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GeneticManager
{
    public bool IsActive;
    public MouseController MouseController;

    public int PopulationSize = 85;
    public float MutationChance;

    public int BestAgents = 20;
    public int WorstAgents = 6;
    public int Children = 20;

    private List<NeuralNetwork> GenePool;

    public List<NeuralNetwork> Population;

    public int currentGeneration;
    public int currentGenome;

    public int InputLayerNeuronCount = 3;
    public int OutputLayerNeuronCount = 2;
    public List<(int, int)> HiddenLayerStructure;

    public List<float> TopFitnesses;

    public float TargetFitness;
    public Action<GeneticManager> OnTrainingComplete;
    public Action<GeneticManager> OnRepopulated;
    public Action<GeneticManager> OnNextAgentStart;

    public void StartTraining(List<NeuralNetwork> existingPopulation = null, int generation = 1, IEnumerable<(int, int)> layersStructure = null, float mutationChance = 0.055f) {
        HiddenLayerStructure = layersStructure?.ToList() ?? new List<(int, int)> { (9, 2) };
        MouseController.OnNeuralDeath += OnNeuralDeath;
        currentGeneration = generation;
        currentGenome = 0;
        MutationChance = mutationChance;
        TargetFitness = float.MaxValue;
        Population = existingPopulation ?? new List<NeuralNetwork>();
        GrowPopulation(Population, PopulationSize);
        GenePool = new List<NeuralNetwork>();
        OnNeuralDeath();
    }

    private void GrowPopulation(List<NeuralNetwork> population, int targetSize) {
        while(population.Count < targetSize) {
            var nnet = new NeuralNetwork(InputLayerNeuronCount, OutputLayerNeuronCount, HiddenLayerStructure, true);
            population.Add(nnet);
        }
    }

    private void OnNeuralDeath() {
        if (currentGenome == Population.Count) {
            RePopulate();
        }
        if (MouseController.IsActive) {
            MouseController.Reset(Population[currentGenome++]);
            OnNextAgentStart?.Invoke(this);
        }
    }

    private void AddNetworksToGenePool(IEnumerable<NeuralNetwork> nnetworks) {
        foreach (var nnet in nnetworks) {
            var genePoolEntries = Mathf.Ceil(nnet.Fitness);
            for (var j = 0; j < genePoolEntries; j++) {
                GenePool.Add(nnet);
            }
        }
    }

    private void Crossover(List<NeuralNetwork> population) {
        if (GenePool.Distinct().Count() < 2) {
            return;
        }
        var children = new List<NeuralNetwork>();
        for (var i = 0; i < Children && population.Count + children.Count + 1 < PopulationSize; i += 2) {
            var index = Random.Range(0, GenePool.Count);
            //Debug.LogWarning($"genePool Count: {genePool.Count} / Index: {index}");
            var parent1 = GenePool[index];
            var remainingGenePool = GenePool.Where(g => g != parent1).ToList();
            var parent2 = remainingGenePool[Random.Range(0, remainingGenePool.Count)];
            var child1 = new NeuralNetwork(InputLayerNeuronCount, OutputLayerNeuronCount, HiddenLayerStructure, true);
            var child2 = new NeuralNetwork(InputLayerNeuronCount, OutputLayerNeuronCount, HiddenLayerStructure, true);
            for (var j = 0; j < parent1.Weights.Count; j++) {
                //Debug.Log($"Weights: {parent1.Weights.Count} / {child1.Weights.Count}");
                child1.Weights[j] = (float[,])parent1.Weights[j].Clone();
                child2.Weights[j] = (float[,])parent2.Weights[j].Clone();
                
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
            children.Add(child1);
            children.Add(child2);
        }
        population.AddRange(children);
        Debug.Log($"Children added: {children.Count} / {population.Count}");
    }

    private void MutateArray(float[,] arr) {
        var mutations = Random.Range(0, arr.Length / 2 + 1);
        for(var i = 0; i < mutations; i++) {
            ref var val = ref arr[Random.Range(0, arr.GetLength(0)), Random.Range(0, arr.GetLength(1))];
            val = Mathf.Clamp(val + Random.Range(-1f, 1f), -1f, 1f);
        }
    }

    private NeuralNetwork GetWeightMutant(NeuralNetwork nnet, int weightIndex) {
        var mutant = nnet.Clone();
        MutateArray(mutant.Weights[weightIndex]);
        //Debug.Log($"Weights[{weightIndex}] mutant created.");
        return mutant;
    }

    private NeuralNetwork GetBiasMutant(NeuralNetwork nnet, int biasIndex) {
        var mutant = nnet.Clone();
        mutant.Biases[biasIndex] = Mathf.Clamp(nnet.Biases[biasIndex] + Random.Range(-1f, 1f), -1f, 1f);
        //Debug.Log($"Biases[{biasIndex}] mutant created.");
        return mutant;
    }

    private void Mutate(List<NeuralNetwork> population) {
        var mutants = new List<NeuralNetwork>();
        foreach(var nnet in population) {
            for (var i = 0; i < nnet.Weights.Count && population.Count + mutants.Count < PopulationSize; i++) {
                if (Random.Range(0f, 1f) < MutationChance) {
                    if (Random.Range(0, 2) == 0) {
                        mutants.Add(GetWeightMutant(nnet, i));
                    } else {
                        mutants.Add(GetBiasMutant(nnet, i));
                    }
                }
            }
        }
        population.AddRange(mutants);
        Debug.Log($"{mutants.Count} / {population.Count} mutants added.");
    }

    private void RePopulate() {
        Population = Population.OrderByDescending(n => n.Fitness).ToList();
        var newPopulation = Population.GetRange(0, BestAgents).ToList();
        TopFitnesses = newPopulation.Select(n => n.Fitness).ToList();
        if (!newPopulation.Any(n => n.Fitness < TargetFitness)) {
            Debug.LogWarning($"Training complete!");
            OnTrainingComplete?.Invoke(this);
        }
        currentGenome = 0;
        GenePool.Clear();
        currentGeneration++;
        AddNetworksToGenePool(newPopulation);
        foreach (var nnet in newPopulation) {
            nnet.Fitness = 0;
        }
        
        //var worstAgents = Population.GetRange(Population.Count - WorstAgentSelection, WorstAgentSelection);
        //AddNetworksToGenePool(worstAgents);
        Crossover(newPopulation);
        Mutate(newPopulation);
        GrowPopulation(newPopulation, PopulationSize);
        Population = newPopulation;
        OnRepopulated?.Invoke(this);
    }
}
