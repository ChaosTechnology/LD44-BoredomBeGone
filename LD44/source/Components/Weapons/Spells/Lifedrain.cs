using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;

namespace LD44.Components.Weapons.Spells
{
    [Spell("Lifedrain", "Drains your targets life from a distance\nand adds part of the drained health\nto your own.", 5, 100, 0, 2, 1)]
    class Lifedrain : OffHand
    {
        const float RANGE = 10;

        bool casting = false;
        float accumulatedDamage = 0;

        Characters.StickMan target;
        EffectText src, hit;

        protected override void InvokeSpell()
        {
            base.InvokeSpell();
            casting = true;
            hit = null;
            src = null;
            accumulatedDamage = 0;
        }

        protected override void FinishSpellInvocation()
        {
            base.FinishSpellInvocation();
            casting = false;
            PrintHits(false);
            target = null;
            EndCast();
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            parent.scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollision].Add(Suck);
        }

        void Suck()
        {
            if (target == null && casting)
            {
                Ray ray = new Ray(position, parent.scene.view.Direction * RANGE);
                ChaosFramework.Physics.RayImpact impact = parent.scene.physics.ShootRay(ray, NotMe);
                if (impact.physical != null)
                {
                    Characters.StickMan victim = impact.physical.creator as Characters.StickMan;
                    if (victim != null)
                        target = victim;
                }
            }

            if (target != null)
            {
                float dmg = attr.damage * parent.stats.magicDamage / target.stats.magicResistance;
                dmg *= ftime;
                target.DoDamageIncludingInitialHealth(dmg);
                parent.DoDamageIncludingInitialHealth(dmg / -2);
                accumulatedDamage += dmg;
                PrintHits(false);

                Vector3f dist = target.physics.state.position - position;
                if (target.health <= 0
                    || !target.alive
                    || Vector3f.Dot(parent.physics.state.baseTransform.row2.xyz, dist) < 0
                    || dist.LengthSq() > RANGE * RANGE
                    )
                    EndCast();
            }
        }

        bool NotMe(ChaosFramework.Physics.Physical p)
            => p != parent.physics;

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            scene.drawLayers[(int)WorldScene.DrawLayers.FillInstancers].Add(AddBands);
        }

        void AddBands()
        {
            parent.scene.lifeorbs.renderedBands.AddInstance(Matrix.Scaling(0.75f) * Matrix.Translation(position));
            parent.scene.lifeorbs.renderedBands.AddInstance(Matrix.Scaling(0.5f) * Matrix.Translation(position));

            if (target != null)
            {
                Vector3f targetPos = Vector3f.TransformCoordinate(
                    new Vector3f(0, target.boneTorso.length * 0.75f, 0),
                    target.boneTorso.restTransform * target.boneTorso.transform * target.physics.state.GetTransform()
                    );
                Vector3f d = targetPos - position;
                Vector3f localX = Vector3f.Normalize(Vector3f.Cross(new Vector3f(0, 1, 0), d));
                Vector3f localZ = Vector3f.Normalize(Vector3f.Cross(localX, d));

                Matrix transform = Matrix.IDENTITY;
                transform.row0 = new Vector4f(-localX, 0);
                transform.row1 = new Vector4f(-d * 2, 0);
                transform.row2 = new Vector4f(localZ, 0);
                transform.row3 = new Vector4f((targetPos + position) * 0.5f, 1);

                parent.scene.lifeorbs.renderedBands.AddInstance(Matrix.Scaling(-1.5f, 1, -1.5f) * transform);
                parent.scene.lifeorbs.renderedBands.AddInstance(transform);
            }
        }

        void PrintHits(bool reset)
        {
            if (target != null)
            {
                if (reset || hit == null)
                    hit = target.AddComponent<EffectText>();
                hit.text = "-" + ((int)accumulatedDamage).ToString();
                hit.color = new Rgba(1, 0, 0, 1);
                hit.position = target.headPos + new Vector3f(0, 1, 0);
                hit.lifeTime = 1;

                if (reset || src == null)
                    src = parent.AddComponent<EffectText>();
                src.text = "+" + ((int)accumulatedDamage / 2).ToString();
                src.color = new Rgba(0, 1, 0, 1);
                src.position = parent.headPos + new Vector3f(0, 1, 0);
                src.lifeTime = 1;

                if (reset)
                    accumulatedDamage = 0;
            }
        }

        public override Vector2f GetTargetCastingPosition()
            => 1;
    }
}
