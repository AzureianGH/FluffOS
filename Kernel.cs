using System;
using System.Collections.Generic;
using System.Text;
using Sys = Cosmos.System;
using Cosmos.System.Graphics;
using Cosmos.System.Graphics.Fonts;
using System.Drawing;
using FluffOS.Drawing;
using Cosmos.System;
using IL2CPU.API.Attribs;
namespace FluffOS
{
    public class Kernel : Sys.Kernel
    {
        Canvas screen;
        FURAPI.FurFrame frame;
        FURAPI api;
        public static Bitmap Cursor;
        [ManifestResourceStream(ResourceName = "FluffOS.Cursors.cnormal.bmp")]
        public static byte[] CursorNormal;

        protected override void BeforeRun()
        {
            screen = FullScreenCanvas.GetFullScreenCanvas();
            screen.Mode = new(1024, 768, ColorDepth.ColorDepth32);
            screen.Clear(Color.Black);
            api = new(screen);
            frame = api.CreateFrame("Window 1", 10, 10, 500, 500);
            frame.Title = "Hello, World!";
            
            MouseManager.ScreenWidth = 1024;
            MouseManager.ScreenHeight = 768;
            MouseManager.X = 512;
            MouseManager.Y = 384;
            Cursor = new Bitmap(CursorNormal);

        }
        public int x = 0;
        public int y = 0;
        protected override void Run()
        {
            screen.Clear(Color.Black);
            frame.Clear(Color.Blue);
            api.DrawFrames();
            screen.DrawImageAlpha(Cursor, (int)MouseManager.X, (int)MouseManager.Y);
            screen.Display();
        }
    }
}
