using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Script.Neural.Interfaces {
	public interface INeuralNetwork {
		public float Fitness { get; set; }
		public float[] InputLayer { get; }
		public float[] OutputLayer { get; }

		public List<(int, int)> GetHiddenLayersStructure();
		public void CalculateLayers(IEnumerable<float> input);
		public INeuralNetwork Clone(bool cloneCurrentValues = false);
	}
}
