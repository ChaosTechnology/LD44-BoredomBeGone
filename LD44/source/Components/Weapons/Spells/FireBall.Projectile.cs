using ChaosFramework.Components;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Physics;
using ChaosFramework.Shapes.Convex;
using System;
using SysCol = System.Collections.Generic;

namespace LD44.Components.Weapons.Spells
{
    partial class FireBall
    {
        public class Projectile : StrictComponent<FireBall>
        {
            public Transparents.ParticleFire.Spawner fire;
            public Physical physics;
            public int remainingBounces = 5;
            public float intendedDamage;
            public int numExplosionParticles = 100;

            protected override void Create(CreateParameters cparams)
            {
                fire = AddComponent<Transparents.ParticleFire.Spawner>();
                fire.spawnInterval *= 0.5f;
                parent.parent.scene.physics.Add(physics = new Physical(this));
                physics.shapes.Clear();
                physics.shapes.Add(new SphereShape(Vector3f.EMPTY, 0.1f));
                physics.validateCollision = IsCollisionPartnerValid;
                physics.getRelevantShapes = GetRelevantShapes;
            }

            bool IsCollisionPartnerValid(Physical partner, CollisionData _)
                => partner != parent.parent.physics && !(partner.creator is Impact);

            SysCol.IEnumerable<Shape> GetRelevantShapes(Physical partner)
                => IsCollisionPartnerValid(partner, null) ? physics.shapes : null;

            public override void SetUpdateCalls()
            {
                base.SetUpdateCalls();
                scene.updateLayers[(int)WorldScene.UpdateLayers.Move].Add(Peace);
                scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollision].Add(Violence);
            }

            void Peace()
            {
                physics.state.Move(ftime);
                fire.position = physics.state.position;
                fire.velocity = physics.state.velocity;
            }

            void Violence()
            {
                foreach (CollisionData data in physics.currentCollisions)
                    if (data.p1 != parent.parent.physics && data.p2 != parent.parent.physics)
                    {
                        Characters.StickMan crisp = data.p1.creator as Characters.StickMan
                                                 ?? data.p2.creator as Characters.StickMan;

                        if (--remainingBounces < 0 || crisp != null)
                        {
                            parent.parent.scene.fire.Explode(physics.state.position, fire.diffusion * 10, fire.particleColor, numExplosionParticles);
                            Impact impact = parent.parent.scene.AddComponent<Impact>(CreateParameters.Create(physics.state.position, 1.0f));
                            impact.damage = intendedDamage * parent.parent.stats.magicDamage;
                            Dispose();
                            return;
                        }
                    }

                if (physics.state.position.y < -10)
                    Dispose();
            }

            protected override void DoDispose()
            {
                base.DoDispose();
                parent.parent.scene.physics.Remove(physics);
            }
        }
    }
}
