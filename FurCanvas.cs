using Cosmos.HAL.Drivers.Video;
using Cosmos.System.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluffOS.Drawing
{
    public class FurCanvas : VBECanvas
    {
        public override void DrawPoint(Color aColor, int aX, int aY)
        {
            if (aX < 1 || aX > Mode.Width - 2 || aY < 1 || aY > Mode.Height - 2 || aColor.A == 0) return;
            base.DrawPoint(aColor, aX, aY);
        }
    }
}
