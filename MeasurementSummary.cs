using Newtonsoft.Json.Linq;
using Ohiopyle.Geodesy.GeographicLib;
using Pennsylvania;
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
        public static void BoulderData()
        {
            var geodesy = new GeographicLibGeodesy();

            var filepath = @"C:\Users\wkozma\Desktop\JWG-Clutter\input-documents\R23-WP3K-C-0148!P1!ZIP-E\Boulder_MartinAcres_GreenMesa_7601_20241114.json";

            var json = JObject.Parse(File.ReadAllText(filepath));

            var losses = new List<double>();
            using (var fs = new StreamWriter(@"C:\outputs\Boulder_MartinAcres_GreenMesa_7601_20241114.csv"))
            {
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
                    fs.WriteLine(L_c__db);
                }
            }

            losses.Sort();

            var ps = new[] { 0.1, 0.2, 0.5, 1, 2, 5, 10, 20, 50 };
            foreach (var p in ps)
            {
                int i = Convert.ToInt32(losses.Count * p / 100);
                Console.WriteLine($"{p}% = {losses[i]:0.00} dB");
            }
        }
    }
}
