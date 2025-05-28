using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClutterAnalysis
{
    internal static class InverseComplementaryCumulativeDistribution
    {
        public static double Invoke(double q)
        {
            double C_0 = 2.515517;
            double C_1 = 0.802853;
            double C_2 = 0.010328;
            double D_1 = 1.432788;
            double D_2 = 0.189269;
            double D_3 = 0.001308;

            double x = q;
            if (q > 0.5)
                x = 1.0 - x;

            double T_x = Math.Sqrt(-2.0 * Math.Log(x));

            double zeta_x = ((C_2 * T_x + C_1) * T_x + C_0) / (((D_3 * T_x + D_2) * T_x + D_1) * T_x + 1.0);

            double Q_q = T_x - zeta_x;

            if (q > 0.5)
                Q_q = -Q_q;

            return Q_q;
        }
    }
}
