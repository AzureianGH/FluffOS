using Cosmos.Core;
using Cosmos.Core.Memory;
using Cosmos.Core.Multiboot;
using Cosmos.System;
using Cosmos.System.FileSystem.ISO9660;
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
        FurCanvas screen;
        FurFrame Active;
        internal bool DraggingWindow = false;
        public unsafe FURAPI(FurCanvas screen)
        {
            this.screen = screen;
            OpenWindows = Array.Empty<FurFrame>();
            Active = null;
        }
        public FurFrame ActiveFrame
        {
            get
            {
                return Active;
            }
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

        public enum SenderType
        {
            UserInput,
            Frame,
            FrameControl
        }

        public struct Sender
        {
            public SenderType Type;
            public object From;
        }

        public abstract class FrameControl
        {
            public abstract bool IsVisible { get; set; }
            public abstract bool UnhandledEvent { get; set; }
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

        public enum FrameModes
        {
            None,
            Windowed,
            Fullscreen,
            WindowedFullscreen
        }

        public class FurFrame
        {
            Canvas Screen;
            FrameControl[] Controls;
            public int RelativeX { get; set; }
            public int RelativeY { get; set; }
            public int RelativeWidth { get; set; }
            public int RelativeHeight { get; set; }
            public int Width { get; private set; }
            public int Height { get; private set; }
            public string Title { get; set; }
            public bool IsVisible { get; set; }
            public bool HasBorder { get; set; }
            public bool TitleBarDragging { get; private set; }
            public bool IsInWindowedFullscreen { get; set; }
            public bool IsInFullscreen { get; set; }
            public bool IsActiveWindow { get; set; }
            public Color BackGroundArea { get; set; }
            private bool Disposed = false;
            public FURAPI OwnerAPI { get; set; }
            private Point OldPoint;
            private Size OldSize;
            private Size MovingSize;

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
                TitleBarDragging = false;
                IsInFullscreen = false;
                IsInWindowedFullscreen = false;
                IsActiveWindow = false;
                OwnerAPI = null;
                Controls = Array.Empty<FrameControl>();
                BackGroundArea = Color.White;
            }

            public void PlotPixel(int x, int y, Color color)
            {
                if (Disposed) return;
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                {
                    return;
                }
                if (HasBorder) Screen.DrawPoint(color, RelativeX + x + 1, RelativeY + y + 21);
                else Screen.DrawPoint(color, RelativeX + x, RelativeY + y);
            }
            public void Dispose()
            {
                if (Disposed) return;
                Disposed = true;
                Screen = null;
                Controls = null;
                OwnerAPI = null;
            }
            public Point ToLocalSpace(int x, int y, bool inDrawingSpace = false)
            {
                if (Disposed) return new Point();
                if (!inDrawingSpace)
                {
                    if (x < RelativeX || x >= RelativeX + RelativeWidth || y < RelativeY || y >= RelativeY + RelativeHeight)
                    {
                        return new Point(-1, -1);
                    }
                    if (HasBorder) return new Point(x - RelativeX + 2, y - RelativeY + 22);
                    else return new Point(x - RelativeX, y - RelativeY);
                }
                else
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height)
                    {
                        return new Point(-1, -1);
                    }
                    return new Point(x, y);
                }
            }

            public Point ToGlobalSpace(int x, int y)
            {
                if (Disposed) return new Point();
                return new Point(x + RelativeX, y + RelativeY);
            }

            int outlinex = 0;
            int outliney = 0;
            MouseState LastState = MouseState.None;

            public void HandleMouse()
            {
                if (Disposed) return;
                if (!IsVisible) return;

                if (MouseManager.X >= RelativeX && MouseManager.X <= RelativeX + RelativeWidth && MouseManager.Y >= RelativeY && MouseManager.Y <= RelativeY + 20 && HasBorder && !TitleBarDragging)
                {
                    //check if the windows above block the window, if so, return
                    foreach (FurFrame frame in OwnerAPI.OpenWindows)
                    {
                        if (frame == this) continue;
                        if (frame.RelativeX <= RelativeX && frame.RelativeX + frame.RelativeWidth >= RelativeX + RelativeWidth && frame.RelativeY <= RelativeY && frame.RelativeY + frame.RelativeHeight >= RelativeY + RelativeHeight)
                        {
                            return;
                        }
                    }
                    if (MouseManager.MouseState == MouseState.Left && !OwnerAPI.DraggingWindow)
                    {
                        if (!IsInWindowedFullscreen && !IsInFullscreen) MovingSize = new Size(RelativeWidth, RelativeHeight);
                        if (IsInWindowedFullscreen)
                        {
                            MovingSize = OldSize;
                        }
                        TitleBarDragging = true;
                        OwnerAPI.DraggingWindow = true;
                        outlinex = (int)(MouseManager.X - RelativeX);
                        outliney = (int)MouseManager.Y - RelativeY;
                        LastState = MouseState.Left;
                        if (!this.IsActiveWindow)
                        {
                            OwnerAPI.Active.IsActiveWindow = false;
                            OwnerAPI.Active = this;
                            this.IsActiveWindow = true;
                        }
                    }
                }

                if (TitleBarDragging)
                {
                    if (MouseManager.MouseState == MouseState.Left && LastState == MouseState.Left && MouseManager.Y < 3)
                    {
                        Screen.DrawRectangle(Color.Blue, 0, 0, (int)Screen.Mode.Width, (int)Screen.Mode.Height - 1);
                        Screen.DrawRectangle(Color.Blue, 1, 1, (int)Screen.Mode.Width - 1, (int)Screen.Mode.Height - 2);
                    }
                    if (MouseManager.MouseState == MouseState.None && LastState == MouseState.Left)
                    {
                        TitleBarDragging = false;
                        OwnerAPI.DraggingWindow = false;

                        if (MouseManager.Y < 3)
                        {
                            OldPoint = new Point(RelativeX, RelativeY);
                            OldSize = new Size(RelativeWidth, RelativeHeight);
                            RelativeX = 0;
                            RelativeY = 0;
                            RelativeWidth = (int)Screen.Mode.Width;
                            RelativeHeight = (int)Screen.Mode.Height;
                            IsInWindowedFullscreen = true;
                        }
                        else if (IsInWindowedFullscreen)
                        {
                            IsInWindowedFullscreen = false;
                            Resize(OldSize.Width, OldSize.Height);
                            Move(OldPoint.X, OldPoint.Y);
                        }
                        else
                        {
                            Move((int)MouseManager.X - outlinex, (int)MouseManager.Y - outliney);
                        }
                    }
                    Screen.DrawRectangle(Color.White, (int)MouseManager.X - outlinex, (int)MouseManager.Y - outliney, MovingSize.Width, MovingSize.Height);
                }

                foreach (FrameControl control in this.Controls)
                {
                    if (!control.IsVisible) continue;
                    // Handle mouse events for controls if needed
                }

                HandleCloseButton();
            }

            private void HandleCloseButton()
            {
                if (Disposed) return;
                if (HasBorder && MouseManager.X >= RelativeX + RelativeWidth - 24 && MouseManager.X <= RelativeX + RelativeWidth - 4 && MouseManager.Y >= RelativeY + 2 && MouseManager.Y <= RelativeY + 18)
                {
                    if (MouseManager.MouseState == MouseState.Left && LastState == MouseState.None)
                    {
                        OwnerAPI.RemoveFrame(this);
                    }
                }
            }

            public void DrawFrame()
            {
                if (Disposed) return;
                if (!IsVisible) return;

                if (HasBorder)
                {
                    Screen.DrawFilledRectangle(Color.White, RelativeX, RelativeY, RelativeWidth, 20);
                    Screen.DrawRectangle(Color.Gray, RelativeX, RelativeY, RelativeWidth, 20);
                    Screen.DrawRectangle(Color.Gray, RelativeX, RelativeY, RelativeWidth, RelativeHeight);
                    Screen.DrawString(Title, PCScreenFont.Default, Color.Black, RelativeX + 2, RelativeY + 2);
                    Screen.DrawString("[X]", PCScreenFont.Default, Color.Black, RelativeX + RelativeWidth - 24, RelativeY + 2);
                }
                //draw drawing area
                Screen.DrawFilledRectangle(BackGroundArea, RelativeX + 1, RelativeY + 21, RelativeWidth - 2, RelativeHeight - 22);

                foreach (FrameControl control in Controls)
                {
                    if (control.IsVisible) control.Draw();
                }
            }

            public void Move(int x, int y)
            {
                if (Disposed) return;
                RelativeX = x;
                RelativeY = y;
            }

            public void Resize(int width, int height)
            {
                if (Disposed) return;
                RelativeWidth = width;
                RelativeHeight = height;
                Width = RelativeWidth - 2;
                Height = RelativeHeight - 22;
            }

            public void SetVisibility(bool visible)
            {
                if (Disposed) return;
                IsVisible = visible;
            }

            public void SetBorder(bool border)
            {
                if (Disposed) return;
                HasBorder = border;
            }

            public void SetTitle(string title)
            {
                if (Disposed) return;
                Title = title;
            }

            public void RegisterControl(FrameControl control)
            {
                if (Disposed) return;
                Controls = Controls.Append(control).ToArray();
            }
        }

        public FurFrame CreateFrame(string Title, int X, int Y, int Width, int Height, bool HasBorder = true)
        {
            FurFrame frame = new(screen, Title, X, Y, Width, Height, HasBorder);
            OpenWindows = OpenWindows.Append(frame).ToArray();
            frame.IsActiveWindow = true;
            Active = frame;
            frame.OwnerAPI = this;
            return frame;
        }

        public void RegisterFrame(FurFrame frame)
        {
            OpenWindows = OpenWindows.Append(frame).ToArray();
            frame.IsActiveWindow = true;
            Active = frame;
            frame.OwnerAPI = this;
        }

        public bool RemoveFrame(FurFrame frame)
        {
            if (OpenWindows.Contains(frame))
            {
                OpenWindows = OpenWindows.Where(x => x != frame).ToArray();
                frame.IsActiveWindow = false;
                frame.OwnerAPI = null;
                if (Active == frame)
                {
                    //Next frame in line
                    if (OpenWindows.Length > 0)
                    {
                        Active = OpenWindows[OpenWindows.Length - 1];
                        Active.IsActiveWindow = true;
                    }
                    else
                    {
                        Active = null;
                    }
                }
                return true;
            }
            return false;
        }

        public void DrawFrames()
        {
            foreach (FurFrame frame in OpenWindows)
            {
                frame.DrawFrame();
            }
        }
    }
}
