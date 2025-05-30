using Newtonsoft.Json.Linq;
using Ohiopyle.Geodesy.GeographicLib;
using Pennsylvania.Propagation;
using System;
using System.Collections.Generic;
using System.IO;
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
            DumpClutterLossToFile();
            return;

            //K137.ComparisionCurveSetForFrequency(1, 9, 9, 5);
            //K137.ImpactOfVaryingHb(3.5, 60, 5, 50);
            //K137.ImpactOfVaryingHm(3.5, 60, 6, 25);
            //K137.ImpactOfVaryingHg(3.5, 60, 70, 30);

            //K137.ComparisonWithBristolMeasurements(10, 20, 5);

            //ClutterAnalysis.ClutterStatistics();

            var ps = new[] { 0.1, 0.2, 0.5, 1, 2, 5, 10, 20, 50 };
            foreach (var p in ps)
            {
                P2108.AeronauticalStatisticalModel(7.601, 3, p, out double L_ces__db);
                //double L_ces__db = K137.Invoke(7.601, 4.5, p, 9, 26.8, 5);
                Console.WriteLine($"p = {p}%; L_c = {L_ces__db:0.00} dB");
            }

            MeasurementSummary.BoulderData();
        }

        static void DumpClutterLossToFile()
        {
            var geodesy = new GeographicLibGeodesy();

            string dir = @"C:\Users\wkozma\Downloads\R23-WP3K-C-0021!P1!ZIP-E";
            string file = "Boulder_Drexel_GreenMesa_3475_20221216";
            var filepath = Path.Combine(dir, $"{file}.json");

            var json = JObject.Parse(File.ReadAllText(filepath));

            using (var fs = new StreamWriter(Path.Combine(@"C:\outputs", $"{file}.csv")))
            {

                double txLat = Convert.ToDouble(json["metadata"]["TxLat"]);
                double txLon = Convert.ToDouble(json["metadata"]["TxLon"]);
                double f__mhz = Convert.ToDouble(json["metadata"]["f__mhz"]);

                foreach (var pt in json["measurements"])
                {
                    double rxLat = Convert.ToDouble(pt["RxLat"]);
                    double rxLon = Convert.ToDouble(pt["RxLon"]);

                    double L_btl__db = Convert.ToDouble(pt["L_btl__db"]);

                    double d__km = geodesy.Distance(txLat, txLon, rxLat, rxLon) / 1000;

                    double L_fs__db = Common.FreeSpaceLoss(f__mhz, d__km);

                    double L_c__db = L_btl__db - L_fs__db;

                    fs.WriteLine(L_c__db);
                }
            }
        }
    }
}
