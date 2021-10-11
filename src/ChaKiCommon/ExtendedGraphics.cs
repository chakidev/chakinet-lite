using System;
using System.Drawing;
using System.Drawing.Drawing2D;

// A simple extension to the Graphics class for extended 
// graphic routines, such, 
// as for creating rounded rectangles. 
// Because, Graphics class is an abstract class, 
// that is why it can not be inherited. Although, 
// I have provided a simple constructor 
// that builds the ExtendedGraphics object around a 
// previously created Graphics object. 
// Please contact: aaronreginald@yahoo.com for the most 
// recent implementations of
// this class. 
// http://www.codeproject.com/KB/GDI-plus/ExtendedGraphics.aspx?df=100&forumid=30033&exp=0&select=1269526
//
namespace System.Drawing.Extended
{

    /// <SUMMARY> 
    /// Inherited child for the class Graphics encapsulating 
    /// additional functionality for curves and rounded rectangles. 
    /// </SUMMARY> 
    public class ExtendedGraphics
    {

        private Graphics mGraphics;
        public Graphics Graphics
        {
            get { return this.mGraphics; }
            set { this.mGraphics = value; }
        }


        public ExtendedGraphics(Graphics graphics)
        {
            this.Graphics = graphics;
        }

        public void FillRoundRectangle(System.Drawing.Brush brush, Rectangle rect, int radius)
        {
            if (rect.Width - radius * 2 <= 0 || rect.Height - radius * 2 <= 0)
            {
                return;
            }
            GraphicsPath path = this.GetRoundedRect(rect, radius, 0);
            this.Graphics.FillPath(brush, path);
        }

        public void FillRoundRectangleUpper(System.Drawing.Brush brush, Rectangle rect, int radius, int height)
        {
            if (rect.Width - radius * 2 <= 0 || rect.Height - radius * 2 <= 0)
            {
                return;
            }
            GraphicsPath path = this.GetRoundedRectUpper(rect, radius, 0, height);
            this.Graphics.FillPath(brush, path);
        }

        public void FillRoundRectangleLower(System.Drawing.Brush brush, Rectangle rect, int radius, int height)
        {
            if (rect.Width - radius * 2 <= 0 || rect.Height - radius * 2 <= 0)
            {
                return;
            }
            GraphicsPath path = this.GetRoundedRectLower(rect, radius, 0, height);
            this.Graphics.FillPath(brush, path);
        }

        public void DrawRoundRectangle(System.Drawing.Pen pen, int x, int y,
          int width, int height, int radius)
        {
            Rectangle rectangle = new Rectangle(x, y, width, height);
            DrawRoundRectangle(pen, rectangle, radius);
        }

        public void DrawRoundRectangle(System.Drawing.Pen pen, Rectangle rect, int radius)
        {
            if (rect.Width - radius * 2 <= 0 || rect.Height - radius * 2 <= 0)
            {
                return;
            }
            GraphicsPath path = this.GetRoundedRect(rect, radius, (int)pen.Width);
            this.Graphics.DrawPath(pen, path);
        }


        #region Get the desired Rounded Rectangle path.
        private GraphicsPath GetRoundedRect(Rectangle baseRect, int radius, int lw)
        {
            // x,y - top left corner of rounded rectangle
            // width, height - width and height of round rect
            // radius - radius for corners
            // lw - line width (for Graphics.Pen)
            int x = baseRect.Left;
            int y = baseRect.Top;
            int width = baseRect.Width;
            int height = baseRect.Height;
            GraphicsPath gp = new GraphicsPath();
            int diameter = radius * 2;
            gp.AddArc(x, y, diameter, diameter, 180, 90);
            gp.AddLine(x + radius, y, x + width - radius - lw, y);
            gp.AddArc(x + width - diameter - lw, y, diameter, diameter, 270, 90);
            gp.AddLine(x + width - lw, y + radius + lw, x + width - lw, y + height - radius - lw);
            gp.AddArc(x + width - diameter - lw, y + height - diameter - lw, diameter, diameter, 0, 90);
            gp.AddLine(x + width - radius - lw, y + height - lw, x + radius, y + height - lw);
            gp.AddArc(x, y + height - diameter - lw, diameter, diameter, 90, 90);
            gp.AddLine(x, y + height - radius - lw, x, y + radius - lw);
            //            gp.CloseFigure();
            return gp;
        }

        private GraphicsPath GetRoundedRectLower(Rectangle baseRect, int radius, int lw, int h)
        {
            // x,y - top left corner of rounded rectangle
            // width, height - width and height of round rect
            // radius - radius for corners
            // lw - line width (for Graphics.Pen)
            int x = baseRect.Left;
            int y = baseRect.Top;
            int width = baseRect.Width;
            int height = baseRect.Height;
            GraphicsPath gp = new GraphicsPath();
            int diameter = radius * 2;
            gp.AddLine(x + width - lw, y + height - h, x + width - lw, y + height - radius - lw);
            gp.AddArc(x + width - diameter - lw, y + height - diameter - lw, diameter, diameter, 0, 90);
            gp.AddLine(x + width - radius - lw, y + height - lw, x + radius, y + height - lw);
            gp.AddArc(x, y + height - diameter - lw, diameter, diameter, 90, 90);
            gp.AddLine(x, y + height - radius - lw, x, y + height - h);
            gp.AddLine(x, y + height - h, x + width - lw, y + height - h);
            return gp;
        }

        private GraphicsPath GetRoundedRectUpper(Rectangle baseRect, int radius, int lw, int h)
        {
            // x,y - top left corner of rounded rectangle
            // width, height - width and height of round rect
            // radius - radius for corners
            // lw - line width (for Graphics.Pen)
            int x = baseRect.Left;
            int y = baseRect.Top;
            int width = baseRect.Width;
            int height = baseRect.Height;
            GraphicsPath gp = new GraphicsPath();
            int diameter = radius * 2;
            gp.AddLine(x, y + h, x, y + radius);
            gp.AddArc(x, y, diameter, diameter, 180, 90);
            gp.AddLine(x + radius, y, x + width - radius - lw, y);
            gp.AddArc(x + width - diameter - lw, y, diameter, diameter, 270, 90);
            gp.AddLine(x + width - lw, y + radius + lw, x + width - lw, y + h);
            gp.AddLine(x + width - lw, y + h, x, y + h);
            return gp;
        }




        #endregion
    }
}
