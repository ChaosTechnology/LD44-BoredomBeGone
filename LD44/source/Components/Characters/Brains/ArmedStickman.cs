using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.Text;
using ChaosFramework.Graphics.OpenGl.Text;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Primitives;
using static ChaosFramework.Math.Clamping;
using static ChaosFramework.Math.Constants;
using static ChaosFramework.Math.Exponentials;

namespace LD44.Components.Characters.Brains
{
    abstract class ArmedStickman : StickmanController
    {
        const float MAX_ATTACK_DIST_SQ = 9;
        const float ATTACK_BASE_SPEED = 10;
        const float MARGIN_SQ = 0.01f;

        public float aggroRangeSq = 100;
        public float deaggroRangeSq = 225;
        public float minCastingRangeSq = 25;
        public float maxCastingRangeSq = 100;

        bool aggro = false;
        Vector2f swipeTarget;
        TextMesh healthText;
        Vector3f differenceToVictim = new Vector3f(float.NaN, 0, 0);

        public override void TakeControl()
            => scene.font.content.UpdateText(ref healthText, ((int)myMan.health).ToString(), LayoutInfo.BOTTOM);

        public override void Input()
        {
            if (myMan.mainHandWeapon != null)
            {
                bool idle = !aggro || differenceToVictim.LengthSq() > MAX_ATTACK_DIST_SQ || scene.player.myMan.health <= 0;
                if (idle)
                    swipeTarget += (new Vector2f(0.25f, 1) - swipeTarget) * ftime;

                if (!idle && (myMan.armRotation - swipeTarget).LengthSq() < MARGIN_SQ)
                {
                    Vector2f dir = Random.instance.RndVector2(0.5f) - Vector2f.Normalize(myMan.armRotation);
                    swipeTarget += dir * 3;
                }

                swipeTarget.x = Clamp(StickMan.MIN_ROT_X, StickMan.MAX_ROT_X, swipeTarget.x);
                swipeTarget.y = Clamp(StickMan.MIN_ROT_Y, myMan.MAX_ROT_Y, swipeTarget.y);

                Vector2f nor = swipeTarget - myMan.armRotation;
                float len = nor.Length();
                if (len > 1)
                    nor /= len;

                myMan.armRotationSpeed += (nor * ATTACK_BASE_SPEED - myMan.armRotationSpeed)
                                          * ftime * StickMan.WEAPON_BASE_INERTIA / myMan.mainHandWeapon.attr.weight;
            }
        }

        public override void Move()
        {
            StickMan target = GetTarget();
            if (target != null)
            {
                differenceToVictim = target.physics.state.position - physics.state.position;
                Vector3f differenceToTargetPosition = differenceToVictim - Vector3f.Normalize(differenceToVictim) * 1.666f;

                float howFarMustIWalkSq = differenceToTargetPosition.LengthSq();
                if (howFarMustIWalkSq < aggroRangeSq)
                    aggro = true;
                else if (howFarMustIWalkSq > deaggroRangeSq)
                    aggro = false;

                if (aggro && target.health > 0)
                {
                    float howFarMustIWalk = Sqrt(howFarMustIWalkSq);
                    if (howFarMustIWalk > 1)
                        differenceToTargetPosition /= howFarMustIWalk;

                    if (myMan.onGround && (myMan.offHandWeapon == null || myMan.offHandWeapon.state == Weapons.OffHand.State.Idle))
                        physics.state.velocity += (differenceToTargetPosition * myMan.stats.walkingSpeed - physics.state.velocity)
                                                  * ftime * 15;

                    Matrix transform = physics.state.GetTransform();
                    Vector2f viewXZ = transform.row2.xz;
                    float angleTarget = (float)System.Math.Atan2(-differenceToVictim.z, -differenceToVictim.x);
                    float angleDiff = angleTarget - (float)System.Math.Atan2(transform.m22, transform.m20);
                    if (angleDiff > PI) angleDiff -= PI_2;
                    if (angleDiff < -PI) angleDiff += PI_2;
                    physics.state.baseTransform *= Matrix.RotationY(Clamp(-PI * ftime, PI * ftime, angleDiff));
                }
            }
            scene.font.content.UpdateText(ref healthText, ((int)myMan.health).ToString(), LayoutInfo.BOTTOM);
        }

        public override bool TryingToCast()
        {
            float range = differenceToVictim.LengthSq();
            return aggro
                && !float.IsNaN(differenceToVictim.x)
                && myMan.offHandWeapon != null
                && range > minCastingRangeSq
                && range < maxCastingRangeSq;
        }

        public override void DrawHUD()
        {
            if (!aggro)
                return;

            scene.font.content.SetValues(scene.textShader);
            scene.view.SetValues(
                scene.textShader,
                Matrix.Scaling(0.2f) * scene.view.yBillBoard * Matrix.Translation(myMan.headPos + new Vector3f(0, 0.5f, 0))
                );
            healthText.DrawText(scene.textShader, "HUD");
        }

        protected abstract StickMan GetTarget();

        protected bool GageInterest<Brain>(StickMan candidate, ref float closestDistSq, ref StickMan closest)
            where Brain : StickmanController
        {
            if (candidate.health > 0 && candidate.brain is Brain)
            {
                float d = (candidate.physics.state.position - physics.state.position).LengthSq();
                if (d < closestDistSq)
                {
                    closestDistSq = d;
                    closest = candidate;
                }

                return true;
            }
            else
                return false;
        }
    }
}
