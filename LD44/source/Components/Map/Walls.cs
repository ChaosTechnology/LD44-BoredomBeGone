using ChaosFramework.Shapes.Convex;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Collections;
using ChaosFramework.Components;
using ChaosFramework.Graphics.OpenGl.ChaosShader;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Physics;

namespace LD44.Components.Map
{
    class Walls : Component<WorldScene>
    {
        MeshContainer.Entry mesh, obelisk;
        MaterialContainer.Entry mat, obeliskMat;
        Shader shader;

        Physical physics;

        protected override void Create(CreateParameters cparams)
        {
            mesh = scene.game.meshes.Load("Models/Border.gmdl", this);
            obelisk = scene.game.meshes.Load("Models/Obelisk.gmdl", this);

            physics = new Physical(this);
            physics.shapes.Clear();
            physics.isStatic = true;
            physics.state.baseTransform = Matrix.Scaling(1, 1.666f, 1);
            physics.shapes.Add(scene.game.shapes.Load("Models/Border.obj", this).content);
            physics.shapes.Add(scene.game.shapes.Load("Models/Obelisk.obj", this).content);
            physics.state.position = new Vector3f(0, -HeightMap.mapHeight + 12, 0);
            scene.physics.Add(physics);

            mat = scene.game.materials.Load("Materials/Border.mat", this);
            obeliskMat = scene.game.materials.Load("Materials/Obelisk.mat",this);
            shader = scene.game.graphics.shaders.normalMap;
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)WorldScene.UpdateLayers.Move].Add(DontMove);
        }

        void DontMove()
            => physics.state.Move(ftime);

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            scene.drawLayers[(int)WorldScene.DrawLayers.World].Add(DrawWorld);
            scene.drawLayers[(int)WorldScene.DrawLayers.Material].Add(DrawMaterial);
        }

        void DrawWorld()
            => Draw("World");

        void DrawMaterial()
            => Draw("Material");

        void Draw(string pass)
        {
            scene.view.SetValues(shader, physics.state.GetTransform());
            mat.content.SetValues(shader);
            mesh.content.Draw(shader, pass);
            obeliskMat.content.SetValues(shader);
            obelisk.content.Draw(shader, pass);
        }
    }
}
