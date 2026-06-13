using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Components;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math;
using ChaosFramework.Platform;
using OpenTK.Graphics.OpenGL;

namespace LD44.Components
{
    public class DeathScreen : Menu.Control<WorldScene>
    {
        TextureContainer.Entry loadingScreen;
        bool firstFrame = true;

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            texture = null;
            text = new[] {
                new Menu.Text(scene, "You died.\n\n"),
                new Menu.Text(scene, "\n\n\n\n\nPress [Backspace] to retry.")
            };
            txtCol = new Rgba(1, 0, 0, 0);
            text[1].scale = 0.3f;
            txtCol.a = 0;
            loadingScreen = scene.game.textures.Load("LoadingScreen.png", this);
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();

            // let a frame pass before checking the suicide button so the Game can update the keyboard state in the meantime
            if (firstFrame)
                firstFrame = false;
            else
                scene.updateLayers[(int)WorldScene.UpdateLayers.Input].Add(ConsiderSuicide);

            scene.updateLayers[(int)WorldScene.UpdateLayers.UpdateParticles].Add(UpdateText);
        }

        void UpdateText()
            => txtCol.a = scene.player.myMan.health > 0 ? 0 : (float)System.Math.Abs(System.Math.Sin(ftime.totalTime));

        void ConsiderSuicide()
        {
            if (scene.game.keyboard[ChaosFramework.Input.Keyboard.HidUsage.Backspace].down)
            {
                // what were we thinking?
                scene.Dispose();
                GL.ClearColor(0, 0, 0, 0);
                ChaosFramework.Graphics.OpenGl.Graphics.ThrowErrors();
                GL.Clear(ClearBufferMask.ColorBufferBit);
                ChaosFramework.Graphics.OpenGl.Graphics.ThrowErrors();
                DrawLoadingScreen();
                scene.game.window.Present();
                scene.game.scenes.Add(new WorldScene(scene.game));
                Dispose();
                return;
            }
        }

        void DrawLoadingScreen()
        {
            TextureContainer.Entry tmpTex = texture;
            Bounds2f tmpBounds = bounds;
            texture = loadingScreen;
            bounds = new Bounds2f(-scene.game.window.Ratio(), -1, scene.game.window.Ratio(), 1);
            DrawControl();
            texture = tmpTex;
            bounds = tmpBounds;
        }
    }
}
