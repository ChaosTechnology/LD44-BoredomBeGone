using ChaosFramework.Core;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics.OpenGl.Lights;

namespace LD44
{
    class PentagramInstancer : MaskedSpotLightInstancer
    {
        public PentagramInstancer(Graphics graphics, int expectedInstances, TextureContainer.Entry mask)
            : base(graphics, expectedInstances, mask)
        { }

        protected override ShaderContainer.Entry GetShader(Disposable monitor)
            => AccessScope<Game>.current.shaders.Load($"Shaders/Pentagram.fx", monitor);
    }
}
