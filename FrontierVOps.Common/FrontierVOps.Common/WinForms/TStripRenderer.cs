using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FrontierVOps.Common.WinForms
{
    public class TStripRenderer : ToolStripProfessionalRenderer
    {
        public LinearGradientMode Mode { get; set; }
        public ProColorsTable ProColorTable { get; private set; }

        public TStripRenderer()
            : this(new ProColorsTable())
        {
            
        }

        public TStripRenderer(ProColorsTable pct)
        {
            Mode = LinearGradientMode.Horizontal;
            this.ProColorTable = pct;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new LinearGradientBrush(e.AffectedBounds, ProColorTable.MenuStripGradientBegin, ProColorTable.MenuStripGradientEnd, Mode))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }
    }
}
