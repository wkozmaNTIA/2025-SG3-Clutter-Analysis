using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClutterAnalysis
{
    static internal class Common
    {
        /// <summary>
        /// Compute free space basic transmission loss
        /// </summary>
        /// <param name="f__mhz">Frequency, in MHz</param>
        /// <param name="d__km">Distance, in km</param>
        /// <returns>Loss, in dB</returns>
        public static double FreeSpaceLoss(double f__mhz, double d__km)
        {
            return 20 * Math.Log10(d__km) + 20 * Math.Log10(f__mhz) + 32.45;
        }
    }
}
