using ChaosFramework.Graphics;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Graphics.Text;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Platform;
using static ChaosFramework.Math.Clamping;
using static ChaosFramework.Math.Constants;
using static ChaosFramework.Math.Exponentials;

namespace LD44.Components.Characters.Brains
{
    public class ChosenOne : StickmanController
    {
        const float MOUSE_SENS_X = 0.1f;
        const float MOUSE_SENS_Y = 0.05f;
        const float MOUSE_SENS_Z = 15f;
        const float CAM_RAD = 0.15f;
        const float MAX_ZOOM = 11.0f / 3.0f;
        const float MIN_ZOOM = 4.0f / 3.0f;
        const float EGO_ZOOM = 0;

        public static ChosenOne GimmeMyChosenOne(ChaosFramework.Physics.Physical p)
            => (p.creator as StickMan)?.brain as ChosenOne;

        public static ChosenOne GimmeMyChosenOne(ChaosFramework.Physics.CollisionData data)
            => GimmeMyChosenOne(data.p1) ?? GimmeMyChosenOne(data.p2);

        Game.LD44Keyboard keyboard => myMan.scene.game.keyboard;
        Game.LD44Mouse mouse => myMan.scene.game.mouse;

        float oldMouseX, oldMouseY, oldMouseZ;
        float verticalView = 0;
        float camDist = 3;
        float drawnHealth;
        float cameraDistance = 5;

#if DEBUG
        bool lockHealthCheat = false;
#endif

        public bool interacting = false;
        public bool triesToInteract = false;
        public bool lockF = false;
        public bool autorun = false;

        Vector3f targetCameraPosition;
        Vector3f interpolatedCamDir;
        Menu.Control<WorldScene> healthBar;

        public override void TakeControl()
        {
            healthBar = myMan.AddComponent<Menu.Control<WorldScene>>();
            healthBar.bounds = new Bounds2f(myMan.scene.game.window.Ratio() - 0.3f, 0.9f, myMan.scene.game.window.Ratio(), 1);
            healthBar.texture = null;
            healthBar.text = new Menu.Text[] { new Menu.Text(myMan.scene, (drawnHealth = myMan.health).ToString(), Align.Right) };
        }

        public override void DropControl()
            => healthBar.Dispose();

        public override void Input()
        {
            if (ftime > 0 && mouse.leftButton && myMan.mainHandWeapon != null)
            {
                float dX = mouse.X - oldMouseX;
                float dY = mouse.Y - oldMouseY;

                myMan.armRotationSpeed += (new Vector2f(-dX * 0.005f / ftime, dY * 0.005f / ftime) - myMan.armRotationSpeed)
                                          * ftime
                                          * StickMan.WEAPON_BASE_INERTIA / myMan.mainHandWeapon.attr.weight
                                          * myMan.stats.swingStrength;
            }
        }

        public override void Move()
        {
            if (!interacting)
            {
                float dX = mouse.X - oldMouseX;
                float dY = mouse.Y - oldMouseY;
                float dZ = mouse.WheelPrecise - oldMouseZ;

                if (!mouse.leftButton)
                {
                    physics.state.baseTransform *= Matrix.RotationY(dX * MOUSE_SENS_X * ftime);
                    verticalView += dY * ftime * MOUSE_SENS_Y;
                }
                verticalView = Clamp(-PI_HALF, PI_HALF / 12, verticalView);

                float zoomDelta = dZ * MOUSE_SENS_Z * ftime;
                if (camDist <= EGO_ZOOM && zoomDelta < 0)
                    camDist = MIN_ZOOM;
                else if (camDist >= MIN_ZOOM && camDist - zoomDelta <= MIN_ZOOM)
                    camDist = EGO_ZOOM;
                else
                    camDist = Clamp(MIN_ZOOM, MAX_ZOOM, camDist - zoomDelta);

                oldMouseZ = mouse.WheelPrecise;

                Matrix m = physics.state.GetTransform();
                Vector3f localX = Vector3f.Normalize(m.row0.x0z);
                Vector3f localZ = Vector3f.Normalize(m.row2.x0z);

                if (keyboard.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.T))
                    autorun = true;

                Vector3f move = Vector3f.EMPTY;
                if (myMan.onGround)
                {
                    if (keyboard.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.A)) { move -= localX; autorun = false; }
                    if (keyboard.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D)) { move += localX; autorun = false; }
                    if (keyboard.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.S)) { move -= localZ; autorun = false; }
                    if (autorun || keyboard.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.W)) move += localZ;
                    if (!lockF && keyboard.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.F))
                        lockF = triesToInteract = true;
                    else if (!keyboard.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.F))
                        lockF = triesToInteract = false;
                }

                move.Normalize();
                move *= myMan.stats.walkingSpeed;
                if (scene.game.keyboard.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftShift))
                {
                    move.x *= myMan.stats.speedFactor;
                    if (move.z > 0)
                        move.z *= myMan.stats.speedFactor;
                    else
                        move.z *= myMan.stats.speedFactor / 2;
                    if (myMan.onGround)
                        myMan.DoDamage(myMan.stats.lifeDrainStamina * ftime);
                }

                Vector3f horizontalVelocity = move - physics.state.velocity;
                horizontalVelocity.y = 0;
                if (myMan.onGround)
                    physics.state.velocity += horizontalVelocity * ftime * 15;

                Vector3f headPos = physics.state.position;
                if (myMan.onGround && scene.game.keyboard.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space))
                {
                    myMan.DoDamage(myMan.stats.lifeDrainStamina);
                    physics.state.velocity.y = myMan.stats.jumpLaunch;
                    myMan.lastGrounded = float.MaxValue;
                }
            }
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)WorldScene.UpdateLayers.UpdateCamera].Add(UpdateCamera);
        }

        void UpdateCamera()
        {
            if (!interacting)
            {
                Vector3f targetCamDirection;
                float targetCameraDistance;

                Matrix m = physics.state.GetTransform();
                Vector3f localX = Vector3f.Normalize(m.row0.x0z);
                Vector3f localZ = Vector3f.Normalize(m.row2.x0z);
                Vector3f viewDirection = Vector3f.Normalize(Vector3f.TransformCoordinate(localZ, Matrix.RotationAxis(localX, verticalView)));
                if (camDist <= EGO_ZOOM)
                {
                    targetCameraPosition = myMan.headPos + localZ * 0.325f + new Vector3f(0, 0.125f, 0);
                    targetCamDirection = viewDirection;
                    targetCameraDistance = 1234;
                }
                else
                {
                    Vector3f camUp = Vector3f.Cross(viewDirection, localX);
                    Vector3f camPos = myMan.headPos - viewDirection * camDist + camUp * camDist * camDist + localZ * camDist / 4;
                    float h = scene.map.GetHeightAt(camPos.x, camPos.z);
                    camPos.y = Max(h + 1f, camPos.y, physics.state.position.y - 0.666f);
                    targetCamDirection = myMan.headPos - new Vector3f(0, 0.25f, 0) - camPos;
                    targetCameraPosition += (camPos - targetCameraPosition) * ftime * 10f;
                    targetCameraDistance = 5;
                }
                cameraDistance += (targetCameraDistance - cameraDistance) * EaseIn(ftime * 10);

#if DEBUG
                if (keyboard.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.H))
                {
                    if (!lockHealthCheat)
                    {
                        lockHealthCheat = true;
                        myMan.DoDamage(-666666f);
                    }
                }
                else
                    lockHealthCheat = false;
#endif

                interpolatedCamDir += (targetCamDirection - interpolatedCamDir) * EaseIn(ftime * 10f);
                scene.view.Update(
                    scene.view.Position + (targetCameraPosition - scene.view.Position) * EaseIn(ftime * cameraDistance),
                    scene.view.Direction + (interpolatedCamDir - scene.view.Direction) * EaseIn(ftime * 10),
                    new Vector3f(0, 1, 0));

                oldMouseX = mouse.X;
                oldMouseY = mouse.Y;
            }
        }

        public override void DrawHUD()
        {
            float diff = myMan.health - drawnHealth;
            drawnHealth += (diff + System.Math.Sign(diff) * 5) * myMan.ftime;
            if (diff < 0) drawnHealth = Max(myMan.health, drawnHealth);
            if (diff > 0) drawnHealth = Min(myMan.health, drawnHealth);
            healthBar.text[0].text = ((int)drawnHealth).ToString();
            healthBar.bounds = new Bounds2f(myMan.scene.game.window.Ratio() - 0.3f, 0.9f, myMan.scene.game.window.Ratio(), 1);
        }

        public override bool TryingToCast()
            => mouse.rightButton;

        public override void CreateDamageDisplay(float displayDamage)
        {
            var textDisplay = scene.AddComponent<HudDamageDisplay>();
            textDisplay.text = "-" + displayDamage;
            textDisplay.velocity = new Vector3f(0, -0.05f, 0);
            textDisplay.antiGravity = new Vector3f(0, -0.1f, 0);
            textDisplay.color = new Rgba(1, 0, 0, 1);
            textDisplay.position = new Vector3f(scene.hudView.screenRatio, 0.95f, 0);
        }
    }
}
