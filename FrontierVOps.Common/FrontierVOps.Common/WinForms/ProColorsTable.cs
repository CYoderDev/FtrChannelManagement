using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FrontierVOps.Common.WinForms
{
    public class ProColorsTable : ProfessionalColorTable
    {
        public Color TStripGradientBegin { get; set; }
        public Color TStripGradientMiddle { get; set; }
        public Color TStripGradientEnd { get; set; }
        public Color MStripGradientBegin { get; set; }
        public Color MStripGradientEnd { get; set; }


        public ProColorsTable()
        {
            this.TStripGradientBegin = Color.DarkSlateGray;
            this.TStripGradientMiddle = Color.Gray;
            this.TStripGradientEnd = Color.LightGray;
            this.MStripGradientBegin = Color.LightGray;
            this.MStripGradientEnd = Color.DarkGray;
        }

        public override Color ToolStripGradientBegin
        {
            get
            {
                return this.TStripGradientBegin;
            }
        }

        public override Color ToolStripGradientMiddle
        {
            get
            {
                return this.TStripGradientMiddle;
            }
        }

        public override Color ToolStripGradientEnd
        {
            get
            {
                return this.ToolStripGradientEnd;
            }
        }

        public override Color MenuStripGradientBegin
        {
            get
            {
                return this.MStripGradientBegin;
            }
        }

        public override Color MenuStripGradientEnd
        {
            get
            {
                return this.MStripGradientEnd;
            }
        }
    }
}
