using Accord.Statistics;
using Ohiopyle.Data.Gdal.Raster;
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
    internal class ClutterAnalysis
    {
        static RasterFile _clutter;

        static public void ClutterStatistics()
        {
            _clutter = new RasterFile(@"C:\Environment\SaltLakeCity2013.Downtown.cltr.tif", new GeographicLibGeodesy());

            // representative clutter area
            double lat_NW = 40.769452;
            double lon_NW = -111.899547;
            double lat_SE = 40.756320;
            double lon_SE = -111.885306;

            // convert to UTM coords
            _clutter.ProjectPoint(lat_NW, lon_NW, out double plat1, out double plon1);
            _clutter.ProjectPoint(lat_SE, lon_SE, out double plat2, out double plon2);

            // extract clutter data
            ExtractClutterAreaValues(plat1, plon1, plat2, plon2, out List<double> heights_c, out double density);

            Mathematics.BinData(heights_c.ToArray(), out double[] bins_h, out _, out double[] probabilities_h);

            var pm = new PlotModel
            {
                Title = $"Distribution of Clutter Height in Salt Lake City",
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

            Console.WriteLine($"Clutter Count: {heights_c.Count}");
            Console.WriteLine($"Mean: {heights_c.Average()}");
            Console.WriteLine($"Median: {heights_c.ToArray().Median()}");
            Console.WriteLine($"Maximum: {heights_c.Max()}");
            Console.WriteLine($"St Dev: {heights_c.ToArray().StandardDeviation()}");
            Console.WriteLine($"IQR: {heights_c.ToArray().LowerQuartile()}-{heights_c.ToArray().UpperQuartile()}");
            Console.WriteLine($"Density: {density}");

            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 800, Height = 600 };
            OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, pm, Path.Combine(@"C:\outputs", "SaltLakeCity-Clutter-Distribution.png"));

            // dump clutter heights to csv
            File.WriteAllText(@"C:\outputs\SaltLakeCity-Clutter-Heights.csv", String.Join(",", heights_c.ToArray()));
        }

        static public void ClutterHeightComparisionCdf()
        {
            var root = @"C:\outputs";

            var files = new[]
            {
                "Denver-Clutter-Heights.csv",
                "Downtown-Boulder-Clutter-Heights.csv",
                "London-Clutter-Heights.csv",
                "Martin-Acres-Clutter-Heights.csv",
                "SaltLakeCity-Clutter-Heights.csv"
            };

            var names = new[]
            {
                "Denver",
                "Boulder",
                "London",
                "Martin Acres",
                "Salt Lake City"
            };

            var colors = new[]
            {
                OxyColors.Red,
                OxyColors.Orange,
                OxyColors.Blue,
                OxyColors.Purple,
                OxyColors.Aquamarine
            };

            var pm = new PlotModel()
            {
                Background = OxyColors.White,
                Title = $"Comparison of Distribution of Clutter Heights"
            };

            var xAxis = new LinearAxis();
            xAxis.Title = "Clutter Height (m)";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.MajorGridlineStyle = LineStyle.Solid;
            xAxis.Maximum = 60;
            pm.Axes.Add(xAxis);

            var yAxis = new LinearAxis();
            yAxis.Title = "Cummulative Probability";
            yAxis.Position = AxisPosition.Left;
            yAxis.MajorGridlineStyle = LineStyle.Solid;
            yAxis.MinorGridlineStyle = LineStyle.Solid;
            pm.Axes.Add(yAxis);

            for (int i = 0; i < files.Length; i++)
            {
                var heights = File.ReadAllText(Path.Combine(root, files[i])).Split(',').Select(x => Convert.ToDouble(x)).ToArray();

                Mathematics.BinData(heights.ToArray(), out double[] bins, out _, out double[] probs, 0.1);

                var cdfMeasurements = new LineSeries()
                {
                    StrokeThickness = 3,
                    Title = names[i],
                    Color = colors[i]
                };

                var sortedBins = bins.OrderBy(b => b).ToList();
                double total = 0;
                for (int j = 0; j < bins.Length; j++)
                {
                    // get index in bins of next sorted bin
                    int k = Array.IndexOf(bins, sortedBins[j]);

                    total += probs[k];
                    cdfMeasurements.Points.Add(new DataPoint(bins[k], total));
                }
                pm.Series.Add(cdfMeasurements);
            }

            pm.Legends.Add(new Legend()
            {
                LegendPosition = LegendPosition.BottomCenter,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendBorder = OxyColors.Black
            });

            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 800, Height = 600 };
            OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, pm, Path.Combine(@"C:\outputs", "Clutter-Height-Comparison.png"));
        }

        static void ExtractClutterAreaValues(double plat1, double plon1,
            double plat2, double plon2, out List<double> heights_c, out double density)
        {
            heights_c = new List<double>();
            int cnt = 0;

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
                        cnt++;

                        // only count clutter if 2 meters above terrain
                        if (elev_c > 2)
                            heights_c.Add(elev_c);
                    }
                }
            }

            density = heights_c.Count / (double)cnt;
        }
    }
}
