using Newtonsoft.Json.Linq;
using Ohiopyle.Geodesy.GeographicLib;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using Pennsylvania;
using Pennsylvania.Propagation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ClutterAnalysis
{
    /// <summary>
    /// This is an implementation of Document 3K/137 by Australia. It is a proposed modification 
    /// of Recommendation ITU-R P.2108 Sec 3.3
    /// URL: https://www.itu.int/md/R23-WP3K-C-0137/en
    /// </summary>
    internal static class K137
    {
        static OxyColor[] _colors = new[]
        {
            OxyColors.Green,
            OxyColors.Red,
            OxyColors.Blue,
            OxyColors.Goldenrod,
            OxyColors.Purple,
            OxyColors.Orange,
            OxyColors.Orchid,
            OxyColors.Brown,
            OxyColors.Salmon,
            OxyColors.RoyalBlue,
            OxyColors.Pink
        };

        /// <summary>
        /// Proposed clutter model in 3K/137
        /// </summary>
        /// <param name="f__ghz">Frequency, in GHz</param>
        /// <param name="theta__deg">Elevation angle, in deg</param>
        /// <param name="p">Precentage of locations</param>
        /// <param name="h_b__meter">Average building height, in meters</param>
        /// <param name="h_m__meter">Maximum building height, in meters</param>
        /// <param name="h_g__meter">Terrestrial antenna height, in meters</param>
        /// <returns>Clutter loss, in dB</returns>
        public static double Invoke(double f__ghz, double theta__deg, double p,
            double h_b__meter, double h_m__meter, double h_g__meter)
        {
            // partial input validation
            if (h_b__meter < 9 || h_b__meter > 30)
                throw new ArgumentException("h_b is out of range");
            if (h_m__meter < 9 || h_m__meter > 200)
                throw new ArgumentException("h_m is out of range");
            if (h_g__meter < 5 || h_g__meter > h_m__meter)
                throw new ArgumentException("h_g is out of range");

            // supporting parameters
            double K_1 = 93.0 * Math.Pow(f__ghz, 0.175);
            double A_1 = 0.05;
            double B_1 = 0.00476 * h_b__meter + 0.4071;
            double C_1 = (h_g__meter - 5) / (h_m__meter - 5) * (90 - theta__deg) + theta__deg;

            double term1 = Math.Log(1 - p / 100);
            double term2 = A_1 * (1 - C_1 / 90) + Math.PI * C_1 / 180;
            double term3 = B_1 * (90 - C_1) / 90;

            double term4 = -K_1 * term1 * (1 / Math.Tan(term2));

            double Q = InverseComplementaryCumulativeDistribution.Invoke(p / 100);

            double L_ces__db = Math.Pow(term4, term3) - 1 - 0.6 * Q;

            return L_ces__db;
        }

        /// <summary>
        /// Generate a set of curves for varying elevation angles
        /// </summary>
        /// <param name="f__ghz">Frequency, in GHz</param>
        /// <param name="h_b__meter">Average building height, in meters</param>
        /// <param name="h_m__meter">Maximum building height, in meters</param>
        /// <param name="h_g__meter">Terrestrial station antenna height, in meters</param>
        public static void CurveSetForFrequency(double f__ghz, double h_b__meter,
            double h_m__meter, double h_g__meter)
        {
            var pm = new PlotModel()
            {
                Background = OxyColors.White,
                Title = $"Cumulative distribution of clutter loss not exceeded for {f__ghz} GHz",
                Subtitle = $"3K/137 (AUS) Model; h_b = {h_b__meter}; h_m = {h_m__meter}, h_g = {h_g__meter}"
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
                    double L_ces__db = K137.Invoke(f__ghz, theta__deg, p, h_b__meter, h_m__meter, h_g__meter);
                    losses.Add(L_ces__db);
                }

                Mathematics.BinData(losses.ToArray(), out double[] bins, out _, out double[] probs, 0.1);

                var cdfSeries = new LineSeries()
                {
                    StrokeThickness = 2,
                    Title = $"{theta__deg}°"
                };

                var sortedBins = bins.OrderBy(b => b).ToList();
                double total = 0;
                for (int i = 0; i< bins.Length; i++)
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
            OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, pm, Path.Combine(@"C:\outputs", "plot.png"));
        }

        /// <summary>
        /// Generate a set of curves for varying elevation angles
        /// </summary>
        /// <param name="f__ghz">Frequency, in GHz</param>
        /// <param name="h_b__meter">Average building height, in meters</param>
        /// <param name="h_m__meter">Maximum building height, in meters</param>
        /// <param name="h_g__meter">Terrestrial station antenna height, in meters</param>
        public static void ComparisionCurveSetForFrequency(double f__ghz, double h_b__meter,
            double h_m__meter, double h_g__meter)
        {
            var pm = new PlotModel()
            {
                Background = OxyColors.White,
                Title = $"Comparision of clutter loss for {f__ghz} GHz",
                Subtitle = $"P.2018 Sec 3.3 and 3K/137 (AUS) Model; h_b = {h_b__meter}; h_m = {h_m__meter}, h_g = {h_g__meter}"
            };
            
            var xAxis = new LinearAxis();
            xAxis.Title = "Clutter Loss (dB)";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.MajorGridlineStyle = LineStyle.Solid;
            xAxis.MinorGridlineStyle = LineStyle.Solid;
            xAxis.Maximum = 50;
            pm.Axes.Add(xAxis);

            var yAxis = new LinearAxis();
            yAxis.Title = "Cummulative Probability";
            yAxis.Position = AxisPosition.Left;
            yAxis.MajorGridlineStyle = LineStyle.Solid;
            yAxis.MinorGridlineStyle = LineStyle.Solid;
            pm.Axes.Add(yAxis);

            // loop through each of the elevation angles
            for (int theta__deg = 0; theta__deg <= 90; theta__deg += 10)
            {
                var L_K137__db = new List<double>();
                var L_P2108__db = new List<Double>();

                // loop through each of the location p's
                for (double p = 0.01; p < 100; p += 0.01)
                {
                    double L_K137_ces__db = K137.Invoke(f__ghz, theta__deg, p, h_b__meter, h_m__meter, h_g__meter);
                    L_K137__db.Add(L_K137_ces__db);

                    P2108.AeronauticalStatisticalModel(f__ghz, theta__deg, p, out double L_P2108_ces__db);
                    L_P2108__db.Add(L_P2108_ces__db);
                }

                Mathematics.BinData(L_K137__db.ToArray(), out double[] bins_K137, out _, out double[] probs_K137, 0.1);
                Mathematics.BinData(L_P2108__db.ToArray(), out double[] bins_P2108, out _, out double[] probs_P2108, 0.1);

                var cdfSeriesK137 = new LineSeries()
                {
                    StrokeThickness = 3,
                    LineStyle = LineStyle.Solid,
                    Title = $"{theta__deg}°",
                    Color = _colors[theta__deg / 10]
                };

                var sortedBins_K137 = bins_K137.OrderBy(b => b).ToList();
                double total = 0;
                for (int i = 0; i < bins_K137.Length; i++)
                {
                    // get index in bins of next sorted bin
                    int j = Array.IndexOf(bins_K137, sortedBins_K137[i]);

                    total += probs_K137[j];
                    cdfSeriesK137.Points.Add(new DataPoint(bins_K137[j], total));
                }
                pm.Series.Add(cdfSeriesK137);

                var cdfSeriesP2108 = new LineSeries()
                {
                    StrokeThickness = 2,
                    LineStyle = LineStyle.Dot,
                    Color = OxyColors.Black,// _colors[theta__deg / 10]
                    //Title = $"P2108: {theta__deg}°"
                };

                var sortedBins_P2108 = bins_P2108.OrderBy(b => b).ToList();
                total = 0;
                for (int i = 0; i < bins_P2108.Length; i++)
                {
                    // get index in bins of next sorted bin
                    int j = Array.IndexOf(bins_P2108, sortedBins_P2108[i]);

                    total += probs_P2108[j];
                    cdfSeriesP2108.Points.Add(new DataPoint(bins_P2108[j], total));
                }
                pm.Series.Add(cdfSeriesP2108);
            }

            pm.Legends.Add(new Legend()
            {
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Vertical,
                LegendBorder = OxyColors.Black
            });

            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 800, Height = 600 };
            OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, pm, Path.Combine(@"C:\outputs", "plot.png"));
        }

        public static void CompareWithBoulderMeasurements(double h_b__meter,
            double h_m__meter, double h_g__meter)
        {
            var geodesy = new GeographicLibGeodesy();

            var filepath = @"C:\Users\wkozma\Desktop\JWG-Clutter\input-documents\R23-WP3K-C-0148!P1!ZIP-E\Boulder_Downtown_GreenMesa_7601_20241114.json";

            var json = JObject.Parse(File.ReadAllText(filepath));

            var losses = new List<double>();
            foreach (var pt in json["datapoints"])
            {
                double txLat = Convert.ToDouble(pt["tx_lat__deg"]);
                double txLon = Convert.ToDouble(pt["tx_lon__deg"]);
                double rxLat = Convert.ToDouble(pt["rx_lat__deg"]);
                double rxLon = Convert.ToDouble(pt["rx_lon__deg"]);

                double L_btl__db = Convert.ToDouble(pt["L_btl__dB"]);
                double f__mhz = Convert.ToDouble(pt["f__MHz"]);

                double d__km = geodesy.Distance(txLat, txLon, rxLat, rxLon) / 1000;

                double L_fs__db = FreeSpaceLoss(f__mhz, d__km);

                double L_c__db = L_btl__db - L_fs__db;
                losses.Add(L_c__db);
            }

            var pm = new PlotModel()
            {
                Background = OxyColors.White,
                Title = $"Downtown Boulder 7601 MHz Comparison",
                Subtitle = $"3K/137 (AUS) Model; h_b = {h_b__meter}; h_m = {h_m__meter}, h_g = {h_g__meter}"
            };

            var xAxis = new LinearAxis();
            xAxis.Title = "Clutter Loss (dB)";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.MajorGridlineStyle = LineStyle.Solid;
            xAxis.Maximum = 45;
            pm.Axes.Add(xAxis);

            var yAxis = new LinearAxis();
            yAxis.Title = "Cummulative Probability";
            yAxis.Position = AxisPosition.Left;
            yAxis.MajorGridlineStyle = LineStyle.Solid;
            yAxis.MinorGridlineStyle = LineStyle.Solid;
            pm.Axes.Add(yAxis);

            Mathematics.BinData(losses.ToArray(), out double[] bins, out _, out double[] probs, 0.1);

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

            var losses_3deg = new List<double>();
            var losses_6deg = new List<double>();

            // loop through each of the location p's
            for (double p = 0.01; p < 100; p += 0.01)
            {
                double L_ces__db = K137.Invoke(7601, 2, p, h_b__meter, h_m__meter, h_g__meter);
                losses_3deg.Add(L_ces__db);

                L_ces__db = K137.Invoke(7601, 4, p, h_b__meter, h_m__meter, h_g__meter);
                losses_6deg.Add(L_ces__db);
            }

            Mathematics.BinData(losses_3deg.ToArray(), out double[] bins_3deg, out _, out double[] probs_3deg, 0.1);
            Mathematics.BinData(losses_6deg.ToArray(), out double[] bins_6deg, out _, out double[] probs_6deg, 0.1);

            var cdfSeries_3deg = new LineSeries()
            {
                StrokeThickness = 2,
                Title = $"2°",
                Color = OxyColors.Purple,
                LineStyle = LineStyle.Dash
            };
            var cdfSeries_6deg = new LineSeries()
            {
                StrokeThickness = 2,
                Title = $"4°",
                Color = OxyColors.Green,
                LineStyle = LineStyle.Dash
            };

            var sortedBins_3deg = bins_3deg.OrderBy(b => b).ToList();
            var sortedBins_6deg = bins_6deg.OrderBy(b => b).ToList();

            total = 0;
            for (int i = 0; i < bins_3deg.Length; i++)
            {
                // get index in bins of next sorted bin
                int j = Array.IndexOf(bins_3deg, sortedBins_3deg[i]);

                total += probs_3deg[j];
                cdfSeries_3deg.Points.Add(new DataPoint(bins_3deg[j], total));
            }

            total = 0;
            for (int i = 0; i < bins_6deg.Length; i++)
            {
                // get index in bins of next sorted bin
                int j = Array.IndexOf(bins_6deg, sortedBins_6deg[i]);

                total += probs_6deg[j];
                cdfSeries_6deg.Points.Add(new DataPoint(bins_6deg[j], total));
            }

            pm.Series.Add(cdfSeries_3deg);
            pm.Series.Add(cdfSeries_6deg);

            pm.Legends.Add(new Legend()
            {
                LegendPosition = LegendPosition.BottomCenter,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendBorder = OxyColors.Black
            });

            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 800, Height = 600 };
            OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, pm, Path.Combine(@"C:\outputs", "plot.png"));
        }

        public static void ComparisonWithBristolMeasurements(double h_b__meter,
            double h_m__meter, double h_g__meter)
        {
            double total = 0;

            var lines = File.ReadAllLines(@"C:\Users\wkozma\Desktop\JWG-Clutter\UK-Bristol.csv").Skip(1);

            var pm = new PlotModel()
            {
                Background = OxyColors.White,
                Title = $"Bristol 5760 MHz Comparison",
                Subtitle = $"3K/137 (AUS) Model; h_b = {h_b__meter}; h_m = {h_m__meter}, h_g = {h_g__meter}"
            };

            var xAxis = new LinearAxis();
            xAxis.Title = "Clutter Loss (dB)";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.MajorGridlineStyle = LineStyle.Solid;
            xAxis.Maximum = 45;
            pm.Axes.Add(xAxis);

            var yAxis = new LinearAxis();
            yAxis.Title = "Cummulative Probability";
            yAxis.Position = AxisPosition.Left;
            yAxis.MajorGridlineStyle = LineStyle.Solid;
            yAxis.MinorGridlineStyle = LineStyle.Solid;
            pm.Axes.Add(yAxis);

            // loop through each of the elevation angles
            double f__ghz = 5.760;
            for (int theta__deg = 2; theta__deg <= 8; theta__deg += 2)
            {
                var L_K137__db = new List<double>();
                var L_P2108__db = new List<Double>();

                // loop through each of the location p's
                for (double p = 0.01; p < 100; p += 0.01)
                {
                    double L_K137_ces__db = K137.Invoke(f__ghz, theta__deg, p, h_b__meter, h_m__meter, h_g__meter);
                    L_K137__db.Add(L_K137_ces__db);

                    P2108.AeronauticalStatisticalModel(f__ghz, theta__deg, p, out double L_P2108_ces__db);
                    L_P2108__db.Add(L_P2108_ces__db);
                }

                Mathematics.BinData(L_K137__db.ToArray(), out double[] bins_K137, out _, out double[] probs_K137, 0.1);
                Mathematics.BinData(L_P2108__db.ToArray(), out double[] bins_P2108, out _, out double[] probs_P2108, 0.1);

                var cdfSeriesK137 = new LineSeries()
                {
                    StrokeThickness = 3,
                    LineStyle = LineStyle.Solid,
                    Title = $"K137, {theta__deg}°",
                    Color = _colors[theta__deg / 2]
                };

                var sortedBins_K137 = bins_K137.OrderBy(b => b).ToList();
                total = 0;
                for (int i = 0; i < bins_K137.Length; i++)
                {
                    // get index in bins of next sorted bin
                    int j = Array.IndexOf(bins_K137, sortedBins_K137[i]);

                    total += probs_K137[j];
                    cdfSeriesK137.Points.Add(new DataPoint(bins_K137[j], total));
                }
                //pm.Series.Add(cdfSeriesK137);

                var cdfSeriesP2108 = new LineSeries()
                {
                    StrokeThickness = 2,
                    Color = _colors[theta__deg / 2],// _colors[theta__deg / 10]
                    Title = $"P2108: {theta__deg}°"
                };

                var sortedBins_P2108 = bins_P2108.OrderBy(b => b).ToList();
                total = 0;
                for (int i = 0; i < bins_P2108.Length; i++)
                {
                    // get index in bins of next sorted bin
                    int j = Array.IndexOf(bins_P2108, sortedBins_P2108[i]);

                    total += probs_P2108[j];
                    cdfSeriesP2108.Points.Add(new DataPoint(bins_P2108[j], total));
                }
                pm.Series.Add(cdfSeriesP2108);
            }

            // bin Bristol measurements
            var deg1_3 = new List<Double>();
            var deg3_5 = new List<Double>();
            var deg5_7 = new List<Double>();
            var deg7_9 = new List<Double>();
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                var L_c = Convert.ToDouble(parts[0]);
                var theta = Convert.ToDouble(parts[1]);

                if (theta >= 1 && theta < 3)
                    deg1_3.Add(L_c);
                if (theta >= 3 && theta < 5)
                    deg3_5.Add(L_c);
                if (theta >= 5 && theta < 7)
                    deg5_7.Add(L_c);
                if (theta >= 7 && theta < 9)
                    deg7_9.Add(L_c);
            }

            Mathematics.BinData(deg1_3.ToArray(), out double[] bins_13, out _, out double[] probs_13, 0.1);
            var cdfSeries13 = new LineSeries()
            {
                StrokeThickness = 3,
                LineStyle = LineStyle.Dot,
                Title = $"Meas, 1-3°",
                Color = _colors[1]
            };

            var sortedBins_13 = bins_13.OrderBy(b => b).ToList();
            total = 0;
            for (int i = 0; i < bins_13.Length; i++)
            {
                // get index in bins of next sorted bin
                int j = Array.IndexOf(bins_13, sortedBins_13[i]);

                total += probs_13[j];
                cdfSeries13.Points.Add(new DataPoint(bins_13[j], total));
            }
            pm.Series.Add(cdfSeries13);

            Mathematics.BinData(deg3_5.ToArray(), out double[] bins_35, out _, out double[] probs_35, 0.1);
            var cdfSeries35 = new LineSeries()
            {
                StrokeThickness = 3,
                LineStyle = LineStyle.Dot,
                Title = $"Meas, 3-5°",
                Color = _colors[2]
            };

            var sortedBins_35 = bins_35.OrderBy(b => b).ToList();
            total = 0;
            for (int i = 0; i < bins_35.Length; i++)
            {
                // get index in bins of next sorted bin
                int j = Array.IndexOf(bins_35, sortedBins_35[i]);

                total += probs_35[j];
                cdfSeries35.Points.Add(new DataPoint(bins_35[j], total));
            }
            pm.Series.Add(cdfSeries35);

            Mathematics.BinData(deg5_7.ToArray(), out double[] bins_57, out _, out double[] probs_57, 0.1);
            var cdfSeries57 = new LineSeries()
            {
                StrokeThickness = 3,
                LineStyle = LineStyle.Dot,
                Title = $"Meas, 5-7°",
                Color = _colors[3]
            };

            var sortedBins_57 = bins_57.OrderBy(b => b).ToList();
            total = 0;
            for (int i = 0; i < bins_57.Length; i++)
            {
                // get index in bins of next sorted bin
                int j = Array.IndexOf(bins_57, sortedBins_57[i]);

                total += probs_57[j];
                cdfSeries57.Points.Add(new DataPoint(bins_57[j], total));
            }
            pm.Series.Add(cdfSeries57);

            Mathematics.BinData(deg7_9.ToArray(), out double[] bins_79, out _, out double[] probs_79, 0.1);
            var cdfSeries79 = new LineSeries()
            {
                StrokeThickness = 3,
                LineStyle = LineStyle.Dot,
                Title = $"Meas, 1-3°",
                Color = _colors[4]
            };

            var sortedBins_79 = bins_79.OrderBy(b => b).ToList();
            total = 0;
            for (int i = 0; i < bins_79.Length; i++)
            {
                // get index in bins of next sorted bin
                int j = Array.IndexOf(bins_79, sortedBins_79[i]);

                total += probs_79[j];
                cdfSeries79.Points.Add(new DataPoint(bins_79[j], total));
            }
            pm.Series.Add(cdfSeries79);

            pm.Legends.Add(new Legend()
            {
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Vertical,
                LegendBorder = OxyColors.Black
            });

            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 800, Height = 600 };
            OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, pm, Path.Combine(@"C:\outputs", "plot.png"));
        }

        public static void ImpactOfVaryingHb(double f__ghz, double theta__deg,
            double h_g__meter, double h_m__meter)
        {
            var pm = new PlotModel()
            {
                Background = OxyColors.White,
                Title = $"Impact of h_b on clutter loss for {f__ghz} GHz",
                Subtitle = $"3K/137 (AUS) Model; h_m = {h_m__meter}, h_g = {h_g__meter}, theta = {theta__deg}"
            };

            var xAxis = new LinearAxis();
            xAxis.Title = "Clutter Loss (dB)";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.MajorGridlineStyle = LineStyle.Solid;
            xAxis.MinorGridlineStyle = LineStyle.Solid;
            xAxis.Maximum = 50;
            pm.Axes.Add(xAxis);

            var yAxis = new LinearAxis();
            yAxis.Title = "Cummulative Probability";
            yAxis.Position = AxisPosition.Left;
            yAxis.MajorGridlineStyle = LineStyle.Solid;
            yAxis.MinorGridlineStyle = LineStyle.Solid;
            pm.Axes.Add(yAxis);

            // loop values of h_b__meter
            for (double h_b__meter = 9; h_b__meter <= 30; h_b__meter += 3)
            {
                var L_K137__db = new List<double>();

                // loop through each of the location p's
                for (double p = 0.01; p < 100; p += 0.01)
                {
                    double L_K137_ces__db = K137.Invoke(f__ghz, theta__deg, p, h_b__meter, h_m__meter, h_g__meter);
                    L_K137__db.Add(L_K137_ces__db);
                }

                Mathematics.BinData(L_K137__db.ToArray(), out double[] bins_K137, out _, out double[] probs_K137, 0.1);

                var cdfSeriesK137 = new LineSeries()
                {
                    StrokeThickness = 2,
                    LineStyle = LineStyle.Solid,
                    Title = $"{h_b__meter} m",
                };

                var sortedBins_K137 = bins_K137.OrderBy(b => b).ToList();
                double total = 0;
                for (int i = 0; i < bins_K137.Length; i++)
                {
                    // get index in bins of next sorted bin
                    int j = Array.IndexOf(bins_K137, sortedBins_K137[i]);

                    total += probs_K137[j];
                    cdfSeriesK137.Points.Add(new DataPoint(bins_K137[j], total));
                }
                pm.Series.Add(cdfSeriesK137);
            }

            pm.Legends.Add(new Legend()
            {
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Vertical,
                LegendBorder = OxyColors.Black
            });

            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 800, Height = 600 };
            OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, pm, Path.Combine(@"C:\outputs", "plot.png"));
        }

        public static void ImpactOfVaryingHm(double f__ghz, double theta__deg,
            double h_g__meter, double h_b__meter)
        {
            var pm = new PlotModel()
            {
                Background = OxyColors.White,
                Title = $"Impact of h_b on clutter loss for {f__ghz} GHz",
                Subtitle = $"3K/137 (AUS) Model; h_b = {h_b__meter}, h_g = {h_g__meter}, theta = {theta__deg}"
            };

            var xAxis = new LinearAxis();
            xAxis.Title = "Clutter Loss (dB)";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.MajorGridlineStyle = LineStyle.Solid;
            xAxis.MinorGridlineStyle = LineStyle.Solid;
            xAxis.Maximum = 50;
            pm.Axes.Add(xAxis);

            var yAxis = new LinearAxis();
            yAxis.Title = "Cummulative Probability";
            yAxis.Position = AxisPosition.Left;
            yAxis.MajorGridlineStyle = LineStyle.Solid;
            yAxis.MinorGridlineStyle = LineStyle.Solid;
            pm.Axes.Add(yAxis);

            // loop values of h_b__meter
            for (double h_m__meter = 25; h_m__meter <= 200; h_m__meter += 25)
            {
                var L_K137__db = new List<double>();

                // loop through each of the location p's
                for (double p = 0.01; p < 100; p += 0.01)
                {
                    double L_K137_ces__db = K137.Invoke(f__ghz, theta__deg, p, h_b__meter, h_m__meter, h_g__meter);
                    L_K137__db.Add(L_K137_ces__db);
                }

                Mathematics.BinData(L_K137__db.ToArray(), out double[] bins_K137, out _, out double[] probs_K137, 0.1);

                var cdfSeriesK137 = new LineSeries()
                {
                    StrokeThickness = 2,
                    LineStyle = LineStyle.Solid,
                    Title = $"{h_m__meter} m",
                };

                var sortedBins_K137 = bins_K137.OrderBy(b => b).ToList();
                double total = 0;
                for (int i = 0; i < bins_K137.Length; i++)
                {
                    // get index in bins of next sorted bin
                    int j = Array.IndexOf(bins_K137, sortedBins_K137[i]);

                    total += probs_K137[j];
                    cdfSeriesK137.Points.Add(new DataPoint(bins_K137[j], total));
                }
                pm.Series.Add(cdfSeriesK137);
            }

            pm.Legends.Add(new Legend()
            {
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Vertical,
                LegendBorder = OxyColors.Black
            });

            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 800, Height = 600 };
            OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, pm, Path.Combine(@"C:\outputs", "plot.png"));
        }

        public static void ImpactOfVaryingHg(double f__ghz, double theta__deg,
            double h_m__meter, double h_b__meter)
        {
            var pm = new PlotModel()
            {
                Background = OxyColors.White,
                Title = $"Impact of h_g on clutter loss for {f__ghz} GHz",
                Subtitle = $"3K/137 (AUS) Model; h_b = {h_b__meter}, h_m = {h_m__meter}, theta = {theta__deg}"
            };

            var xAxis = new LinearAxis();
            xAxis.Title = "Clutter Loss (dB)";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.MajorGridlineStyle = LineStyle.Solid;
            xAxis.MinorGridlineStyle = LineStyle.Solid;
            xAxis.Maximum = 50;
            pm.Axes.Add(xAxis);

            var yAxis = new LinearAxis();
            yAxis.Title = "Cummulative Probability";
            yAxis.Position = AxisPosition.Left;
            yAxis.MajorGridlineStyle = LineStyle.Solid;
            yAxis.MinorGridlineStyle = LineStyle.Solid;
            pm.Axes.Add(yAxis);

            for (double h_g__meter = 5; h_g__meter <= 30; h_g__meter += 5)
            {
                var L_K137__db = new List<double>();

                // loop through each of the location p's
                for (double p = 0.01; p < 100; p += 0.01)
                {
                    double L_K137_ces__db = K137.Invoke(f__ghz, theta__deg, p, h_b__meter, h_m__meter, h_g__meter);
                    L_K137__db.Add(L_K137_ces__db);
                }

                Mathematics.BinData(L_K137__db.ToArray(), out double[] bins_K137, out _, out double[] probs_K137, 0.1);

                var cdfSeriesK137 = new LineSeries()
                {
                    StrokeThickness = 2,
                    LineStyle = LineStyle.Solid,
                    Title = $"{h_g__meter} m",
                };

                var sortedBins_K137 = bins_K137.OrderBy(b => b).ToList();
                double total = 0;
                for (int i = 0; i < bins_K137.Length; i++)
                {
                    // get index in bins of next sorted bin
                    int j = Array.IndexOf(bins_K137, sortedBins_K137[i]);

                    total += probs_K137[j];
                    cdfSeriesK137.Points.Add(new DataPoint(bins_K137[j], total));
                }
                pm.Series.Add(cdfSeriesK137);
            }

            pm.Legends.Add(new Legend()
            {
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Vertical,
                LegendBorder = OxyColors.Black
            });

            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 800, Height = 600 };
            OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, pm, Path.Combine(@"C:\outputs", "plot.png"));
        }

        /// <summary>
        /// Compute free space basic transmission loss
        /// </summary>
        /// <param name="f__mhz">Frequency, in MHz</param>
        /// <param name="d__km">Distance, in km</param>
        /// <returns>Loss, in dB</returns>
        private static double FreeSpaceLoss(double f__mhz, double d__km)
        {
            return 20 * Math.Log10(d__km) + 20 * Math.Log10(f__mhz) + 32.45;
        }
    }
}
