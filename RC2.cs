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

namespace ClutterAnalysis
{
    internal class RC2
    {
        public enum Environment : int
        {
            LowRise = 0,
            MidRise = 1,
            HighRise = 2
        };

        static readonly double[] a_k = new[] { 4.9, -2.6, 2.4 };
        static readonly double[] b_k = new[] { 6.7, 6.6, 7 };
        static readonly double[] c_k = new[] { 2.6, 2, 1 };

        static readonly double[] a_c = new[] { 0.19, 0.42, 0.19 };
        static readonly double[] b_c = new[] { 0, -6.7, -2.7 };

        static readonly double[] a_v = new[] { 1.4, 0.15, 0.15 };
        static readonly double[] b_v = new[] { 74.0, 97, 98 };

        static readonly double[] a_kprime = new[] { 6, 3.6, 5 };
        static readonly double[] b_kprime = new[] { 0.07, 0.05, 0.003 };

        static readonly double[] a_cprime = new[] { 0.15, 0.17, 0.17 };
        static readonly double[] b_c1prime = new[] { 5.4, 13, 32.6 };
        static readonly double[] b_c2prime = new[] { -0.3, -0.2, 0.012 };
        static readonly double[] b_c3prime = new[] { 3.2, 3.7, -23.9 };
        static readonly double[] b_c4prime = new[] { 0.07, 0.05, -0.07 };
        static readonly double[] c_cprime = new double[] { -27, -41, -41 };

        static readonly double[] a_vprime = new[] { 1.6, 1, 1 };
        static readonly double[] b_vprime = new double[] { -17, -21, -18 };

        static readonly double[] a_N = new[] { 8.54, 0.056 };
        static readonly double[] b_N = new[] { 17.57, 6.32 };
        static readonly double[] c_N = new[] { 0.63, 0.19 };

        static public double Invoke(double f__ghz, double theta__deg,
            double p, double h__meter, Environment env)
        {
            // compute probability of LOS
            //////////////////////////////////

            // equation 8(a-c)
            double V_max = Math.Min(a_v[(int)env] * h__meter + b_v[(int)env], 100);
            double C_e = a_c[(int)env] * h__meter + b_c[(int)env];
            double k = Math.Pow((h__meter + a_k[(int)env]) / b_k[(int)env], c_k[(int)env]);
            // equation 7
            double p_LOS = V_max * ((1 - Math.Exp(-k * (theta__deg + C_e) / 90)) / (1 - Math.Exp(-k * (90 + C_e) / 90)));

            // compute conditional probability of FcLOS | LOS
            ///////////////////////////////////////////////////

            // equation 10(a-c)
            double kprime = a_kprime[(int)env] * Math.Exp(b_kprime[(int)env] * h__meter);
            double C_eprime = Math.Pow(f__ghz * 1e9, a_cprime[(int)env]) +
                              b_c1prime[(int)env] * Math.Exp(b_c2prime[(int)env] * h__meter) +
                              b_c3prime[(int)env] * Math.Exp(b_c4prime[(int)env] * h__meter) +
                              c_cprime[(int)env];
            double V_maxprime = Math.Min(a_vprime[(int)env] * h__meter + b_vprime[(int)env], 0) * Math.Pow(f__ghz, -0.55) + 100;
            // equation 9
            double p_FcLOS_LOS = V_maxprime * ((1 - Math.Exp(-kprime * (theta__deg + C_eprime) / 90)) / (1 - Math.Exp(-kprime * (90 + C_eprime) / 90)));

            // probability of being Fresnel clear LOS
            // equation 11
            double p_FcLOS = p_LOS * p_FcLOS_LOS / 100;

            double L_clt__db;
            if (0 <= p && p <= p_FcLOS)
            {
                // equation 15
                L_clt__db = Math.Log10(p / p_FcLOS) / Math.Log10(30);
            }
            else if (p <= p_LOS)
            {
                // equation 14
                L_clt__db = 6 / (p_LOS - p_FcLOS) * (p - p_FcLOS);
            }
            else
            {
                // NLOS clutter loss

                // equation 13(a-b)
                double mu = a_N[0] + b_N[0] * Math.Log(1 + (90 - theta__deg) / 90) + Math.Pow(f__ghz, c_N[0]);
                double sigma = a_N[1] + b_N[1] * Math.Log(1 + (90 - theta__deg) / 90) + Math.Pow(f__ghz, c_N[1]);
                // equation 12
                double p_prime = (p - p_LOS) / (100 - p_LOS);
                double q = InverseComplementaryCumulativeDistribution.Invoke(p_prime);
                double L = mu + q * sigma;
                double L_clt_NLOS__db = Math.Max(L, 6);

                // NLOS
                L_clt__db = L_clt_NLOS__db;
            }

            return L_clt__db;
        }

        /// <summary>
        /// Generate a set of curves for varying elevation angles
        /// </summary>
        /// <param name="f__ghz">Frequency, in GHz</param>
        public static void CurveSetForFrequency(double f__ghz, double h__meter, Environment env)
        {
            var pm = new PlotModel()
            {
                Background = OxyColors.White,
                Title = $"Cumulative distribution of clutter loss not exceeded for {f__ghz} GHz",
                Subtitle = $"RC1 Model; h = {h__meter}; env = {env.ToString()}"
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
                    Title = $"{theta__deg}°"
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
            OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, pm, Path.Combine(@"C:\outputs", "RC1-FreqeuncyCurves.png"));
        }

        public static void CompareWithBoulderMeasurements(Environment env)
        {
            var geodesy = new GeographicLibGeodesy();

            var filepath = @"C:\Users\wkozma\Desktop\JWG-Clutter\input-documents\USA-Measurements\Boulder_Downtown_GreenMesa_7601_20241114.json";

            var json = JObject.Parse(File.ReadAllText(filepath));

            double lowDeg = 2;
            double highDeg = 4;
            double f__ghz = 7.601;

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

                double L_fs__db = Common.FreeSpaceLoss(f__mhz, d__km);

                double L_c__db = L_btl__db - L_fs__db;
                losses.Add(L_c__db);
            }

            var pm = new PlotModel()
            {
                Background = OxyColors.White,
                Title = $"Downtown Boulder 7601 MHz Comparison",
                Subtitle = $"RC2 Model; env = {env.ToString()}"
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

            var losses_LowDeg = new List<double>();
            var losses_HighDeg = new List<double>();
            var losses_AvgDeg = new List<double>();

            // loop through each of the location p's
            for (double p = 0.01; p < 100; p += 0.01)
            {
                double L_ces__db = RC2.Invoke(f__ghz, lowDeg, p, 2.82, env);
                losses_LowDeg.Add(L_ces__db);

                L_ces__db = RC2.Invoke(f__ghz, highDeg, p, 2.82, env);
                losses_HighDeg.Add(L_ces__db);

                L_ces__db = RC2.Invoke(f__ghz, (lowDeg + highDeg) / 2, p, 2.82, env);
                losses_AvgDeg.Add(L_ces__db);
            }

            Mathematics.BinData(losses_LowDeg.ToArray(), out double[] bins_LowDeg, out _, out double[] probs_LowDeg, 0.1);
            Mathematics.BinData(losses_HighDeg.ToArray(), out double[] bins_HighDeg, out _, out double[] probs_HighDeg, 0.1);

            var cdfSeries_LowDeg = new LineSeries()
            {
                StrokeThickness = 2,
                Title = $"{lowDeg}°",
                Color = OxyColors.Purple,
                LineStyle = LineStyle.Dash
            };
            var cdfSeries_HighDeg = new LineSeries()
            {
                StrokeThickness = 2,
                Title = $"{highDeg}°",
                Color = OxyColors.Green,
                LineStyle = LineStyle.Dash
            };

            var sortedBins_3deg = bins_LowDeg.OrderBy(b => b).ToList();
            var sortedBins_6deg = bins_HighDeg.OrderBy(b => b).ToList();

            total = 0;
            for (int i = 0; i < bins_LowDeg.Length; i++)
            {
                // get index in bins of next sorted bin
                int j = Array.IndexOf(bins_LowDeg, sortedBins_3deg[i]);

                total += probs_LowDeg[j];
                cdfSeries_LowDeg.Points.Add(new DataPoint(bins_LowDeg[j], total));
            }

            total = 0;
            for (int i = 0; i < bins_HighDeg.Length; i++)
            {
                // get index in bins of next sorted bin
                int j = Array.IndexOf(bins_HighDeg, sortedBins_6deg[i]);

                total += probs_HighDeg[j];
                cdfSeries_HighDeg.Points.Add(new DataPoint(bins_HighDeg[j], total));
            }

            pm.Series.Add(cdfSeries_LowDeg);
            pm.Series.Add(cdfSeries_HighDeg);

            var cdfSeries_P2108 = new LineSeries()
            {
                StrokeThickness = 2,
                Title = $"P.2108",
                Color = OxyColors.Black,
                LineStyle = LineStyle.Dot
            };

            var L_P2108__db = new List<Double>();

            // loop through each of the location p's
            for (double p = 0.01; p < 100; p += 0.01)
            {
                P2108.AeronauticalStatisticalModel(f__ghz, (lowDeg + highDeg) / 2, p, out double L_P2108_ces__db);
                L_P2108__db.Add(L_P2108_ces__db);
            }
            Mathematics.BinData(L_P2108__db.ToArray(), out double[] bins_P2108, out _, out double[] probs_P2108, 0.1);

            var sortedBins_P2108 = bins_P2108.OrderBy(b => b).ToList();

            total = 0;
            for (int i = 0; i < bins_P2108.Length; i++)
            {
                // get index in bins of next sorted bin
                int j = Array.IndexOf(bins_P2108, sortedBins_P2108[i]);

                total += probs_P2108[j];
                cdfSeries_P2108.Points.Add(new DataPoint(bins_P2108[j], total));
            }

            pm.Series.Add(cdfSeries_P2108);

            pm.Legends.Add(new Legend()
            {
                LegendPosition = LegendPosition.BottomCenter,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendBorder = OxyColors.Black
            });

            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 800, Height = 600 };
            OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, pm, Path.Combine(@"C:\outputs", "RC2-BoulderMeasurements.png"));

            // print out statistics
            losses_AvgDeg.Sort();
            losses.Sort();
            var markers = new[] { 0.001, 0.002, 0.005 };
            Console.WriteLine("%\tModel\tData\tDelta");
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < markers.Length; j++)
                    Console.WriteLine($"{markers[j] * Math.Pow(10, i) * 100}%\t" +
                                      $"{losses_AvgDeg[Convert.ToInt32(losses_AvgDeg.Count * markers[j] * Math.Pow(10, i))]:0.00}\t" +
                                      $"{losses[Convert.ToInt32(losses.Count * markers[j] * Math.Pow(10, i))]:0.00}\t" +
                                      $"{losses_AvgDeg[Convert.ToInt32(losses_AvgDeg.Count * markers[j] * Math.Pow(10, i))] - losses[Convert.ToInt32(losses.Count * markers[j] * Math.Pow(10, i))]:0.00}");
        }
    }
}
