using ChaosFramework.Components;
using ChaosFramework.Math;
using ChaosUtil.Primitives;
using System.Linq;

namespace LD44.Components.Characters.Brains
{
    class Enemy : ArmedStickman
    {
        protected override StickMan GetTarget()
        {
            StickMan victim = null;
            float closest = float.MaxValue;

            foreach (StickMan candidate in scene.EnumerateChildren<StickMan>(false))
                if (!GageInterest<ChosenOne>(candidate, ref closest, ref victim))
                    GageInterest<Ally>(candidate, ref closest, ref victim);

            return victim;
        }

        public override void Die()
        {
            base.Die();
            LifeOrb orb = scene.AddComponent<LifeOrb>();
            orb.physics.state.position = physics.state.position;
            orb.physics.state.velocity = Random.instance.RndVector3(2);
            orb.health = myMan.initialHealth;
        }
    }
}
