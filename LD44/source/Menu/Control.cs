using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Components;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Graphics.OpenGl.ChaosShader;
using ChaosFramework.Graphics.Colors;

namespace LD44.Menu
{
    public class Control<Scene> : Component<Scene>
        where Scene : HudScene
    {
        public TextureContainer.Entry texture;
        public Bounds2f bounds = new Bounds2f(-0.5f, -0.25f, 0.5f, 0.25f);
        public Text[] text;
        public Rgba txtCol = Rgba.OPAQUE_WHITE;
        public float textSz = -1;
        public Vector2f margin = Vector2f.EMPTY;

        Shader shader;

        protected override void Create(CreateParameters cparams)
        {
            shader = scene.game.graphics.shaders.spriteEffect;
            texture = scene.game.textures.Load("Textures/Menu/Button.png", this);
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            scene.drawLayers[(int)HudScene.DrawLayers.HUD].Add(Draw);
        }

        void Draw()
        {
            DrawControl();
            DrawText();
        }

        public virtual void DrawControl()
        {
            if (texture == null)
                return;

            scene.hudView.SetValues(
                shader,
                Matrix.Scaling(bounds.size.x / 2, bounds.size.y / 2, 1)
                    * Matrix.Translation(bounds.center.x, bounds.center.y, 0)
                );

            using (scene.game.graphics.stateTracker.Start())
            {
                scene.game.graphics.stateTracker.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.DepthTest, false);
                shader.SetValue("tex", texture);
                shader.BeginPass("Sprite");
                Sprite.DrawPositionOnly(scene.game.graphics);
                shader.EndPass();
            }
        }

        public virtual void DrawText()
        {
            if (text != null)
            {
                foreach (Text t in text)
                {
                    scene.font.content.SetValues(scene.textShader);
                    float x = (t.left ? bounds.left : (t.right ? bounds.right : bounds.center.x)) + margin.x;
                    float y = (t.top ? bounds.top : (t.bottom ? bounds.bottom : bounds.center.y)) + margin.y;
                    bool autoScale = textSz == -1;

                    scene.hudView.SetValues(
                        scene.textShader,
                        Matrix.Scaling(t.scale * (autoScale ? bounds.height / 2 : textSz))
                            * Matrix.Translation(x, y, 0)
                        );
                    scene.textShader.SetValue("color", Vector4f.ComponentWiseMul(txtCol.ToVec(), t.color.ToVec()));
                    t.geo.DrawText(scene.textShader, "HUD");
                    scene.textShader.SetValue("color", Rgba.OPAQUE_WHITE.ToVec());
                }
            }
        }
    }
}
