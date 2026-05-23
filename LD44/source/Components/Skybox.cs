using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Components;
using ChaosFramework.Math;
using OpenTK.Graphics.OpenGL;

namespace LD44.Components
{
    public class Skybox : Component<WorldScene>
    {
        public CubeTexture skyReflection { get; private set; }
        CubeTexture sky;
        ShaderContainer.Entry shader;

        protected override void Create(CreateParameters cparams)
        {
            sky = CubeTexture.FromStreams(scene.game.graphics.dispatcher,
                Game.assetSource.OpenRead("Textures/Sky/Left_.png"),
                Game.assetSource.OpenRead("Textures/Sky/Right_.png"),
                Game.assetSource.OpenRead("Textures/Sky/Bottom_.png"),
                Game.assetSource.OpenRead("Textures/Sky/Top.png"),
                Game.assetSource.OpenRead("Textures/Sky/Back_.png"),
                Game.assetSource.OpenRead("Textures/Sky/Front_.png"));
            skyReflection = CubeTexture.FromStreams(scene.game.graphics.dispatcher,
                Game.assetSource.OpenRead("Textures/Sky/Left.png"),
                Game.assetSource.OpenRead("Textures/Sky/Right.png"),
                Game.assetSource.OpenRead("Textures/Sky/Bottom.png"),
                Game.assetSource.OpenRead("Textures/Sky/Top.png"),
                Game.assetSource.OpenRead("Textures/Sky/Back.png"),
                Game.assetSource.OpenRead("Textures/Sky/Front.png"));
            shader = scene.game.shaders.Load("Shaders/Sky.fx", this);
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            scene.drawLayers[(int)HudScene.DrawLayers.Sky].Add(DrawSky);
        }

        void DrawSky()
        {
            shader.content.SetValue("tex", sky);
            scene.view.SetValues(shader, Matrix.Translation(scene.view.Position));
            GL.BindVertexArray(scene.game.graphics.emptyVAO);
            Graphics.ThrowErrors();
            shader.BeginPass("Sky");
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 14);
            Graphics.ThrowErrors();
            shader.EndPass();
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            sky.Dispose();
            skyReflection.Dispose();
        }
    }
}
