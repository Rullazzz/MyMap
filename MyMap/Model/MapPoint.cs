using System;

namespace MyMap.Model
{
    public class MapPoint
    {
		public double X { get; set; }
        public double Y { get; set; }

		public MapPoint(double x, double y)
		{
			X = x;
			Y = y;
		}
	}
}
