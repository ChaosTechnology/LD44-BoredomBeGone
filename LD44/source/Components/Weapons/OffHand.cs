using ChaosFramework.Collections;
using ChaosFramework.Collections.Immutable;
using ChaosFramework.Components;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Reflection;

namespace LD44.Components.Weapons
{
    public abstract class OffHand : StrictComponent<Characters.StickMan>
    {
        public enum State
        {
            Idle,
            Charging,
            Casting,
            Cooldown
        }

        public static readonly ImmutableArray<TypeWithAttribute<SpellAttribute>> spells;

        static OffHand()
        {
            TypeWithAttribute<SpellAttribute>[] spells = TypeWithAttribute<SpellAttribute>.GetTypes(typeof(OffHand));
            System.Array.Sort(spells, SpellAttribute.Compare);
            OffHand.spells = spells;
        }

        public readonly SpellAttribute attr;

        public virtual Vector3f position { get; set; }
        public virtual Vector3f velocity { get; set; }

        public State state { get; protected set; } = State.Idle;
        public float chargeTimer = 0;

        public OffHand()
        {
            attr = TypeWithAttribute<SpellAttribute>.GetType(GetType()).attribute;
        }

        protected override void Create(CreateParameters cparams)
        { }

        public void StartCast()
        {
            if (state == State.Idle)
                state = State.Charging;
        }

        public void EndCast()
        {
            switch (state)
            {
                case State.Charging:
                    state = State.Cooldown;
                    break;

                case State.Casting:
                    state = State.Cooldown;
                    FinishSpellInvocation();
                    break;
            }
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)WorldScene.UpdateLayers.Input].Add(Magic);
        }

        void Magic()
        {
            switch (state)
            {
                case State.Charging:
                    if ((chargeTimer += 1 / attr.castingTime * ftime) >= 1)
                    {
                        chargeTimer = 1;
                        state = State.Casting;
                        InvokeSpell();
                    }
                    break;

                case State.Cooldown:
                    if ((chargeTimer -= 1 / attr.coolDown * ftime) <= 0)
                    {
                        chargeTimer = 0;
                        state = State.Idle;
                    }
                    break;
            }
        }

        protected virtual void InvokeSpell() { }
        protected virtual void FinishSpellInvocation() { }

        public virtual Vector2f GetTargetCastingPosition()
            => new Vector2f(0, 0.5f);
    }
}
