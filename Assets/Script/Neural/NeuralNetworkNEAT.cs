using Assets.Script.Neural.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Script.Neural {
	public class NeuralNetworkNEAT : INeuralNetwork {
		public float Fitness { get; set; }
		public float[] InputLayer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public float[] OutputLayer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public NeuralNetworkNEAT(int inputLayerNeuronCount, int outputLayerNeuronCount) {
			if (inputLayerNeuronCount < 1 || outputLayerNeuronCount < 1) {
				throw new ArgumentException($"Invalid input ({inputLayerNeuronCount}) or output ({outputLayerNeuronCount}) layer neuron count.");
			}

			InputLayer = new float[inputLayerNeuronCount];
			OutputLayer = new float[outputLayerNeuronCount];
		}

		public void CalculateLayers(IEnumerable<float> input) {
			var inputArr = input?.ToArray();
			if (inputArr == null || inputArr.Length != InputLayer.Length) {
				throw new ArgumentException($"Invalid input length ({inputArr?.Length + ""}).");
			}

			throw new NotImplementedException();
		}

		public NeuralNetwork Clone(bool cloneCurrentValues = false) {
			throw new NotImplementedException();
		}

		public List<(int, int)> GetHiddenLayersStructure() {
			throw new NotImplementedException();
		}
	}
}
