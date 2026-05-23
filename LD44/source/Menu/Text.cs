using ChaosFramework.Graphics.Colors;
using ChaosFramework.Graphics;
using ChaosFramework.Graphics.Text;
using ChaosFramework.Graphics.OpenGl.Text;

namespace LD44.Menu
{
    public class Text
    {
        readonly HudScene scene;

        public TextMesh geo;
        LayoutInfo layout;

        public bool left => (align & Align.Left) != 0;
        public bool right => (align & Align.Right) != 0;
        public bool bottom => (align & Align.Bottom) != 0;
        public bool top => (align & Align.Top) != 0;
        public Rgba color = Rgba.OPAQUE_WHITE;
        public float scale = 1;

        string _text;
        public string text
        {
            get { return _text; }
            set
            {
                _text = value;
                scene.font.content.UpdateText(ref geo, text, layout);
            }
        }

        public Align align
        {
            get { return layout.align; }
            set
            {
                layout = new LayoutInfo(value);
                scene.font.content.UpdateText(ref geo, text, layout);
            }
        }

        public Text(HudScene scene, string text, Align align = Align.Center)
        {
            this.scene = scene;
            _text = text;
            this.align = align;
        }
    }
}
