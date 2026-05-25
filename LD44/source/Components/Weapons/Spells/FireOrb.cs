using ChaosFramework.Components;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Clamping;

namespace LD44.Components.Weapons.Spells
{
    [Spell("Fire Orb", "Throw a chargable fire ball.\nDrains health while charging.\nDamage grows with charging time.", 30, 50, 1, 0.5f, 0.5f)]
    class FireOrb : FireBall
    {
        const float TIME_UNTIL_MAX_CHARGE = 3f;

        float charge;
        float idleParticleSz;
        float idleDiffusion;

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            idleDiffusion = fire.diffusion;
            idleParticleSz = fire.particleSize;
            fire.particleColor = new Rgba(0.5f, 0.2f, 0.65f, 1);
        }

        protected override void InvokeSpell()
            => charge = 0;

        protected override void FinishSpellInvocation()
        {
            Projectile projectile = AddComponent<Projectile>();
            projectile.physics.state.position = projectile.fire.position = position;
            projectile.physics.state.velocity = projectile.fire.velocity
                = velocity + Vector3f.Normalize(parent.physics.state.GetTransform().row2.xyz) * (7 + charge * 8);
            projectile.intendedDamage = Clamp(0, 1, charge / TIME_UNTIL_MAX_CHARGE) * attr.damage;
            projectile.fire.particleSize = fire.particleSize;
            projectile.fire.diffusion = fire.diffusion;
            projectile.fire.particleColor = fire.particleColor;
            projectile.physics.state.baseTransform = Matrix.Scaling(fire.diffusion);
            fire.diffusion = idleDiffusion;
            fire.particleSize = idleParticleSz;
            EndCast();
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            if (state == State.Casting)
            {
                if ((charge = Min(TIME_UNTIL_MAX_CHARGE, charge + ftime)) >= TIME_UNTIL_MAX_CHARGE)
                    FinishSpellInvocation();

                parent.DoDamage(attr.drain * parent.stats.lifeDrainMagic * ftime);
                fire.diffusion = idleDiffusion / 2 + charge / TIME_UNTIL_MAX_CHARGE * idleDiffusion * 1.5f;
                fire.particleSize = idleDiffusion / 2 + charge / TIME_UNTIL_MAX_CHARGE * idleParticleSz * 1.5f;
            }
        }
    }
}
