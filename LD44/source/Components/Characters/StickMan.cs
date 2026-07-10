using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Shapes.Rigging;
using ChaosFramework.Graphics.OpenGl.Model;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Components;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Shapes;
using ChaosFramework.Physics.States;
using ChaosFramework.Shapes.Convex;
using ChaosUtil.Primitives;
using static ChaosFramework.Math.Clamping;
using static ChaosFramework.Math.Constants;
using static ChaosFramework.Math.Exponentials;
using SysCol = System.Collections.Generic;

namespace LD44.Components.Characters
{
    using Weapons;

    public sealed class StickMan : Component<WorldScene>
    {
        public const float MIN_ROT_X = 0;
        public const float MAX_ROT_X = PI_HALF;
        public const float MIN_ROT_Y = 0;
        public const float WEAPON_BASE_INERTIA = 35;
        const float IFRAMES = 0.3f;
        const float MAP_FAILSAFE_DIST = 0.1f;
        const float CAPSULE_RADIUS = 0.25f;
        const float WALKING_SPEED_THRESHOLD = 0.01f;

        static readonly Matrix MAIN_WEAPON_BASE_TRANSFORM = Matrix.RotationY(PI_HALF) * Matrix.RotationX(PI_HALF);

        public float MAX_ROT_Y => PI_HALF - Max(armRotation.x * 0.25f, 0.25f - 0.5f * armRotation.x);

        public Rig.Bone boneSwordArm;
        public Rig.Bone boneSpellArm;
        public Rig.Bone boneHead;
        public Rig.Bone boneTorso;

        public PlayerStats stats;

        public readonly ChaosFramework.Collections.LinkedList<System.Action> onDeath = [];

        public StickmanController brain;

        public Matrix drawTransform { get; private set; } = Matrix.IDENTITY;

        public MaterialContainer.Entry mat;
        public ChaosFramework.Graphics.OpenGl.ChaosShader.Shader shader;
        public MeshContainer.Entry mesh;
        public AnimationContainer.Entry anim;

        public ChaosFramework.Physics.Physical physics;
        private Rig rig;

        public Vector2f armRotation, armRotationSpeed;
        public Weapon mainHandWeapon;
        public OffHand offHandWeapon;

        Vector3f lastMainHandPosition, lastOffHandPosition;
        Vector3f lastSwordTipPosition;
        float mainWrist = 0;
        float mainHandSpeed;

        public float initialHealth { get; private set; } = 100f;
        public float health { get; private set; } = 100f;
        public float lastGrounded;
        public bool onGround => lastGrounded < 0.3f;
        bool doPhysics = false;

        float runningProgress, standingProgress, animInterpolateRunning, animInterpolateWalking, animInterpolateStanding = 1;
        public float originHeight { get; private set; } = 0;

        SysCol.Dictionary<Weapon, Wrapper<float>> invincibility = [];

        public Vector3f headPos => Vector3f.TransformCoordinate(Vector3f.EMPTY, boneHead.restTransform * boneHead.transform * drawTransform);

        public StickMan()
        {
            stats = new PlayerStats(this);
        }

        public void TogglePhysics()
        {
            if (doPhysics = !doPhysics)
            {
                doUpdate = false;
                scene.physics.Remove(physics);
                if (mainHandWeapon != null)
                    scene.physics.Remove(mainHandWeapon.physics);
            }
            else
            {
                doUpdate = true;
                scene.physics.Add(physics);
                if (mainHandWeapon != null)
                    scene.physics.Add(mainHandWeapon.physics);
            }
        }

        protected override void Create(CreateParameters cparams)
        {
            mesh = scene.game.meshes.Load("Models/Chosen One.gmdl", MeshLoadFlags.Animated, this);
            shader = scene.game.graphics.shaders.skinnedNormalMap;
            mat = scene.game.materials.Load("Materials/Characters/Chosen One.mat", this);
            anim = scene.game.animations.Load("Animations/Chosen One.anim", this);

            rig = Rig.FromStreamSource(scene.game.assetSource, "Animations/Chosen One.rig");
            boneSwordArm = rig.root.GetBoneByName("Lower Arm.L");
            boneSpellArm = rig.root.GetBoneByName("Lower Arm.R");
            boneHead = rig.root.GetBoneByName("Head");
            boneTorso = rig.root.GetBoneByName("Body");

            scene.physics.Add(physics = new ChaosFramework.Physics.Physical(this));
            physics.shapes.Clear();
            physics.shapes.Add(new CapsuleShape(
                new Vector3f(0, -0.75f + CAPSULE_RADIUS, 0),
                new Vector3f(0, 1.05f - CAPSULE_RADIUS, 0),
                CAPSULE_RADIUS
                ));
            physics.shapes[0].bounce = 0;
            physics.shapes[0].Update(Matrix.IDENTITY, true);
            originHeight = -physics.Support(new Vector3f(0, -1, 0)).y;

            physics.getRelevantShapes = GetRelevantShapes;
            physics.validateCollision = IsCollisionPartnerValid;
        }

        bool IsCollisionPartnerValid(ChaosFramework.Physics.Physical partner, ChaosFramework.Physics.CollisionData data)
            => partner.creator != this && partner.creator != mainHandWeapon;

        SysCol.IEnumerable<Shape> GetRelevantShapes(ChaosFramework.Physics.Physical collisionPartner)
            => IsCollisionPartnerValid(collisionPartner, null) ? physics.shapes : null;

        public void SetHealth(float health)
            => this.health = initialHealth = health;

        public void DoDamageIncludingInitialHealth(float damage)
        {
            health -= damage;
            initialHealth -= damage;
        }

        public void DoDamage(float damage)
            => health -= damage;

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)WorldScene.UpdateLayers.PrepareUpdate].Add(UpdateWeaponPositions);
            scene.updateLayers[(int)WorldScene.UpdateLayers.Input].Add(MindControl);
            scene.updateLayers[(int)WorldScene.UpdateLayers.Move].Add(Move);
            scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollision].Add(HitStuff);
            scene.updateLayers[(int)WorldScene.UpdateLayers.UpdateRenderTransforms].Add(UpdateTransform);
            brain?.SetUpdateCalls();
        }

        void MindControl()
            => brain?.Input();

        void UpdateWeaponPositions()
        {
            if (mainHandWeapon != null)
            {
                lastMainHandPosition = mainHandWeapon.physics.state.position;
                Vector3f swordTip = Vector3f.TransformCoordinate(new Vector3f(0, 1, 0), mainHandWeapon.physics.state.GetTransform());
                lastSwordTipPosition = swordTip;
            }

            if (offHandWeapon != null)
                lastOffHandPosition = offHandWeapon.position;
        }

        void Move()
        {
            physics.state.Move(ftime);

            armRotation += armRotationSpeed * ftime;
            armRotation.x = Clamp(MIN_ROT_X, MAX_ROT_X, (float)armRotation.x);
            armRotation.y = Clamp(MIN_ROT_Y, MAX_ROT_Y, armRotation.y);

            float horizontalSpeedSq = physics.state.velocity.xz.LengthSq();
            if (horizontalSpeedSq > stats.walkingSpeed * stats.walkingSpeed)
                animInterpolateRunning += (1 - animInterpolateRunning) * ftime * 20;
            else
                animInterpolateRunning -= animInterpolateRunning * ftime * 20;

            if (horizontalSpeedSq > WALKING_SPEED_THRESHOLD && horizontalSpeedSq < stats.walkingSpeed * stats.walkingSpeed)
                animInterpolateWalking += (1 - animInterpolateWalking) * ftime * 20;
            else
                animInterpolateWalking -= animInterpolateWalking * ftime * 20;

            if (horizontalSpeedSq <= WALKING_SPEED_THRESHOLD)
                animInterpolateStanding += (1 - animInterpolateStanding) * ftime * 20;
            else
                animInterpolateStanding -= animInterpolateStanding * ftime * 20;

            runningProgress += ftime * Sqrt(horizontalSpeedSq) * 0.5f;
            runningProgress = runningProgress % 1;
            standingProgress += ftime * 0.25f;
            standingProgress = standingProgress % 1;

            rig.ClearAnimations();
            anim.content["Run Baked"].SetAnimationData(rig, runningProgress, animInterpolateRunning);
            anim.content["Walk Baked"].SetAnimationData(rig, runningProgress, animInterpolateWalking);
            anim.content["Idle Baked"].SetAnimationData(rig, standingProgress, Max(0.1f, animInterpolateStanding - animInterpolateWalking - animInterpolateRunning));

            UpdateWeapons();
            UpdateHands();

            brain?.Move();
        }

        void HitStuff()
        {
            lastGrounded += ftime;
            foreach (ChaosFramework.Physics.CollisionData col in physics.currentCollisions)
            {
                bool swap = col.p1.creator == this;
                foreach (ChaosFramework.Physics.CollisionData.ShapeCollisionData dat in col.data)
                {
                    Vector3f nor = swap ? -dat.penetrationNormal : dat.penetrationNormal;
                    if (nor.y > 0.5f && Vector3f.Dot(swap ? dat.v1 : dat.v2, nor) <= 0)
                    {
                        lastGrounded = 0;
                        goto iWasGrounded;
                    }
                }
            }
            iWasGrounded:;

            foreach (ChaosFramework.Physics.CollisionData col in physics.currentCollisions)
            {
                bool swap = col.p1.creator == this;
                ChaosFramework.Physics.Physical other = swap ? col.p2 : col.p1;
                Weapon weapon = other.creator as Weapon;
                if (weapon == null || invincibility.ContainsKey(weapon))
                    continue;

                if (other != null)
                    foreach (ChaosFramework.Physics.CollisionData.ShapeCollisionData data in col.data)
                    {
                        string colliderName = (swap ? data.s2 : data.s1).name;
                        if (colliderName != null)
                        {
                            Weapon.WeaponAttribute.WeaponPart part;
                            if (weapon.attr.TryGetPart(colliderName, out part))
                            {
                                bool flip = col.p2 == physics;
                                float v1Nor = Vector3f.Dot(flip ? data.v1 : data.v2, data.penetrationNormal);
                                float v2Nor = Vector3f.Dot(flip ? data.v2 : data.v1, -data.penetrationNormal);
                                float m1 = physics.state.mass;
                                float m2 = flip ? col.p2.state.mass : col.p1.state.mass;
                                float deltaV = 0;

                                Vector3f relativeVelocity = (data.v1 - data.v2);
                                deltaV = relativeVelocity.Length() * weapon.attr.weight;
                                float damageFactor = part.damageType == Weapon.WeaponAttribute.WeaponPart.DamageType.Blunt ? 0.0025f : 0.005f;
                                float damageDone = Min(1, System.Math.Abs(deltaV * damageFactor));
                                if (damageDone >= 0.1f)
                                {
                                    invincibility[weapon] = new Wrapper<float>(IFRAMES);
                                    float damageTaken = part.damage * weapon.attr.damage * damageDone * weapon.parent.stats.physicalDamage / stats.physicalResistance;
                                    TakeHit(damageTaken);
                                }
                            }
                        }
                    }
            }

            foreach (SysCol.KeyValuePair<Weapon, Wrapper<float>> hitCooldown in new SysCol.Dictionary<Weapon, Wrapper<float>>(invincibility))
                if ((hitCooldown.Value.value -= ftime) < 0)
                    invincibility.Remove(hitCooldown.Key);

            if (health <= 0)
                Die();

            if (ftime != 0)
            {
                if (mainHandWeapon != null)
                {
                    mainHandWeapon.physics.state.velocity = (mainHandWeapon.physics.state.position - lastMainHandPosition) / ftime;
                    float mainHandWeaponRelativeSpeed = (mainHandWeapon.physics.state.velocity - physics.state.velocity).Length();
                    mainHandSpeed += (Sqrt(mainHandWeaponRelativeSpeed) - mainHandSpeed) * ftime * 3.5f;

                    RealPhysicsState weaponPhysics = (RealPhysicsState)mainHandWeapon.physics.state;
                    Vector3f swordTip = Vector3f.TransformCoordinate(new Vector3f(0, 1, 0), mainHandWeapon.physics.state.GetTransform());
                    Vector3f a = Vector3f.Normalize(swordTip - mainHandWeapon.physics.state.position);
                    Vector3f b = Vector3f.Normalize(lastSwordTipPosition - lastMainHandPosition);
                    weaponPhysics.angularVelocity = -Vector3f.Cross(a, b) * PI_HALF / ftime;
                    weaponPhysics.orientation = Quaternion.IDENTITY;
                    weaponPhysics.squishMatrix = Matrix.IDENTITY;
                }

                if (offHandWeapon != null)
                    offHandWeapon.velocity = (offHandWeapon.position - lastOffHandPosition) / ftime;
            }
            UpdateHands();
        }

        void UpdateTransform()
            => drawTransform = physics.state.GetTransform();

        public void TakeHit(float damageTaken)
        {
            damageTaken = Min(health, damageTaken);
            health -= damageTaken;
            int displayDamage = (int)(0.5f + damageTaken);
            if (displayDamage > 0)
                brain?.CreateDamageDisplay(displayDamage);
        }

        public void UpdateAnimations()
        {
            rig.ClearAnimations();
            anim.content["Run Baked"].SetAnimationData(rig, ftime.totalTime % 1, 1);
            UpdateWeapons();
            UpdateHands();
            physics.state.Move(0);
        }

        void UpdateWeapons()
        {
            if (mainHandWeapon != null)
            {
                boneSwordArm.influences.Clear();
                boneSwordArm.parent.influences.Clear();
                boneSwordArm.parent.influences.Add(new Rig.Influence(Matrix.RotationZ(-PI_HALF) * Matrix.RotationY(armRotation.y) * Matrix.RotationZ(PI_HALF - armRotation.x), 1));
                boneSwordArm.influences.Add(new Rig.Influence(Matrix.RotationX((PI - armRotation.Length()) * 0.2f) * Matrix.RotationZ(-armRotation.x), 1));
            }

            if (offHandWeapon != null)
            {
                float w = offHandWeapon.chargeTimer * 10f;
                Vector2f animState = offHandWeapon.GetTargetCastingPosition();
                anim.content["Cast X Baked"].SetAnimationData(boneSpellArm, animState.x * offHandWeapon.chargeTimer, w);
                anim.content["Cast X Baked"].SetAnimationData(boneSpellArm.parent, animState.x * offHandWeapon.chargeTimer, w);
                anim.content["Cast Y Baked"].SetAnimationData(boneSpellArm, animState.y * offHandWeapon.chargeTimer, w);
                anim.content["Cast Y Baked"].SetAnimationData(boneSpellArm.parent, animState.y * offHandWeapon.chargeTimer, w);
            }
        }

        void UpdateHands()
        {
            rig.ComputeTransforms();
            Matrix physicsTransform = physics.state.GetTransform();
            if (mainHandWeapon != null)
            {
                Matrix orientationTransform = boneSwordArm.restTransform * boneSwordArm.transform * physicsTransform;
                Matrix handTransform = Matrix.Translation(0, boneSwordArm.length, 0) * orientationTransform;
                orientationTransform.row3 = new Vector4f(0, 0, 0, 1);
                mainHandWeapon.physics.state.position = handTransform.row3.xyz;

                mainWrist = Min(1, (mainHandSpeed * 0.5f + armRotation.x * 0.25f));
                mainHandWeapon.physics.state.baseTransform = MAIN_WEAPON_BASE_TRANSFORM * Matrix.RotationX(-PI_HALF * mainWrist) * orientationTransform;
            }

            if (offHandWeapon != null)
            {
                if (brain != null)
                    if (brain.TryingToCast())
                        offHandWeapon.StartCast();
                    else
                        offHandWeapon.EndCast();

                offHandWeapon.position = Vector3f.TransformCoordinate(
                    new Vector3f(0, boneSpellArm.length, 0),
                    boneSpellArm.restTransform * boneSpellArm.transform * physicsTransform
                    );
            }
        }

        public void Die()
        {
            scene.fire.Explode(physics.state.position, 10, new Rgba(1, 0.45f, 0.1f, 1));
            foreach(System.Action celebrant in onDeath)
                celebrant.Invoke();

            brain?.Die();
            Dispose();
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            scene.drawLayers[(int)HudScene.DrawLayers.World].Add(DrawWorld);
            scene.drawLayers[(int)HudScene.DrawLayers.Material].Add(DrawMaterial);
            scene.drawLayers[(int)HudScene.DrawLayers.HUD].Add(DrawBrain);
        }

        void DrawWorld()
            => Draw("World");

        void DrawMaterial()
            => Draw("Material");

        void DrawBrain()
            => brain?.DrawHUD();

        void Draw(string pass)
        {
            rig.SetData(shader, (AnimatedMeshData)mesh.content.data);
            scene.view.SetValues(shader, drawTransform);
            mat.content.SetValues(shader);
            mesh.content.Draw(shader, pass);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            scene.physics.Remove(physics);
        }
    }
}
