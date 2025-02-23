using System;
using System.Drawing;

namespace ioSenderTouch.Utility
{
    public static class DPIProvider
    {

        public static Dpi GetDpiScale()
        {
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                var x = g.DpiX / 96d;
                var y = g.DpiY / 96d;
                return new Dpi(x, y);
            }
        }
    }
    public class Dpi
    {
        public Dpi(double x, double y)
        {
            DpiX = x;
            DpiY = y;
        }
        public double DpiX { get; set; }
        public double DpiY { get; set; }
    }
}