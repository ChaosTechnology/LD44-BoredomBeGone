using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Components;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Input;

namespace LD44.Menu
{
    public class Button : Control<Shop>
    {
        public System.Action click, hover;
        public bool enabled = true;

        TextureContainer.Entry hoverTexture;
        bool isHovered = false;
        bool isDown = true; // Down until first update, so menu rebuilds don't trigger repeated presses.

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
            if (scene.game.IsMouseDown(Mouse.ButtonSemantic.Left) && enabled && isHovered)
            {
                if (!isDown)
                {
                    click?.Invoke();
                    isDown = true;
                }
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
