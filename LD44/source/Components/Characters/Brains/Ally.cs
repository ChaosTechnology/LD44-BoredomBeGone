using ChaosFramework.Components;
using System.Linq;

namespace LD44.Components.Characters.Brains
{
    class Ally : ArmedStickman
    {
        public Ally()
        {
            deaggroRangeSq = aggroRangeSq = float.MaxValue;
        }

        protected override StickMan GetTarget()
        {
            StickMan victim = null;
            float closestBoss = float.MaxValue;
            float closestEnemy = float.MaxValue;

            foreach (StickMan candidate in scene.EnumerateChildren<StickMan>(false))
                if (!GageInterest<Boss>(candidate, ref closestBoss, ref victim))
                    if (victim == null || !(victim.brain is Boss))
                        GageInterest<Enemy>(candidate, ref closestEnemy, ref victim);

            return victim;
        }
    }
}
