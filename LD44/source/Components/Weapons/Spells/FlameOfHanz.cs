using ChaosFramework.Math.Vectors;

namespace LD44.Components.Weapons.Spells
{
    [Spell("Flame of Hanz", "Hanz, get ze Flammenwerfer!", 5f, 100f, 1.5f, 1, 1)]
    class FlameOfHanz : FireBall
    {
        float interval = 0.02f;
        float nextBall = 0;

        protected override void InvokeSpell()
            => nextBall = 0;

        protected override void FinishSpellInvocation()
            => EndCast();

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            Yeet(); // TODO: decide on a layer for this
        }

        void Yeet()
        {
            if (state == State.Casting && (nextBall -= ftime) < 0)
            {
                nextBall += interval;
                Projectile projectile = AddComponent<Projectile>();
                projectile.physics.state.position = projectile.fire.position = position;
                projectile.physics.state.velocity = projectile.fire.velocity
                    = velocity + Vector3f.Normalize(parent.physics.state.GetTransform().row2.xyz) * 20;
                projectile.intendedDamage = attr.damage * interval;
                projectile.fire.spawnInterval *= 2;
                projectile.fire.particleSize *= 2f;
                projectile.fire.growthRate *= 2f;
                projectile.numExplosionParticles = 20;
                projectile.remainingBounces = 0;
                parent.DoDamage(attr.drain * parent.stats.lifeDrainMagic * interval);
            }
        }
    }
}
