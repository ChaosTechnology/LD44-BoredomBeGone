using ChaosFramework.Components;
using ChaosFramework.Graphics;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.Text;
using ChaosFramework.Graphics.OpenGl.Text;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Clamping;

namespace LD44.Components
{
    public class HudDamageDisplay : Component<WorldScene>
    {
        public Vector3f position;
        public Vector3f velocity = new Vector3f(0, 1, 0);
        public Vector3f antiGravity = new Vector3f(0, 1, 0);
        public float lifeTime = 1;
        public Rgba color;
        public Camera view;

        TextMesh geo;
        string _text;
        public string text
        {
            get { return text; }
            set
            {
                _text = value;
                scene.font.content.UpdateText(ref geo, value, LayoutInfo.RIGHT);
            }
        }

        protected virtual float alpha
            => Min(1, lifeTime);

        protected override void Create(CreateParameters cparams)
        {
            view = scene.view;
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            // TODO: decide on a layer for this
            ConsiderSuicide();
        }

        void ConsiderSuicide()
        {
            position += velocity * ftime;
            velocity += antiGravity * ftime;
            lifeTime -= ftime;
            if (lifeTime < 0)
                Dispose();
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            scene.drawLayers[(int)WorldScene.DrawLayers.HUD].Add(DrawText);
        }

        void DrawText()
        {
            scene.font.content.SetValues(scene.textShader);
            scene.hudView.SetValues(scene.textShader, Matrix.Scaling(0.05f) * Matrix.Translation(position));
            scene.textShader.SetValue("color", new Vector4f(color.rgb.value, color.a * alpha));
            geo.DrawText(scene.textShader, "HUD");
            scene.textShader.SetValue("color", new Vector4f(1));
        }
    }
}
