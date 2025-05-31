using Newtonsoft.Json.Linq;
using Ohiopyle.Geodesy.GeographicLib;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using Pennsylvania;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClutterAnalysis
{
    internal static class K182
    {
        public enum Environment : int
        {
            LowRise = 0,
            MidRise = 1,
            HighRise = 2
        };

        private static double[] a_k = new[] { 4.9, -2.6, 2.4 };
        private static double[] b_k = new[] { 6.7, 6.6, 7 };
        private static double[] c_k = new[] { 2.6, 2, 1 };

        private static double[] a_c = new[] { 0.19, 0.42, 0.19 };
        private static double[] b_c = new[] { 0, -6.7, -2.7 };

        private static double[] a_v = new[] { 1.4, 0.15, 0.15 };
        private static double[] b_v = new[] { 74.0, 97, 98 };

        private static double[] a_kprime = new[] { 6, 3.6, 5 };
        private static double[] b_kprime = new[] { 0.07, 0.05, 0.003 };

        private static double[] a_cprime = new[] { 0.15, 0.17, 0.17 };
        private static double[] b_c1prime = new[] { 5.4, 13, 32.6 };
        private static double[] b_c2prime = new[] { -0.3, -0.2, 0.012 };
        private static double[] b_c3prime = new[] { 3.2, 3.7, -23.9 };
        private static double[] b_c4prime = new[] { 0.07, 0.05, -0.07 };
        private static double[] c_cprime = new double[] { -27, -41, -41 };

        private static double[] a_vprime = new[] { 1.6, 1, 1 };
        private static double[] b_vprime = new double[] { -17, -21, -18 };

        private static double[] h_s = new double[] { 10, 20, 30 };

        public static double Invoke(double f__ghz, double theta__deg, double p, 
            double h__meter, Environment env)
        {

            double V_max = Math.Min(a_v[(int)env] * h__meter + b_v[(int)env], 100);
            double C_e = a_c[(int)env] * h__meter + b_c[(int)env];
            double k = Math.Pow((h__meter + a_k[(int)env]) / b_k[(int)env], c_k[(int)env]);
            double p_los = V_max * ((1 - Math.Exp(-k * (theta__deg + C_e) / 90)) / (1 - Math.Exp(-k * (90 + C_e) / 90)));

            double kprime = a_kprime[(int)env] * Math.Exp(b_kprime[(int)env] * h__meter);
            double C_eprime = Math.Pow(f__ghz * 10e9, a_cprime[(int)env]) +
                              b_c1prime[(int)env] * Math.Exp(b_c2prime[(int)env] * h__meter) +
                              b_c3prime[(int)env] * Math.Exp(b_c4prime[(int)env] * h__meter) +
                              c_cprime[(int)env];
            double V_maxprime = Math.Min(a_vprime[(int)env] * h__meter + b_vprime[(int)env], 0) * Math.Pow(f__ghz, -0.55) + 100;
            double FcLoSp = V_maxprime * ((1 - Math.Exp(-kprime * (theta__deg + C_eprime) / 90)) / (1 - Math.Exp(-kprime * (90 + C_eprime) / 90)));

            double FcLoSpprime = p_los * FcLoSp / 100;

            double A = 7.78 + 0.23 * theta__deg;
            double B = -30 + 8 * Math.Log10(theta__deg + 1);
            double C_t = B + A * Math.Log10(h_s[(int)env]);

            double L_clt__db = Double.MaxValue;
            if (p <= FcLoSpprime)
            {
                // Line-of-sight

                // equation 17
                L_clt__db = Math.Log10(p / FcLoSpprime) / Math.Log10(16);
            }
            else if (FcLoSpprime < p && p <= p_los)
            {
                // Fresnel obstructed LOS

                // equation 18
                L_clt__db = (6 / (p_los - FcLoSpprime)) * (p - FcLoSpprime);
            }
            else
            {
                // NLOS

                // equation 12, but dropping vegetative loss term
                L_clt__db = C_t * C(p) + L_ces(p, f__ghz, theta__deg) -
                            (C_t * C(p_los) + L_ces(p_los, f__ghz, theta__deg)) +
                            6 - 0.1 * (h__meter - 5) * (p - p_los) / 100;
            }

            return L_clt__db;
        }

        private static double C(double p) => 0.7 * p / 100;

        private static double L_ces(double p, double f__ghz, double theta__deg)
        {
            double K_1 = 93 * Math.Pow(f__ghz, 0.175);
            double A_1 = 0.05;

            // equation 16
            double term1 = -K_1 * Math.Log(1 - p / 100);
            double term2 = 1 / Math.Tan(A_1 * (1 - theta__deg / 90) + Math.PI * theta__deg / 180);
            double term3 = 0.5 * (90 - theta__deg) / 90;
            double L_ces__db = Math.Pow(term1 * term2, term3) - 1 -
                0.6 * InverseComplementaryCumulativeDistribution.Invoke(p / 100);

            return L_ces__db;
        }

        public static void CompareWithBoulderMeasurements(Environment env)
        {
            var geodesy = new GeographicLibGeodesy();

            var filepath = @"C:\Users\wkozma\Desktop\JWG-Clutter\input-documents\R23-WP3K-C-0148!P1!ZIP-E\Boulder_MartinAcres_GreenMesa_7601_20241114.json";

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

                double L_fs__db = Common.FreeSpaceLoss(f__mhz, d__km);

                double L_c__db = L_btl__db - L_fs__db;
                losses.Add(L_c__db);
            }

            var pm = new PlotModel()
            {
                Background = OxyColors.White,
                Title = $"Martin Acres 7601 MHz Comparison",
                Subtitle = $"3K/182 (Ericsson) Model; env = {env.ToString()}"
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
                double L_ces__db = K182.Invoke(7.601, 3, p, 2.82, env);
                losses_3deg.Add(L_ces__db);

                L_ces__db = K182.Invoke(7.601, 6, p, 2.82, env);
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
    }
}
