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
        FurCanvas screen;
        FURAPI.FurFrame frame;
        FURAPI.FurFrame frame2;
        FURAPI api;
        public static Bitmap Cursor;
        [ManifestResourceStream(ResourceName = "FluffOS.Cursors.cnormal.bmp")]
        public static byte[] CursorNormal;
        [ManifestResourceStream(ResourceName = "FluffOS.Cursors.calldir.bmp")]
        public static byte[] CursorAllDirection;

        public DateTime LT { get; private set; }
        public int FPS { get; private set; }
        public int Frames { get; private set; }

        protected override void BeforeRun()
        {
            screen = (FurCanvas)FullScreenCanvas.GetFullScreenCanvas();
            screen.Mode = new(1024, 768, ColorDepth.ColorDepth32);
            screen.Clear(Color.Black);
            api = new(screen);
            frame = api.CreateFrame("Window 1", 10, 10, 500, 500);
            frame2 = api.CreateFrame("Window 2", 60, 60, 500, 500);
            MouseManager.ScreenWidth = 1024;
            MouseManager.ScreenHeight = 768;
            MouseManager.X = 512;
            MouseManager.Y = 384;
            Cursor = new Bitmap(CursorNormal);
            frame.BackGroundArea = Color.Blue;
            frame2.BackGroundArea = Color.FromArgb(255, 0, 0, 125);
            LT = DateTime.Now;
            Frames = 0;
            FPS = 0;
        }
        protected override void Run()
        {
            screen.Clear(Color.Black);
            api.DrawFrames();
            frame.HandleMouse();
            frame2.HandleMouse();
            screen.DrawString($"FPS: {FPS}", PCScreenFont.Default, Color.AliceBlue, 0, 0);
            screen.DrawImageAlpha(Cursor, (int)MouseManager.X, (int)MouseManager.Y);
            screen.Display();
            Frames++;
            if ((DateTime.Now - LT).TotalSeconds >= 1)
            {
                Cosmos.Core.Memory.Heap.Collect();
                FPS = Frames;
                Frames = 0;
                LT = DateTime.Now;
            }
        }
    }
}
