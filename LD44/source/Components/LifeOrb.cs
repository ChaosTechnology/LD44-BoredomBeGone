using ChaosFramework.Graphics.OpenGl.Instancing;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Collections;
using ChaosFramework.Components;
using ChaosFramework.Graphics.OpenGl.ChaosShader;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Physics;
using ChaosFramework.Shapes.Convex;
using static ChaosFramework.Math.Clamping;
using static ChaosFramework.Math.Constants;
using SysCol = System.Collections.Generic;

namespace LD44.Components
{
    public class LifeOrb : Component<WorldScene>
    {
        const float RENDER_DIST_SQ = 20f * 20f;
        const float MESH_RAD = 0.25f;
        const float MAX_HEALTH_SIZE = 50;

        public Physical physics, collectablePhysics;
        public float health { get; set; }
        public bool pickedUp = false;
        Transparents.ParticleFire.Spawner fire;
        float pickupDelay = 0.75f;

        protected override void Create(CreateParameters cparams)
        {
            physics = new Physical(this);
            physics.validateCollision = IsCollisionPartnerValid;
            physics.getRelevantShapes = GetRelevantShapes;
            physics.shapes.Add(new SphereShape(Vector3f.EMPTY, MESH_RAD));
            scene.physics.Add(physics);

            collectablePhysics = new Physical(this, false);
            collectablePhysics.validateCollision = IsCollectorValid;
            collectablePhysics.getRelevantShapes = GetCollectorShapes;
            collectablePhysics.shapes.Add(new SphereShape(Vector3f.EMPTY, 5));
            scene.physics.Add(collectablePhysics);
            collectablePhysics.isStatic = true;

            fire = AddComponent<Transparents.ParticleFire.Spawner>();
            scene.lifeorbs.orbs.Add(this);
        }

        bool IsCollisionPartnerValid(Physical partner, CollisionData _ = null)
            => Characters.Brains.ChosenOne.GimmeMyChosenOne(partner) != null || partner.creator is Map.HeightMap;

        SysCol.IEnumerable<Shape> GetRelevantShapes(Physical partner)
            => IsCollisionPartnerValid(partner, null) ? physics.shapes : null;

        bool IsCollectorValid(Physical collector, CollisionData _ = null)
            => Characters.Brains.ChosenOne.GimmeMyChosenOne(collector) != null;

        SysCol.IEnumerable<Shape> GetCollectorShapes(Physical partner)
            => IsCollectorValid(partner) ? collectablePhysics.shapes : null;

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();

#if DEBUG
            if (parent is Weapons.Spells.Lifedrain)
                throw new System.Exception("Please no");
#endif

            scene.updateLayers[(int)WorldScene.UpdateLayers.Move].Add(Move);
            scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollision].Add(Collect);
        }

        void Move()
        {
            if (pickedUp)
            {
                physics.state.velocity = Vector3f.Normalize(scene.player.physics.state.position - physics.state.position) * 6.66f;
                physics.state.Move(ftime);
            }
            else
            {
                float scale = Min(0.25f + (float)System.Math.Sqrt(health / MAX_HEALTH_SIZE), 1) * 2;
                physics.state.baseTransform = Matrix.Scaling(scale, scale, scale);
                if (fire != null)
                    fire.position = physics.state.position;

                fire.spawnInterval = float.MaxValue;
                if (physics.isStatic)
                {
                    physics.state.baseTransform *= Matrix.RotationY(ftime);
                    float yPos = scene.map.GetHeightAt(physics.state.position.x, physics.state.position.z);
                    yPos += (float)System.Math.Sin(ftime.totalTime) * 0.5f + 1 + scale * MESH_RAD;
                    physics.state.position = new Vector3f(physics.state.position.x, yPos, physics.state.position.z);
                }
                else
                    physics.state.Move(ftime);
            }

            if ((health -= health * ftime * 0.1f) < 1)
            {
                scene.fire.Explode(physics.state.position, 1, new Rgba(1, 0, 0.3f, 1));
                Dispose();
                return;
            }
        }

        void Collect()
        {
            collectablePhysics.state.position = physics.state.position;
            collectablePhysics.state.velocity = physics.state.velocity;
            pickupDelay -= ftime;
            foreach (ChaosFramework.Physics.CollisionData collision in physics.currentCollisions)
            {
                if (pickupDelay < 0)
                {
                    Characters.Brains.ChosenOne brain = Characters.Brains.ChosenOne.GimmeMyChosenOne(collision);
                    if (brain != null)
                    {
                        brain.myMan.DoDamage(-health);
                        EffectText text = scene.AddComponent<EffectText>();
                        text.text = "+" + (int)(health + 0.5f);
                        text.position = brain.myMan.headPos + new Vector3f(0, 0.5f, 0);
                        text.color = new Rgba(0, 1, 0, 1);
                        Dispose();
                        return;
                    }
                }

                Map.HeightMap map = collision.p1.creator as Map.HeightMap;
                if (map == null)
                    map = collision.p2.creator as Map.HeightMap;
                if (map != null)
                    physics.isStatic = true;
            }

            if (physics.isStatic)
                if (pickupDelay < 0)
                    foreach (ChaosFramework.Physics.CollisionData a in collectablePhysics.currentCollisions)
                        if (Characters.Brains.ChosenOne.GimmeMyChosenOne(a) != null)
                            pickedUp = true;
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();

            if ((physics.state.position - scene.view.Position).LengthSq() < RENDER_DIST_SQ)
                scene.drawLayers[(int)HudScene.DrawLayers.FillInstancers].Add(AddInstances);
        }

        void AddInstances()
        {
            scene.lifeorbs.renderedBands.AddInstance(Matrix.Scaling(0.75f) * Matrix.RotationY(ftime.totalTime % PI_2) * physics.state.GetTransform());
            scene.lifeorbs.renderedCrystals.AddInstance(Matrix.Scaling(0.75f) * Matrix.RotationY(ftime.totalTime % PI_2) * physics.state.GetTransform());
        }

        public void MergeDispose(LifeOrb mergeWith)
        {
#if DEBUG
            if (parent is Weapons.Spells.Lifedrain)
                throw new System.Exception("Please no");
#endif

            mergeWith.health += health;
            Dispose();
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            scene.lifeorbs.orbs.Remove(this);
            scene.physics.Remove(physics);
            scene.physics.Remove(collectablePhysics);
        }
    }

    public class LifeOrbs : Component<WorldScene>, Transparent
    {
        const float MERGE_DISTANCE_SQ = 2 * 2;
        public MatrixInstancer renderedBands;
        public MatrixInstancer renderedCrystals;
        public AdvancedLinkedList<LifeOrb> orbs = new AdvancedLinkedList<LifeOrb>();

        MeshContainer.Entry bandMesh;
        TextureContainer.Entry bandTexture;
        MeshContainer.Entry crystalMesh;
        MaterialContainer.Entry crystalMat;
        Shader shader;

        protected override void Create(CreateParameters cparams)
        {
            bandMesh = scene.game.meshes.Load("Models/LifeOrb Bands.gmdl", this);
            crystalMesh = scene.game.meshes.Load("Models/LifeOrb Crystal.gmdl", this);
            bandTexture = scene.game.textures.Load("Textures/Particles/LifeOrb.png", this);
            shader = scene.game.shaders.Load("Shaders/LifeOrb.fx", this);
            crystalMat = scene.game.materials.Load("Materials/Crystal.mat", this);
            renderedBands = new MatrixInstancer(scene.game.graphics, null, 1000, false);
            renderedCrystals = new MatrixInstancer(scene.game.graphics, null, 1000, false);
        }

        public void PrepareVertices()
            => renderedBands.UpdateBuffer();

        public void DrawMask(TransparencyRenderer renderer)
        {
            renderer.SetMaskRenderingValues(shader);
            renderer.view.SetValues(shader, Matrix.IDENTITY);
            bandMesh.content.DrawInstanced(shader, "Mask", renderedBands);
        }

        public void DrawTransparent(TransparencyRenderer renderer)
        {
            shader.SetValue("tex", bandTexture);
            shader.SetValue("scrollParams", new Vector4f(-ftime.totalTime, 0, 0, 0));
            renderer.view.SetValues(shader, Matrix.IDENTITY);
            bandMesh.content.DrawInstanced(shader, "Bands", renderedBands);
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            renderedBands.Reset();
            renderedCrystals.Reset();

            scene.drawLayers[(int)HudScene.DrawLayers.World].Add(DrawWorld);
            scene.drawLayers[(int)HudScene.DrawLayers.Material].Add(DrawMaterial);
        }

        void DrawWorld()
        {
            renderedCrystals.UpdateBuffer();
            Draw("World");
        }

        void DrawMaterial()
            => Draw("Material");

        void Draw(string pass)
        {
            scene.view.SetValues(scene.game.graphics.shaders.instancedNormalMap, Matrix.IDENTITY);
            crystalMat.content.SetValues(scene.game.graphics.shaders.instancedNormalMap);
            crystalMesh.content.DrawInstanced(scene.game.graphics.shaders.instancedNormalMap, pass, renderedCrystals);
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollisionImmutable].Add(ConsiderMerge);
        }

        void ConsiderMerge()
        {
            foreach (LifeOrb orb in orbs)
                if (orb.doUpdate)
                {
                    orbs.SetSubEnumerator();
                    foreach (LifeOrb other in orbs)
                    {
                        if (other.doUpdate && (other.physics.state.position - orb.physics.state.position).LengthSq() < MERGE_DISTANCE_SQ)
                            if (other.health > orb.health)
                                orb.MergeDispose(other);
                            else
                                other.MergeDispose(orb);
                    }
                }
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            renderedCrystals.Dispose();
            renderedBands.Dispose();
        }
    }
}
