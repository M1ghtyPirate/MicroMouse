using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Script.Neural.Models.NEAT {
	public class Species {
		public List<NeuralNetworkNEAT> Networks { get; set; }
		public int Index { get; set; }
		public NeuralNetworkNEAT Representative { get; set; }
		public float AvgFitness => (Networks?.Sum(n => n.Fitness) / Networks?.Count) ?? 0;
	}
}
