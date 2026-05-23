using ChaosFramework.Components;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Physics;
using ChaosFramework.Shapes.Convex;
using System;

namespace LD44.Components.Weapons.Spells
{
    [Spell("Fire Ball", "Throw a single fire ball at your foes.", 15, 50, 5, 1, 1)]
    partial class FireBall : OffHand
    {
        protected Transparents.ParticleFire.Spawner fire;

        public override Vector3f position
        {
            get { return fire.position; }
            set { fire.position = value; }
        }

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            fire = AddComponent<Transparents.ParticleFire.Spawner>();
            fire.spawnInterval *= 2f;
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)WorldScene.UpdateLayers.Move].Add(Burn);
        }

        void Burn()
        {
            fire.position = position;
            fire.velocity = velocity;
        }

        protected override void InvokeSpell()
        {
            base.InvokeSpell();
            Projectile ball = AddComponent<Projectile>();
            ball.physics.state.position = ball.fire.position = position;
            ball.physics.state.velocity = ball.fire.velocity = velocity + Vector3f.Normalize(parent.physics.state.GetTransform().row2.xyz) * 10;
            ball.intendedDamage = attr.damage;
            parent.DoDamage(attr.drain * parent.stats.lifeDrainMagic);
            EndCast();
        }

        public override Vector2f GetTargetCastingPosition()
            => new Vector2f(0.444f, 0.666f);
    }
}
