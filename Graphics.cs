using Cosmos.Core;
using Cosmos.Core.Memory;
using Cosmos.Core.Multiboot;
using Cosmos.System.Graphics;
using Cosmos.System.Graphics.Fonts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluffOS.Drawing
{
    /// <summary>
    /// Furry Unified Renderer API
    /// </summary>
    public unsafe class FURAPI
    {
        FurFrame[] OpenWindows;
        Canvas screen;
        FurFrame Active;
        public unsafe FURAPI(Canvas screen)
        {
            this.screen = screen;
            OpenWindows = Array.Empty<FurFrame>();
            Active = null;
        }
        public enum EventHandler
        {
            MouseDownLeft,
            MouseDownRight,
            MouseUpLeft,
            MouseUpRight,
            MouseMove,
            MouseScroll,
            KeyDown,
            KeyUp,
            MouseHover
        }
        public struct Event
        {
            public EventHandler Type;
            public int X;
            public int Y;
            public int Key;
            public int Scroll;
        }
        public abstract class FrameControl
        {
            public abstract bool IsVisible { get; set; }
            public abstract int X { get; set; }
            public abstract int Y { get; set; }
            public abstract int Width { get; set; }
            public abstract int Height { get; set; }
            public abstract FurFrame ParentFrame { get; set; }
            public abstract void OnEvent(Event e);
            public FrameControl(FurFrame parent)
            {
                ParentFrame = parent;

            }
           public abstract void Draw();
        }
        public class FurFrame
        {
            Canvas Screen;
            FrameControl[] Controls;
            /// <summary>
            /// The position of the top left of the window relative to the screen (Horizontal)
            /// </summary>
            public int RelativeX { get; set; }
            /// <summary>
            /// The position of the top left of the window relative to the screen (Vertical)
            /// </summary>
            public int RelativeY { get; set; }
            /// <summary>
            /// Width of the window in total
            /// </summary>
            public int RelativeWidth { get; set; }
            /// <summary>
            /// Height of the window in total
            /// </summary>
            public int RelativeHeight { get; set; }
            /// <summary>
            /// Width of the allowed drawing area
            /// </summary>
            public int Width { get; private set; }
            /// <summary>
            /// Height of the allowed drawing area
            /// </summary>
            public int Height { get; private set; }
            /// <summary>
            /// The buffer that the renderer will draw to
            /// </summary>
            /// <summary>
            /// The title of the window
            /// </summary>
            public string Title { get; set; }
            public bool IsVisible { get; set; }
            public bool HasBorder { get; set; }
            public FurFrame(Canvas screen, string title, int relativeX, int relativeY, int relativeWidth, int relativeHeight, bool hasBorder)
            {
                Screen = screen;
                RelativeX = relativeX;
                RelativeY = relativeY;
                RelativeWidth = relativeWidth;
                RelativeHeight = relativeHeight;
                Width = relativeWidth - 2;
                Height = relativeHeight - 22;
                Title = title;
                IsVisible = true;
                HasBorder = hasBorder;
            }
            public void Clear(Color color)
            {
                //fill the Width * height with color
                Screen.DrawFilledRectangle(color, RelativeX + 1, RelativeY + 21, Width, Height);
            }
            public void PlotPixel(int x, int y, Color color)
            {
                //if out of window bounds, return
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                {
                    return;
                }
                //plot the pixel
                Screen.DrawPoint(color, RelativeX + x + 1, RelativeY + y + 21);
            }
            private void HandleMouse()
            {

            }
            public void DrawFrame()
            {
                if (!IsVisible)
                {
                    return;
                }
                if (HasBorder)
                {
                    //Draw 20 px titlebar  using screen.DrawFilledRectangle(Color
                    Screen.DrawFilledRectangle(Color.White, RelativeX, RelativeY, RelativeWidth, 20);
                    //border around titlebar 1 px
                    Screen.DrawRectangle(Color.Gray, RelativeX, RelativeY, RelativeWidth, 20);
                    //Draw 1 px border around window
                    Screen.DrawRectangle(Color.Gray, RelativeX, RelativeY, RelativeWidth, RelativeHeight);
                    //Draw the title
                    Screen.DrawString(Title, PCScreenFont.Default, Color.Black, RelativeX + 2, RelativeY + 2);
                }
                
                //Draw the controls unless not visible
                foreach (FrameControl control in Controls)
                {
                    if (control.IsVisible)
                    {
                        control.Draw();
                    }
                }
            }
            public void Move(int x, int y)
            {
                RelativeX = x;
                RelativeY = y;
            }
            public void Resize(int width, int height)
            {
                RelativeWidth = width;
                RelativeHeight = height;
                //recalculate the drawing area
                Width = RelativeWidth - 2;
                Height = RelativeHeight - 22;
            }
            public void SetVisibility(bool visible)
            {
                IsVisible = visible;
            }
            public void SetBorder(bool border)
            {
                HasBorder = border;
            }
            public void SetTitle(string title)
            {
                Title = title;
            }
            public void RegisterControl(FrameControl control)
            {
                Controls = Controls.Append(control).ToArray();
            }
        }
        /// <summary>
        /// Handles the drawing of windows and the mouse cursor
        /// </summary>
        /// <param name="Title"> The title of the window</param>
        /// <param name="X"> The position of the top left of the window relative to the screen (Horizontal)</param>
        /// <param name="Y"> The position of the top left of the window relative to the screen (Vertical)</param>
        /// <param name="Width"> Width of the window in total</param>
        /// <param name="Height"> Height of the window in total</param>
        /// <returns> FurFrame</returns>
        public FurFrame CreateFrame(string Title, int X, int Y, int Width, int Height, bool HasBorder = true)
        {
            FurFrame frame = new(screen, Title, X, Y, Width, Height, HasBorder);
            OpenWindows = OpenWindows.Append(frame).ToArray();
            Active = frame;
            return frame;
        }
        /// <summary>
        /// If a frame was created outside of the API, you can register it here to be handled by the API
        /// </summary>
        /// <param name="frame"> The frame to register</param>
        public void RegisterFrame(FurFrame frame)
        {
            OpenWindows = OpenWindows.Append(frame).ToArray();
            Active = frame;
        }
        /// <summary>
        /// Removes a frame from the list of frames to be drawn
        /// </summary>
        /// <param name="frame"> The frame to remove</param>
        /// <returns> If the frame was successfully unregistered </returns>
        public bool RemoveFrame(FurFrame frame)
        {
            if (OpenWindows.Contains(frame))
            {
                OpenWindows = OpenWindows.Where(x => x != frame).ToArray();
                if (Active == frame)
                {
                    //find the one that was active before
                    Active = OpenWindows.FirstOrDefault();
                }
                return true;
            }
            return false;
        }
        /// <summary>
        /// Handles the drawing of windows
        /// </summary>
        public void DrawFrames()
        {
            foreach (FurFrame frame in OpenWindows)
            {
                frame.DrawFrame();
            }
        }
    }
}
