using ChaosFramework.Shapes.Convex;
using ChaosFramework.Graphics.OpenGl.Instancing;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Components;
using ChaosFramework.Graphics;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Graphics.OpenGl.Particles;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Primitives;
using static ChaosFramework.Math.Constants;

namespace LD44.Transparents
{
    partial class ParticleFire
    {
        public class Spawner : Component
        {
            public bool active = true;
            public Shape graphicsShape;

            public float particleLightChance = 0;
            public ParticleFire target;
            public Shape spawnShape;
            public float particleLifeTime = 0.6f;
            public float particleSize = 0.2f;
            public Rgba particleColor = new Rgba(1, 0.45f, 0.1f, 1);
            public float spawnInterval = 0.03f;
            public float diffusion = 0.5f;
            public float growthRate = 0.2f;
            public bool spawnOnShapeOnly = false;
            float timer = 0;
            public Vector3f position;
            public Vector3f velocity;

            protected override void Create(CreateParameters cparams)
            {
                target = ((WorldScene)scene).fire;
                spawnShape = new SphereShape(Vector3f.EMPTY, 0.1f);
                graphicsShape = new SphereShape(Vector3f.EMPTY, 1f);
            }

            public override void SetUpdateCalls()
            {
                base.SetUpdateCalls();
                scene.updateLayers[(int)WorldScene.UpdateLayers.UpdateParticles].Add(Spawn);
            }

            void Spawn()
            {
                Matrix shapeTransform = Matrix.Translation(position);
                if (!active)
                    return;

                timer -= ftime;
                spawnShape.Update(shapeTransform);
                for (; timer < 0; timer += spawnInterval)
                {
                    Vector3f spawnPos;
                    if (spawnOnShapeOnly)
                        spawnPos = spawnShape.GetRandomPositionOnShape();
                    else
                        spawnPos = spawnShape.GetRandomPositionInShape();

                    float life = particleLifeTime;
                    FireParticle p = new FireParticle(
                        ftime,
                        this,
                        spawnPos,
                        life,
                        particleSize,
                        growthRate,
                        Random.instance.RndVector3(Random.instance.Rnd() * diffusion),
                        particleColor
                        );
                    target.particles.Add(p);
                    p.velocity += velocity;
                }
            }
        }
    }
}
