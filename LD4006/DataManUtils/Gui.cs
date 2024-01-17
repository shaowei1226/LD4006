using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Drawing.Drawing2D;

namespace Cognex.DataMan.SDK.Utils
{
    /// <summary>
    /// Contains utility functions related to image display and manipulation.
    /// </summary>
    public class Gui
    {
        /// <summary>
        /// The maximum zoom factor that can be applied to images.
        /// </summary>
        public static double MaximumImageZoomFactor = 16.0;

        /// <summary>
        /// Calculates the zoom factor for placing the image into a control, while keeping the aspect ratio of the image.
        /// </summary>
        /// <param name="imageSize">The size of the image.</param>
        /// <param name="controlSize">The size of the control.</param>
        /// <returns>The zoom factor to be applied to the specified image, in order to fit it into the specified control.</returns>
        public static double GetZoomFactorForImageInControl(Size imageSize, Size controlSize)
        {
            if (imageSize.Height <= 0 || imageSize.Width <= 0 || controlSize.Height <= 0 || controlSize.Width <= 0)
                return 1.0;

            double image_aspect_ratio = (double)imageSize.Width / imageSize.Height;
            double control_aspect_ratio = (double)controlSize.Width / controlSize.Height;

            if (control_aspect_ratio < image_aspect_ratio)
            {
                return Math.Min(MaximumImageZoomFactor, (double)controlSize.Width / (double)imageSize.Width);
            }
            else
            {
                return Math.Min(MaximumImageZoomFactor, (double)controlSize.Height / (double)imageSize.Height);
            }
        }

        /// <summary>
        /// Fits an image into a control, while keeping the aspect ratio of the image.
        /// </summary>
        /// <param name="imageSize">The size of the image.</param>
        /// <param name="controlSize">The size of the control.</param>
        /// <param name="zoomFactor">On return it contains the calculated zoom factor.</param>
        /// <returns>The calculated size of the zoomed image.</returns>
        public static Size FitImageInControl(Size imageSize, Size controlSize, out double zoomFactor)
        {
            zoomFactor = GetZoomFactorForImageInControl(imageSize, controlSize);

            return new Size((int)Math.Round(imageSize.Width * zoomFactor), (int)Math.Round(imageSize.Height * zoomFactor));
        }

        /// <summary>
        /// Fits an image into a control, while keeping the aspect ratio of the image.
        /// </summary>
        /// <param name="imageSize">The size of the image.</param>
        /// <param name="controlSize">The size of the control.</param>
        /// <returns>The calculated size of the zoomed image.</returns>
        public static Size FitImageInControl(Size imageSize, Size controlSize)
        {
            double zoom_factor;

            return FitImageInControl(imageSize, controlSize, out zoom_factor);
        }

        /// <summary>
        /// Calculates the boundary rectangle of an image that is placed into a control, while keeping its aspect ratio. 
        /// </summary>
        /// <param name="imageSize">The size of the image.</param>
        /// <param name="controlSize">The size of the control.</param>
        /// <param name="zoomFactor">On return it contains the calculated zoom factor.</param>
        /// <returns>The boundary rectangle for the image, zoomed and centered as necessary.</returns>
        public static Rectangle FitImageInControl(Size imageSize, Rectangle controlSize, out double zoomFactor)
        {
            Size fitted_image_size = FitImageInControl(imageSize, controlSize.Size, out zoomFactor);
            int horizontal_gap = Math.Max(0, fitted_image_size.Width - controlSize.Width);
            int vertical_gap = Math.Max(0, fitted_image_size.Height - controlSize.Height);
            Rectangle boundary_rectangle;

            if (vertical_gap < horizontal_gap)
            {
                //image is filling up the control in Y direction: center image horizontally in control
                boundary_rectangle = new Rectangle(controlSize.Left + horizontal_gap / 2, 0, fitted_image_size.Width, fitted_image_size.Height);
            }
            else
            {
                //image is filling up the control in X direction: center image vertically in control
                boundary_rectangle = new Rectangle(0, controlSize.Top + vertical_gap / 2, fitted_image_size.Width, fitted_image_size.Height);
            }

            return boundary_rectangle;
        }

        /// <summary>
        /// Calculates the boundary rectangle of an image that is placed into a control, while keeping its aspect ratio. 
        /// </summary>
        /// <param name="imageSize">The size of the image.</param>
        /// <param name="controlSize">The size of the control.</param>
        /// <returns>The boundary rectangle for the image, zoomed and centered as necessary.</returns>
        public static Rectangle FitImageInControl(Size imageSize, Rectangle controlSize)
        {
            double zoom_factor;

            return FitImageInControl(imageSize, controlSize, out zoom_factor);
        }

        /// <summary>
        /// Resizes the specified image to the specified size.
        /// </summary>
        /// <param name="image">The image to be resized.</param>
        /// <param name="desiredSize">The desired size of the image.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImageToBitmap(Image image, Size desiredSize)
        {
            Bitmap resized_image = new Bitmap(desiredSize.Width, desiredSize.Height);
			using (Graphics resized_image_graphics = Graphics.FromImage((Image)resized_image))
			{
#if !WindowsCE
				resized_image_graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
#endif
				resized_image_graphics.DrawImage(image, new Rectangle(0, 0, desiredSize.Width, desiredSize.Height), new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
			}

            return resized_image;
        }

        /// <summary>
        /// Returns the byte representation of the specified image in the specified format.
        /// </summary>
        /// <param name="image">The image whose byte data is requested.</param>
        /// <param name="format">The format of the image for which the byte data is requested.</param>
        /// <returns>The byte array that represents the specified image in the specified format.</returns>
        public static byte[] BitmapToBytes(Bitmap image, System.Drawing.Imaging.ImageFormat format)
        {
            try
            {
                if (image == null)
                    return null;

                MemoryStream memory_stream = new MemoryStream();

                image.Save(memory_stream, format);
                
                byte[] image_bytes = memory_stream.GetBuffer();
                
                memory_stream.Close();
                memory_stream = null;

                return image_bytes;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a bitmap image from the specified image byte data.
        /// </summary>
        /// <param name="imageData">The array that contains the image data.</param>
        /// <returns>The image created from the specified image data.</returns>
        public static Bitmap BytesToBitmap(byte[] imageData)
        {
            if (imageData == null)
                return null;

            return BytesToBitmap(imageData, 0, imageData.Length);
        }

        /// <summary>
        /// Creates a bitmap image from a section of the specified image byte data.
        /// </summary>
        /// <param name="buffer">The array that contains the image data.</param>
        /// <param name="offset">The offset of the beginning of the image data within the specified array.</param>
        /// <param name="count">The number of image data bytes within the specified array.</param>
        /// <returns>The image created from the specified image byte data.</returns>
        public static Bitmap BytesToBitmap(byte[] buffer, int offset, int count)
        {
            try
            {
                if (buffer == null)
                    return null;

                if (buffer.Length < 1 || count < 1)
                    return null;

                MemoryStream memory_stream = new MemoryStream();

                memory_stream.Write(buffer, 0, buffer.Length);
                memory_stream.Seek(0, SeekOrigin.Begin);

                // we need to construct 2 bitmaps so we can close the memory stream before returning.
                // From MS Documentation, "You must keep the stream open for the lifetime of the Bitmap."
                Bitmap temp_image = new Bitmap(memory_stream);
                Bitmap final_image = new Bitmap(temp_image);

                temp_image.Dispose();
                temp_image = null;
                
                memory_stream.Close();
                memory_stream = null;

                return final_image;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a bitmap image from the specified image byte data stream.
        /// </summary>
        /// <param name="imageStream">The stream that provides the image byte data.</param>
        /// <param name="imageDataSize">The number of bytes to read from the specified stream.</param>
        /// <returns>The image created from the specified image byte data stream.</returns>
        public static Bitmap StreamToBitmap(Stream imageStream, int imageDataSize)
        {
            try
            {
                if (imageStream == null || !imageStream.CanRead)
                    return null;

                if (imageDataSize < 1)
                    return null;

                byte[] image_data = new byte[imageDataSize];
                
                if (imageStream.Read(image_data, 0, imageDataSize) != imageDataSize) 
                    return null;
                
                return Gui.BytesToBitmap(image_data);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Represents a DMCC read result graphics.
    /// </summary>
    public class ResultGraphics
    {
        /// <summary>
        /// The list of polygons found in the DMCC read result graphics (frame rectangle as well as boundary polygon around the code)
        /// </summary>
        public List<ResultPolygon> Polygons;

        /// <summary>
        /// View box boundaries found in the DMCC read result graphics (these are the original image dimensions)
        /// </summary>
        public Size ViewBoxSize;    
        
        /// <summary>
        /// The original DMCC graphics result as received from the reader.
        /// </summary>
        public string OriginalSvgData;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ResultGraphics" /> class.
        /// </summary>
        public ResultGraphics()
        {
            Polygons = new List<ResultPolygon>();
            ViewBoxSize = new Size(1280, 1024);   //default graphics size, according to modern readers
            OriginalSvgData = "";
        }
    }

    /// <summary>
    /// Represents a polygon in a DMCC read result graphics.
    /// </summary>
    public class ResultPolygon
    {
        /// <summary>
        /// The default color of the polygon.
        /// </summary>
        public static Color DefaultPolygonColor = Color.LawnGreen;

        /// <summary>
        /// Contains the points that make up the polygon.
        /// </summary>
        public Point[] Points;

        /// <summary>
        /// The color of the polygon.
        /// </summary>
        public Color Color;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultPolygon" /> class.
        /// </summary>
        public ResultPolygon()
        {
            Clear();
        }

        internal void Set(Point[] points)
        {
            Points = points;
        }

        internal void Clear()
        {
            Points = new Point[0];
            Color = DefaultPolygonColor;
        }
    }

    /// <summary>
    /// A parser that takes a raw read result graphics descriptor (SVG data) and turns it into a ResultGraphics object.
    /// </summary>
    public static class GraphicsResultParser
    {
        private static Regex m_RegexpViewBox = new Regex(@"(\d+)\s+(\d+)\s+(\d+)\s+(\d+)");

        /// <summary>
        /// Converts an RGB value to a Color.
        /// </summary>
        /// <param name="argbValue">The integer that contain the Alpha, Red, Green and Blue color components.</param>
        /// <returns>The Color object created from the specified integer that contains the Alpha, Red, Green and Blue color components.</returns>
        public static Color UIntToColor(uint argbValue)
        {
#if WindowsCE
            return Color.FromArgb((int)((argbValue >> 16) & 0xff), (int)((argbValue >> 8) & 0xff), (int)((argbValue >> 0) & 0xff));
#else
            return Color.FromArgb(255, (int)((argbValue >> 16) & 0xff), (int)((argbValue >> 8) & 0xff), (int)((argbValue >> 0) & 0xff));
#endif
        }

        /// <summary>
        /// Parses the specified SVG data and creates a ResultGraphics object that if rendered fits into the specified control rectangle.
        /// </summary>
        /// <param name="svgData">The raw SVG data received from the reader.</param>
        /// <param name="displayControlRect">The rectangle into which the result will be rendered.</param>
        /// <returns>The ResultGraphics object created from the specified raw SVG data.</returns>
        public static ResultGraphics Parse(string svgData, Rectangle displayControlRect)
        {
            ResultGraphics result_graphics = new ResultGraphics();
            
            result_graphics.OriginalSvgData = svgData;

            // Parsing ViewBox size from the following node:   viewBox="0 0 1280 1024"
            int viewBoxIndex = svgData.IndexOf("viewBox=\"");

            if (viewBoxIndex > 0)
            {
                Match VBM = m_RegexpViewBox.Match(svgData, viewBoxIndex);

                if (VBM.Groups.Count > 4)
                    result_graphics.ViewBoxSize = new Size(Int32.Parse(VBM.Groups[3].Value), Int32.Parse(VBM.Groups[4].Value));
            }

            double Zoom;
            Rectangle GraphicRect = Gui.FitImageInControl(result_graphics.ViewBoxSize, displayControlRect, out Zoom);
            Point GraphicShift = new Point((displayControlRect.Width - GraphicRect.Width) / 2, (displayControlRect.Height - GraphicRect.Height) / 2);

            //If there is only image but no decoded result we don't want to parse out the coordinates
            int dataLength = svgData.Length;
            int pointsIndex = svgData.IndexOf("points", 0, dataLength);
            int colorIndex = svgData.IndexOf("stroke=\"#", 0, dataLength);

            int startIndex;
            int endIndex;
            string coordsString;
            string[] coordsArray;

            //parsing one or more polygons
            while (pointsIndex != -1)
            {
                ResultPolygon Polygon = new ResultPolygon();
                bool isDecoderROI = false;

                //parsing polygon color
                colorIndex = svgData.IndexOf("stroke=\"#", colorIndex, dataLength - colorIndex);
                if (colorIndex >= 0)
                {
                    try
                    {
                        uint ColorValue = UInt32.Parse(svgData.Substring(colorIndex + 9, 6), System.Globalization.NumberStyles.HexNumber);
                        Polygon.Color = UIntToColor(ColorValue);
                        colorIndex += 9;
                        // Check if it's the Decoder ROI
                        if (Polygon.Color.R == 0 && Polygon.Color.G == 0 && Polygon.Color.B == 255) isDecoderROI = true;
                    }
                    catch { }
                }

                //parsing polygon points
                List<Point> Points = new List<Point>();
                
                startIndex = svgData.IndexOf("points", pointsIndex, dataLength - pointsIndex) + 8;
                endIndex = svgData.IndexOf('"', startIndex, dataLength - startIndex) - 1;
                coordsString = svgData.Substring(startIndex, endIndex - startIndex);
                coordsArray = coordsString.Split(' ', ',');

                Point LastPoint = new Point();
                for (int i = 0; i < coordsArray.Length; i += 2)
                {
                    int PointX = (int)Math.Round(Convert.ToInt32(coordsArray[i + 0]) * Zoom) + GraphicShift.X;
                    int PointY = (int)Math.Round(Convert.ToInt32(coordsArray[i + 1]) * Zoom) + GraphicShift.Y;

                    //
                    // If this is the blue decoder ROI, make sure to subtract 1 from each of the X,Y coodinates so that the lines
                    // show up in the image at the bottom and right side of the screen.
                    if (isDecoderROI)
                    {
                        if (PointX != 0) --PointX;
                        if (PointY != 0) --PointY;
                    }

                    LastPoint = new Point(PointX, PointY);
                    Points.Add(LastPoint);
                }

                //Adding the first point twice makes drawing easier (see: Graphics.DrawLines)
                if (Points.Count > 0)
                    Points.Add(Points[0]);

                Polygon.Set(Points.ToArray());
                result_graphics.Polygons.Add(Polygon);
                pointsIndex = svgData.IndexOf("points", pointsIndex + 1, dataLength - pointsIndex - 1);
            }

            return result_graphics;
        }
    }

    /// <summary>
    /// Utility class that can be used to render graphics result to a graphics surface.
    /// </summary>
    public static class ResultGraphicsRenderer
    {
        /// <summary>
        /// Renders the specified graphics results to the specified drawing surface.
        /// </summary>
        /// <param name="graphics">The drawing surface onto which the result graphics are rendered.</param>
        /// <param name="resultGraphics">The result graphics that is rendered to the specified drawing surface.</param>
        public static void PaintResults(Graphics graphics, ResultGraphics resultGraphics)
        {
            if (resultGraphics != null && resultGraphics.Polygons != null && resultGraphics.Polygons.Count > 0)
            {
                Pen pen = new Pen(resultGraphics.Polygons[0].Color);

                foreach (ResultPolygon polygon in resultGraphics.Polygons)
                {
                    if (!pen.Color.Equals(polygon.Color))
                        pen.Color = polygon.Color;

                    graphics.DrawLines(pen, polygon.Points);
                }
            }
        }
    }
}
