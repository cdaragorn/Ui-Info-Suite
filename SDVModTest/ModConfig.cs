using StardewModdingAPI;
using System;

namespace UIInfoSuite
{
    class ModConfig
    {
        public string[] KeysForBarrelAndCropTimes { get; set; } = new string[]
        {
            SButton.LeftShift.ToString()
        };

        public bool CanRightClickForBarrelAndCropTimes { get; set; } = true;

        public int[][] Sprinkler { get; set; } = new int[][]
        {
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

        public int[][] QualitySprinkler { get; set; } = new int[][]
        {
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
        };

        public int[][] IridiumSprinkler { get; set; } = new int[][]
        {
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
            new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
            new int[] { 0, 0, 0, 1, 1, 0, 1, 1, 0, 0, 0 },
            new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
            new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
        };
		
		public int[][] PrismaticSprinkler { get; set; } = new int[][]
        {
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 0, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
        };

        public int[][] Beehouse { get; set; } = new int[][]
        {
            new int[] { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
            new int[] { 0, 1, 1, 1, 1, 0, 1, 1, 1, 1, 0 },
            new int[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 }
        };

        public int[][] Scarecrow { get; set; } = CalculateScarecrow(17);

        public int[][] DeluxeScarecrow { get; set; } = CalculateScarecrow(33);

        private static int[][] CalculateScarecrow(int size)
        {
            var oneSideLength = (size - 1) / 2;
            var oneSideLengthPlusHalf = (oneSideLength / 2) + oneSideLength;
            var arrayToUse = new int[size][];
            for (int i = 0; i < size; ++i)
            {
                arrayToUse[i] = new int[size];
                for (int j = 0; j < size; ++j)
                {
                    arrayToUse[i][j] = (Math.Abs(i - oneSideLength) + Math.Abs(j - oneSideLength) <= oneSideLengthPlusHalf) ? 1 : 0;
                }
            }
            return arrayToUse;
        }
    }
}
