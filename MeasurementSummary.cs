using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClutterAnalysis
{
    internal class MeasurementSummary
    {
        public static void Boulder_HistogramOfElevationAngle(string filepath)
        {
            var json = JObject.Parse(File.ReadAllText(filepath));


        }
    }
}
