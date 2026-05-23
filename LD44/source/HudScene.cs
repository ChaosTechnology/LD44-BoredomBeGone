using ChaosFramework.Graphics;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Components;
using ChaosFramework.Graphics.OpenGl.ChaosShader;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Constants;

namespace LD44
{
    public abstract class HudScene : Scene<Game>
    {
        public enum DrawLayers
        {
            UpdateView,
            FillInstancers,
            BeginWorld,
            World,
            BeginMaterial,
            Material,
            PrepareSky,
            Sky,
            RenderDeferredShader,
            Transparents,
            Postprocessing,
            HUD,
            Cursor,
        }

        public readonly Camera hudView;
        public readonly FontContainer.Entry font;
        public readonly Shader textShader;

        public HudScene(Game g, System.Type numUpdate, System.Type numDraw)
            : base(g, numUpdate, numDraw)
        {
            hudView = new Camera();
            hudView.Update(new Vector3f(0, 0, -1), new Vector3f(0, 0, 1), new Vector3f(0, 1, 0), 0.5f, 1.5f, PI_QUART, g.graphics.ratio);
            font = game.fonts.Load("Fonts/font.chf2", this);
            textShader = game.graphics.shaders.text;
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            updateLayers[(int)WorldScene.UpdateLayers.PrepareUpdate].Add(UpdateCamera);
        }

        void UpdateCamera()
            => hudView.Update(
                hudView.Position,
                hudView.Direction,
                hudView.Up,
                hudView.nearClip,
                hudView.farClip,
                hudView.verticalViewAngle,
                game.graphics.ratio
                );
    }
}
