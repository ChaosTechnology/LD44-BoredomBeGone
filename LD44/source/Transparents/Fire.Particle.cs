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
        public class FireParticle : Particle
        {
            public Vector3f velocity;

            Spawner spawner;
            float friction = 4f;
            float lifeTime;
            float targetSize;
            float maxLifeTime = 1;
            float growthRate = 0.2f;
            Matrix spin;
            Rgba color;

            public FireParticle(
                Time ftime,
                Spawner spawner,
                Vector3f position,
                float lifeTime,
                float size,
                float growthRate,
                Vector3f velocity,
                Rgba color
                ) : base(ftime)
            {
                this.spawner = spawner;
                this.growthRate = growthRate;
                maxLifeTime = this.lifeTime = lifeTime;
                targetSize = size;
                this.size = 0;
                this.position = position;
                this.velocity = velocity;
                spin = Matrix.RotationZ(Random.instance.Rnd() * PI_2);
                this.color = color;
            }

            public override bool Update()
            {
                position += velocity * ftime;
                velocity *= (1 - ftime * friction);
                velocity.y += 6 * ftime;
                lifeTime -= ftime;

                float f = 1 - lifeTime / maxLifeTime;
                if (f < growthRate)
                    size = (1 - (float)System.Math.Cos(f / growthRate * PI)) * 0.5f * targetSize;
                else
                    size = (1 + (float)System.Math.Cos((f - growthRate) / (1 - growthRate) * PI)) * 0.5f * targetSize;
                return lifeTime > 0;
            }

            protected override Matrix GetTransform(Camera view)
                => spin * Matrix.Scaling(size) * view.billBoard * Matrix.Translation(position);

            protected override Rgba GetColor()
                => new Rgba(color.rgb, lifeTime / maxLifeTime * color.a);

            public override void SetInstanceData(MatrixInstancer instancer, Camera view)
                => instancer.AddInstance(GetTransform(view), new Vector4f(0, 0, lifeTime / maxLifeTime, 0), GetColor().ToVec());
        }
    }
}
