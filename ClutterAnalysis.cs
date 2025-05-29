using Ohiopyle.Data.Gdal.Raster;
using Ohiopyle.Geodesy.GeographicLib;
using OxyPlot;
using OxyPlot.Axes;
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
    internal class ClutterAnalysis
    {
        static RasterFile _clutter;

        static public void ClutterStatistics()
        {
            _clutter = new RasterFile(@"C:\Environment\Boulder2020.cltr.tif", new GeographicLibGeodesy());

            // representative clutter area
            double lat_NW = 39.997366;
            double lon_NW = -105.260818;
            double lat_SE = 39.990857;
            double lon_SE = -105.251333;

            // convert to UTM coords
            _clutter.ProjectPoint(lat_NW, lon_NW, out double plat1, out double plon1);
            _clutter.ProjectPoint(lat_SE, lon_SE, out double plat2, out double plon2);

            // extract clutter data
            ExtractClutterAreaValues(plat1, plon1, plat2, plon2, out List<double> heights_c);

            Mathematics.BinData(heights_c.ToArray(), out double[] bins_h, out _, out double[] probabilities_h);

            var pm = new PlotModel
            {
                Title = $"Distribution of Clutter Height in Martin Acres",
                Subtitle = $"Number of Samples = {heights_c.Count}",
                Background = OxyColors.White
            };

            var xAxis = new LinearAxis()
            {
                Title = "Clutter Height (meters)",
                Position = AxisPosition.Bottom,
            };
            pm.Axes.Add(xAxis);

            var yAxis = new LinearAxis()
            {
                Title = "Probability",
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Solid
            };
            pm.Axes.Add(yAxis);

            // add clutter height histogram data
            var histogramSeries = new HistogramSeries
            {
                StrokeThickness = 1,
                FillColor = OxyColors.Green,
                Title = "Environmental Data"
            };

            for (int i = 0; i < bins_h.Length; i++)
                histogramSeries.Items.Add(new HistogramItem(bins_h[i], bins_h[i] + 1, 1 * probabilities_h[i], 0));

            pm.Series.Add(histogramSeries);

            Console.WriteLine($"Average: {heights_c.Average()}");
            Console.WriteLine($"Maximum: {heights_c.Max()}");

            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 800, Height = 600 };
            OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, pm, Path.Combine(@"C:\outputs", "histogram.png"));
        }

        static void ExtractClutterAreaValues(double plat1, double plon1,
            double plat2, double plon2, out List<double> heights_c)
        {
            heights_c = new List<double>();

            // loop through all the pixels
            for (int x = 0; x < _clutter.SizeX; x++)
            {
                for (int y = 0; y < _clutter.SizeY; y++)
                {
                    // make sure pixel is inside box
                    _clutter.PixelToPoint(x, y, out double lon, out double lat);
                    _clutter.ProjectPoint(lat, lon, out double plat, out double plon);

                    if (plat1 > plat && plat > plat2 &&
                        plon1 < plon && plon < plon2)
                    {
                        // inside
                        double elev_c = _clutter.GetPixel(x, y);


                        // only count clutter if 2 meters above terrain
                        if (elev_c > 2)
                            heights_c.Add(elev_c);
                    }
                }
            }
        }
    }
}
