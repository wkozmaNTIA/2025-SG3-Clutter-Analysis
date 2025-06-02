using Newtonsoft.Json.Linq;
using Ohiopyle.Geodesy.GeographicLib;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using Pennsylvania;
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
            //RC1.CurveSetForFrequency(3.5, 3, RC1.Environment.LowRise);
            RC2.CompareWithBoulderMeasurements(RC2.Environment.LowRise);
            return;

            GenerateHistogramOfBTL();
            return;

            DumpClutterLossToFile();
            return;

            K182.CompareWithBoulderMeasurements(K182.Environment.MidRise);
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

        static void GenerateHistogramOfBTL()
        {
            var json = JObject.Parse(File.ReadAllText(@"C:\Users\wkozma\Downloads\R23-WP3K-C-0021!P1!ZIP-E\Boulder_MartinAcres_GreenMesa_3475_20230621.json"));

            var btls = new List<double>();
            foreach (var pt in json["measurements"])
            {
                var btl = Convert.ToDouble(pt["L_btl__db"]);
                btls.Add(btl);
            }

            var pm = new PlotModel()
            {
                Title = "Distribution of Basic Transmission Loss",
                Subtitle = "Boulder_MartinAcres_GreenMesa_3475_20230621",
                Background = OxyColors.White
            };

            Mathematics.BinData(btls.ToArray(), out double[] bins, out _, out double[] probs, 0.1);

            var xAxis = new LinearAxis();
            xAxis.Title = "Basic Transmission Loss (dB)";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.MajorGridlineStyle = LineStyle.Solid;
            xAxis.Maximum = 190;
            pm.Axes.Add(xAxis);

            var yAxis = new LinearAxis();
            yAxis.Title = "Cummulative Probability";
            yAxis.Position = AxisPosition.Left;
            yAxis.MajorGridlineStyle = LineStyle.Solid;
            yAxis.MinorGridlineStyle = LineStyle.Solid;
            pm.Axes.Add(yAxis);

            var cdfMeasurements = new LineSeries()
            {
                StrokeThickness = 2,
                Title = $"Measurements",
                Color = OxyColors.Blue
            };

            var sortedBins = bins.OrderBy(b => b).ToList();
            double total = 0;
            for (int i = 0; i < bins.Length; i++)
            {
                // get index in bins of next sorted bin
                int j = Array.IndexOf(bins, sortedBins[i]);

                total += probs[j];
                cdfMeasurements.Points.Add(new DataPoint(bins[j], total));
            }
            pm.Series.Add(cdfMeasurements);

            var noise__db = Convert.ToDouble(json["metadata"]["RelativeNoiseFloorDb"]);

            var noiseFloor = new LineSeries
            {
                StrokeThickness = 3,
                Color = OxyColors.Black
            };
            noiseFloor.Points.Add(new DataPoint(noise__db, 0));
            noiseFloor.Points.Add(new DataPoint(noise__db, 1));
            pm.Series.Add(noiseFloor);


            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 800, Height = 600 };
            OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, pm, Path.Combine(@"C:\outputs", "plot.png"));
        }

        static void DumpClutterLossToFile()
        {
            var geodesy = new GeographicLibGeodesy();

            string dir = @"C:\Users\wkozma\Downloads\R23-WP3K-C-0023!P1!ZIP-E";
            string file = "SaltLakeCity_Urban_CityCreek_3475_20230710";
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
