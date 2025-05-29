using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClutterAnalysis
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //K137.ComparisionCurveSetForFrequency(1, 9, 9, 5);
            //K137.ImpactOfVaryingHb(3.5, 60, 5, 50);
            //K137.ImpactOfVaryingHm(3.5, 60, 6, 25);
            //K137.ImpactOfVaryingHg(3.5, 60, 70, 30);

            //K137.CompareWithBoulderMeasurements(10, 25, 5);

            ClutterAnalysis.ClutterStatistics();
        }
    }
}
