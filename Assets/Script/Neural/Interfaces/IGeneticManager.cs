using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Script.Neural.Interfaces {
	public interface IGeneticManager {
		public Action<IGeneticManager> OnTrainingComplete { get; set; }
		public Action<IGeneticManager> OnRepopulated { get; set; }
		public Action<IGeneticManager> OnNextAgentStart { get; set; }
		public int BestAgents { get; set; }
		public int CurrentGeneration { get; set; }
		public int CurrentGenome { get; set; }
		public List<float> TopFitnesses { get; set; }
		public float TargetFitness { get; set; }
		public int PopulationSize { get; set; }
		public List<INeuralNetwork> PopulationInterface { get; }
		public void ClearSubscriptions();
	}
}
