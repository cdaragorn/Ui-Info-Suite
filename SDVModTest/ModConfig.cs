using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIInfoSuite {
	public class ModConfig {
		public string[] KeysForBarrelAndCropTimes { get; set; } = new string[]
		{
			Keys.LeftShift.ToString()
		};

		public bool CanRightClickForBarrelAndCropTimes { get; set; } = true;
		public Dictionary<string, string> Townspeople = new Dictionary<string, string>();

		public string[] Sprinkler { get; set; } = new string[]
		{
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,1,0,0,0,0,0",
			"0,0,0,0,1,0,1,0,0,0,0",
			"0,0,0,0,0,1,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
		};

		public string[] QualitySprinkler { get; set; } = new string[]
		{
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,1,1,1,0,0,0,0",
			"0,0,0,0,1,0,1,0,0,0,0",
			"0,0,0,0,1,1,1,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
		};

		public string[] IridiumSprinkler { get; set; } = new string[]
		{
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,1,1,1,1,1,0,0,0",
			"0,0,0,1,1,1,1,1,0,0,0",
			"0,0,0,1,1,0,1,1,0,0,0",
			"0,0,0,1,1,1,1,1,0,0,0",
			"0,0,0,1,1,1,1,1,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
			"0,0,0,0,0,0,0,0,0,0,0",
		};

		public string[] Beehouse { get; set; } = new string[]
		{
			"0,0,0,0,0,1,0,0,0,0,0",
			"0,0,0,0,1,1,1,0,0,0,0",
			"0,0,0,1,1,1,1,1,0,0,0",
			"0,0,1,1,1,1,1,1,1,0,0",
			"0,1,1,1,1,1,1,1,1,1,0",
			"0,1,1,1,1,0,1,1,1,1,0",
			"0,1,1,1,1,1,1,1,1,1,0",
			"0,0,1,1,1,1,1,1,1,0,0",
			"0,0,0,1,1,1,1,1,0,0,0",
			"0,0,0,0,1,1,1,0,0,0,0",
			"0,0,0,0,0,1,0,0,0,0,0"
		};

		public int[][] getIntArray(string[] input) {
			int[][] output = new int[][] {
				new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				new int[] { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
				new int[] { 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0 },
				new int[] { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
				new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
			};

			for (int i = 0; i < input.Count(); i++) {
				var columns = input[i].Split(',');

				for (int j = 0; j < columns.Count(); j++) {
					output[i][j] = (columns[j] == "1") ? 1 : 0;
				}
			}

			return output;
		}
	}
}
