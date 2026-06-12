using ChaosFramework.Graphics.OpenGl.Instancing;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Collections;
using ChaosFramework.Components;
using ChaosFramework.Graphics.OpenGl.ChaosShader;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Physics;
using ChaosFramework.Shapes.Convex;
using ChaosFramework.Shapes;
using ChaosUtil.Primitives;
using System.Linq;
using SysCol = System.Collections.Generic;
using static ChaosFramework.Math.Constants;
using static ChaosFramework.Math.Signs;
using ChaosFramework.Graphics.Imaging;
using ChaosFramework.Graphics.Imaging.Formats;

namespace LD44.Components.Map
{
    public class Trees : Component<WorldScene>
    {
        const int NUM_TREES = 666;

        static float SelectY(Vector3f v) => v.y;

        Shader shader;
        MatrixInstancer instancer;
        MeshContainer.Entry mesh;
        Material material;

        LinkedList<Physical> physics = [];

        protected override void Create(CreateParameters cparams)
        {
            instancer = new MatrixInstancer(scene.game.graphics, null, NUM_TREES, false);
            mesh = scene.game.meshes.Load("Models/Tree/Trunk.01.gmdl", this);
            material = scene.game.materials.Load("Materials/Tree/Trunk.01.mat", this);
            shader = scene.game.graphics.shaders.instancedNormalMap;

            LinkedList<Vector3f> branchOrigins = BranchOrigins(mesh.content.data);
            MeshShape collisionBase = new MeshShape(mesh.content.data.pos.ToArray(), true);

            using (System.IO.Stream str = Game.assetSource.OpenRead("Textures/Map/MapBounds.png"))
            {
                Rgba8Image bm = Png.FromStream(str);
                uint width = bm.w;
                uint height = bm.h;

                for (int i = 0; i < NUM_TREES; i++)
                {
                    Vector2f rnd = new Vector2f(Random.instance.Rnd(1), Random.instance.Rnd(1));
                    uint x = (uint)(rnd.x * width);
                    uint y = (uint)(rnd.y * height);
                    byte discardChance = bm[x, y].g;
                    if (Random.instance.RndByte() >= discardChance)
                        continue;

                    Vector3f pos = new Vector3f(HeightMap.MAP_SIZE * (rnd.x - 0.5f), 0, HeightMap.MAP_SIZE * (0.5f - rnd.y));
                    if (Abs(pos.x - scene.satan.position.x) < WorldScene.SATANS_SAFESPACE ||
                        Abs(pos.z - scene.satan.position.y) < WorldScene.SATANS_SAFESPACE)
                        continue;

                    float h = Random.instance.Rnd(0.3f, 1.0f);
                    h *= h;
                    h *= 3.5f;
                    pos.y = scene.map.GetHeightAt(pos.x, pos.z);
                    Matrix treeTransform = Matrix.Scaling(h) * Matrix.RotationY(Random.instance.Rnd(2 * PI)) * Matrix.Translation(pos);
                    instancer.AddInstance(treeTransform);

                    SysCol.IEnumerator<Vector3f> loopCenters = ((SysCol.IEnumerable<Vector3f>)branchOrigins).GetEnumerator();
                    loopCenters.MoveNext();

                    const float MIN_DEVIATION = PI_QUART;
                    Vector3f lowLoop = branchOrigins.first;
                    float angle = 0;
                    for (float branchHeight = 2; branchHeight < branchOrigins.last.y; branchHeight += Random.instance.Rnd(0.2f, 1))
                    {
                        angle += Random.instance.Rnd(MIN_DEVIATION, 2 * (PI - MIN_DEVIATION));
                        while (branchHeight > loopCenters.Current.y)
                        {
                            lowLoop = loopCenters.Current;
                            loopCenters.MoveNext();
                        }

                        Vector3f delta = loopCenters.Current - lowLoop;
                        Vector3f branchPosition = lowLoop + delta * (branchHeight - lowLoop.y) / delta.y;

                        instancer.AddInstance(
                            Matrix.RotationY(angle)
                            * Matrix.RotationX(PI_HALF + Random.instance.Rnd(0.5f))
                            * Matrix.RotationY(Random.instance.Rnd(2 * PI))
                            * Matrix.Scaling((1 - 0.78f * (branchHeight / branchOrigins.last.y)) * 0.35f)
                            * Matrix.Translation(branchPosition)
                            * treeTransform
                            );
                    }

                    Physical tree = new Physical(this);
                    tree.shapes.Clear();
                    tree.isStatic = true;
                    tree.shapes.Add(collisionBase.CloneTypeless());
                    tree.state.baseTransform = treeTransform;
                    scene.physics.Add(tree);
                    physics.Add(tree);
                }
            }

            instancer.UpdateBuffer();
        }

        LinkedList<Vector3f> BranchOrigins(MeshData mesh)
        {
            const float RING_MARGIN = 0.1f;

            float currentY = 0;
            LinkedList<Vector3f> ring = new LinkedList<Vector3f>();
            LinkedList<Vector3f> result = new LinkedList<Vector3f>();
            foreach (Vector3f pos in mesh.pos.OrderBy(SelectY))
            {
                if (pos.y > currentY + RING_MARGIN)
                {
                    result.Add(MergeRing(ring));
                    ring.Clear();
                    currentY = pos.y;
                }
                else
                    ring.Add(pos);
            }
            result.Add(MergeRing(ring));

            return result;
        }

        Vector3f MergeRing(LinkedList<Vector3f> ring)
        {
            Vector3f result = 0;
            foreach (Vector3f v in ring)
                result += v;
            return result / ring.length;
        }

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
            using (scene.game.graphics.stateTracker.Start())
            {
                scene.view.SetValues(shader, Matrix.IDENTITY);
                scene.game.graphics.stateTracker.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.CullFace, false);
                material.SetValues(shader);
                mesh.content.DrawInstanced(shader, pass, instancer);
            }
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            instancer.Dispose();
        }
    }
}
