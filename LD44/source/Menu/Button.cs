using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Components;
using ChaosFramework.Graphics.Colors;

namespace LD44.Menu
{
    public class Button : Control<Shop>
    {
        public System.Action click, hover;
        public bool enabled = true;

        TextureContainer.Entry hoverTexture;
        bool isHovered = false;
        bool isDown = false;

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            hoverTexture = scene.game.textures.Load("Textures/Menu/Button Hover.png", this);
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)Shop.UpdateLayers.Overlay].Add(Update);
        }

        void Update()
        {
            if (isHovered != (isHovered = bounds.Contains(scene.mousePosition.xy)))
                hover?.Invoke();

            txtCol = isHovered ? new Rgba(0.75f, 0, 0, 1) : Rgba.OPAQUE_WHITE;
            if (enabled && isHovered)
            {
                if (!isDown && scene.game.mouse.LeftButton == OpenTK.Input.ButtonState.Pressed)
                    isDown = true;
                else if (isDown && scene.game.mouse.LeftButton == OpenTK.Input.ButtonState.Released)
                {
                    click?.Invoke();
                    isDown = false;
                }
                else if (scene.game.mouse.LeftButton == OpenTK.Input.ButtonState.Released)
                    isDown = false;
            }
            else
                isDown = false;
        }

        public override void DrawControl()
        {
            if (isHovered)
            {
                TextureContainer.Entry tmpTex = texture;
                texture = hoverTexture;
                base.DrawControl();
                texture = tmpTex;
            }
            else
                base.DrawControl();
        }
    }
}
