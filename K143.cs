using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClutterAnalysis
{
    internal class K143
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

        public static double[] a_L = new[] { 40.0, 16.0 };
        public static double[] b_L = new[] { -0.22, -0.25 };
        public static double[] c_L = new[] { -15, -4.64 };

        public static double[] a_N = new[] { 8.54, 0.04, 0.1 };
        public static double[] b_N = new[] { 17.57, 12, 4.59 };
        public static double[] c_N = new[] { 0.63, 0.38, 0.36 };

        public static double Invoke(double f__ghz, double theta__deg, double h__meter, Environment env)
        {
            double V_max = Math.Min(a_v[(int)env] * h__meter + b_v[(int)env], 100);
            double C_e = a_c[(int)env] * h__meter + b_c[(int)env];
            double k = Math.Pow((h__meter + a_k[(int)env]) / b_k[(int)env], c_k[(int)env]);
            double p_los = V_max * ((1 - Math.Exp(-k * (theta__deg + C_e) / 90)) / (1 - Math.Exp(-k * (90 + C_e) / 90)));

            double u_1 = a_L[0] * Math.Pow(theta__deg, b_L[0]) + c_L[0];
            double sigma = a_L[1] * Math.Pow(theta__deg, b_L[1]) + c_L[1];
            double u_2 = a_N[0] + b_N[0] * Math.Log(1 + ((90 - theta__deg) / 90)) + Math.Pow(f__ghz, c_N[0]);
            double alpha = a_N[1] + b_N[1] * Math.Log(1 + ((90 - theta__deg) / 90)) + Math.Pow(f__ghz, c_N[1]);
            double beta = a_N[2] + b_N[2] * Math.Log(1 + ((90 - theta__deg) / 90)) + Math.Pow(f__ghz, c_N[2]);

            //double f_los = 1 / (Math.Sqrt(2 * Math.PI) * sigma) * Math.Exp(-Math.Pow((), 2))

            return -1;
        }
    }
}
