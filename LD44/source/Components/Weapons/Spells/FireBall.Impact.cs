using ChaosFramework.Components;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Physics;
using ChaosFramework.Shapes.Convex;
using System;

namespace LD44.Components.Weapons.Spells
{
    partial class FireBall
    {
        class Impact : Component<WorldScene>
        {
            const float MAX_TTL = 0.1f;

            public float ttl = MAX_TTL;
            public float damage;

            Physical dangerZone;

            protected override void Create(CreateParameters cparams)
            {
                CParams<Vector3f, float> args = cparams as CParams<Vector3f, float>;
                dangerZone = new Physical(this, false);
                dangerZone.isStatic = true;
                dangerZone.shapes.Add(new SphereShape(Vector3f.EMPTY, 1));
                dangerZone.state.baseTransform = Matrix.Scaling(args.v2) * Matrix.Translation(args.v1);
                scene.physics.Add(dangerZone);
            }

            public override void SetUpdateCalls()
            {
                base.SetUpdateCalls();
                scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollision].Add(Linger);
            }

            void Linger()
            {
                foreach (ChaosFramework.Physics.CollisionData data in dangerZone.currentCollisions)
                {
                    Characters.StickMan crisp = data.p1.creator as Characters.StickMan
                                             ?? data.p2.creator as Characters.StickMan;

                    if (crisp != null)
                        crisp.TakeHit(damage / crisp.stats.magicResistance * ftime / MAX_TTL);
                }

                if ((ttl -= ftime) < 0)
                    Dispose();
            }

            protected override void DoDispose()
            {
                base.DoDispose();
                scene.physics.Remove(dangerZone);
            }
        }
    }
}
