using Newtonsoft.Json.Linq;
using Ohiopyle.Geodesy.GeographicLib;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Legends;
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
            RC2.CompareWithUSAMeasurements(RC2.Environment.MidRise);
            //RC2.CompareWithBristolMeasurements(RC2.Environment.MidRise);

            //TerminalHeightDependenceInBoulder();
            //ClutterAnalysis.ClutterStatistics();
            //ClutterAnalysis.ClutterHeightComparisionCdf();

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

        static void FrequencyCurveSet(double h__meter, double theta__deg, RC2.Environment env)
        {
            var pm = new PlotModel()
            {
                Background = OxyColors.White,
                Title = $"Cumulative distribution of clutter loss for {theta__deg} degrees",
                Subtitle = $"RC2 Model; h = {h__meter}; env = {env.ToString()}"
            };

            var xAxis = new LinearAxis();
            xAxis.Title = "Clutter Loss (dB)";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.MajorGridlineStyle = LineStyle.Solid;
            xAxis.Maximum = 50;
            pm.Axes.Add(xAxis);

            var yAxis = new LinearAxis();
            yAxis.Title = "Cummulative Probability";
            yAxis.Position = AxisPosition.Left;
            yAxis.MajorGridlineStyle = LineStyle.Solid;
            pm.Axes.Add(yAxis);

            // loop through each of the elevation angles
            for (int f__ghz = 1; f__ghz <= 30; f__ghz += 4)
            {
                var losses = new List<double>();

                // loop through each of the location p's
                for (double p = 0.01; p < 100; p += 0.01)
                {
                    double L_ces__db = RC2.Invoke(f__ghz, theta__deg, p, h__meter, env);
                    losses.Add(L_ces__db);
                }

                Mathematics.BinData(losses.ToArray(), out double[] bins, out _, out double[] probs, 0.1);

                var cdfSeries = new LineSeries()
                {
                    StrokeThickness = 2,
                    Title = $"{f__ghz} GHz"
                };

                var sortedBins = bins.OrderBy(b => b).ToList();
                double total = 0;
                for (int i = 0; i < bins.Length; i++)
                {
                    // get index in bins of next sorted bin
                    int j = Array.IndexOf(bins, sortedBins[i]);

                    total += probs[j];
                    cdfSeries.Points.Add(new DataPoint(bins[j], total));
                }
                pm.Series.Add(cdfSeries);
            }

            pm.Legends.Add(new Legend()
            {
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Vertical,
                LegendBorder = OxyColors.Black
            });

            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 800, Height = 600 };
            OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, pm, Path.Combine(@"C:\outputs", "RC2-FreqeuncyCurves.png"));
        }

        static void TerminalHeightDependence(double f__ghz, double theta__deg, RC2.Environment env)
        {
            var pm = new PlotModel()
            {
                Background = OxyColors.White,
                Title = $"Terminal Height Dependence",
                Subtitle = $"RC2 Model; f = {f__ghz} GHz; theta = {theta__deg}; env = {env.ToString()}"
            };

            var xAxis = new LinearAxis();
            xAxis.Title = "Clutter Loss (dB)";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.MajorGridlineStyle = LineStyle.Solid;
            xAxis.Maximum = 50;
            pm.Axes.Add(xAxis);

            var yAxis = new LinearAxis();
            yAxis.Title = "Cummulative Probability";
            yAxis.Position = AxisPosition.Left;
            yAxis.MajorGridlineStyle = LineStyle.Solid;
            pm.Axes.Add(yAxis);

            // loop through each of the elevation angles
            for (int h__meter = 1; h__meter <= 30; h__meter += 3)
            {
                var losses = new List<double>();

                // loop through each of the location p's
                for (double p = 0.01; p < 100; p += 0.01)
                {
                    double L_ces__db = RC2.Invoke(f__ghz, theta__deg, p, h__meter, env);
                    losses.Add(L_ces__db);
                }

                Mathematics.BinData(losses.ToArray(), out double[] bins, out _, out double[] probs, 0.1);

                var cdfSeries = new LineSeries()
                {
                    StrokeThickness = 2,
                    Title = $"{h__meter} m"
                };

                var sortedBins = bins.OrderBy(b => b).ToList();
                double total = 0;
                for (int i = 0; i < bins.Length; i++)
                {
                    // get index in bins of next sorted bin
                    int j = Array.IndexOf(bins, sortedBins[i]);

                    total += probs[j];
                    cdfSeries.Points.Add(new DataPoint(bins[j], total));
                }
                pm.Series.Add(cdfSeries);
            }

            pm.Legends.Add(new Legend()
            {
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Vertical,
                LegendBorder = OxyColors.Black
            });

            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 800, Height = 600 };
            OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, pm, Path.Combine(@"C:\outputs", "RC2-TerminalHeightDependence.png"));
        }

        static void TerminalHeightDependenceInBoulder()
        {
            double f__ghz = 3.5;
            double h__meter = 20;
            var env = RC2.Environment.HighRise;

            var pm = new PlotModel()
            {
                Background = OxyColors.White,
                Title = $"Terminal Height Dependence in Denver",
                Subtitle = $"RC2 Model; f = {f__ghz} GHz; h = {h__meter} m; env = {env.ToString()}"
            };

            var xAxis = new LinearAxis();
            xAxis.Title = "Clutter Loss (dB)";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.MajorGridlineStyle = LineStyle.Solid;
            xAxis.Maximum = 50;
            pm.Axes.Add(xAxis);

            var yAxis = new LinearAxis();
            yAxis.Title = "Cummulative Probability";
            yAxis.Position = AxisPosition.Left;
            yAxis.MajorGridlineStyle = LineStyle.Solid;
            pm.Axes.Add(yAxis);

            // loop through each of the elevation angles
            for (int theta__deg = 0; theta__deg <= 90; theta__deg += 10)
            {
                var losses = new List<double>();

                // loop through each of the location p's
                for (double p = 0.01; p < 100; p += 0.01)
                {
                    double L_ces__db = RC2.Invoke(f__ghz, theta__deg, p, h__meter, env);
                    losses.Add(L_ces__db);
                }

                Mathematics.BinData(losses.ToArray(), out double[] bins, out _, out double[] probs, 0.1);

                var cdfSeries = new LineSeries()
                {
                    StrokeThickness = 2,
                    Title = $"{theta__deg} deg"
                };

                var sortedBins = bins.OrderBy(b => b).ToList();
                double total = 0;
                for (int i = 0; i < bins.Length; i++)
                {
                    // get index in bins of next sorted bin
                    int j = Array.IndexOf(bins, sortedBins[i]);

                    total += probs[j];
                    cdfSeries.Points.Add(new DataPoint(bins[j], total));
                }
                pm.Series.Add(cdfSeries);
            }

            pm.Legends.Add(new Legend()
            {
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Vertical,
                LegendBorder = OxyColors.Black
            });

            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 800, Height = 600 };
            OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, pm, Path.Combine(@"C:\outputs", "RC2-TerminalHeightDependenceInDenver.png"));
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
