using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Script.Neural.Models.NEAT {
	public class Link {
		public Node From { get; set; }
		public Node To { get; set; }
		public float Weight { get; set; }
		public int Innovation { get; set; }
		public bool Enabled { get; set; }
		public bool IsRecursive => From?.LayerNumber > To?.LayerNumber;
	}
}
