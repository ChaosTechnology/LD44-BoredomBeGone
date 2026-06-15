using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.Text;
using ChaosFramework.Graphics.OpenGl.Text;
using ChaosFramework.Math;
using ChaosFramework.Platform;
using static ChaosFramework.Math.Clamping;
using ChaosFramework.Input;

namespace LD44.Components.Interaction.Actions
{
    public class Dialog : NPCInteraction
    {
        public struct Line
        {
            public readonly string speakerName, line;
            public readonly TextMesh speakerGeo, lineGeo;

            public Line(HudScene scene, string speakerName, string line)
            {
                this.speakerName = speakerName;
                this.line = line;

                speakerGeo = null;
                lineGeo = null;
                scene.font.content.UpdateText(ref speakerGeo, $"{speakerName}:", LayoutInfo.TOP_LEFT);
                scene.font.content.UpdateText(ref lineGeo, line, LayoutInfo.TOP_LEFT);
            }

            public override string ToString()
                => $"{speakerName}: {line}";
        }

        const float TEXT_SZ = .075f;
        const float SOME_MARGIN = .05f;
        const float SOME_CONSTANT = 1;

        bool lockF = false;
        float widestName;
        float[] heights;
        Line[] lines;

        int currentText = 0;

        readonly WorldScene _scene;
        protected override WorldScene scene => _scene;

        public Line[] text
        {
            get { return lines; }
            set
            {
                if (value.Length == 0)
                {
                    widestName = 0;
                    heights = new float[0];
                    lines = new Line[0];
                }
                else
                {
                    widestName = 0;
                    for (int i = 0; i < value.Length; i++)
                        widestName = Max(widestName, value[i].speakerGeo.geo.geometryBounds.width);

                    widestName += SOME_CONSTANT;
                    float lineWidth = (scene.game.window.Ratio() * 2 - 2 * SOME_MARGIN) / TEXT_SZ - widestName;
                    for (int i = 0; i < value.Length; i++)
                        value[i] = new Line(scene, value[i].speakerName, scene.font.content.FitText(value[i].line, lineWidth));

                    lines = value;
                    heights = new float[value.Length];
                    for (int i = 0; i < value.Length; i++)
                        heights[i] = Max(value[i].speakerGeo.geo.geometryBounds.height, value[i].lineGeo.geo.geometryBounds.height) * TEXT_SZ;
                }
            }
        }

        public Dialog(WorldScene _scene, Line[] text)
            : base()
        {
            this._scene = _scene;
            this.text = text;
            _scene.game.graphics.windowsChanged += ResizeText;
        }

        void ResizeText()
            => text = text;

        protected override void StartInteraction()
        {
            base.StartInteraction();
            currentText = 0;
        }

        public override void SetUpdateCalls()
            => interactPoint.scene.updateLayers[(int)WorldScene.UpdateLayers.Input].Add(Interact);

        void Interact()
        {
            if (scene.game.IsKeyDown(Keyboard.HidUsage.F))
            {
                if (!lockF)
                    currentText++;

                lockF = true;
            }
            else
                lockF = false;

            if (currentText >= text.Length)
                interactPoint.Conclude();
        }

        public override void SetDrawCalls()
            => interactPoint.scene.drawLayers[(int)HudScene.DrawLayers.HUD].Add(DrawText);

        void DrawText()
        {
            if (currentText < text.Length)
            {
                interactPoint.scene.font.content.SetValues(interactPoint.scene.textShader);

                interactPoint.scene.hudView.SetValues(interactPoint.scene.textShader,
                    Matrix.Scaling(TEXT_SZ) *
                    Matrix.Translation(
                        -scene.game.window.Ratio() + SOME_MARGIN,
                        -1 + SOME_MARGIN + heights[currentText],
                        0));
                text[currentText].speakerGeo?.DrawText(interactPoint.scene.textShader, "HUD");

                interactPoint.scene.hudView.SetValues(interactPoint.scene.textShader,
                    Matrix.Translation(widestName, 0, 0) *
                    Matrix.Scaling(TEXT_SZ) *
                    Matrix.Translation(
                        -scene.game.window.Ratio() + 0.05f,
                        -1 + SOME_MARGIN + heights[currentText],
                        0));
                text[currentText].lineGeo?.DrawText(interactPoint.scene.textShader, "HUD");
            }
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            _scene.game.graphics.windowsChanged -= ResizeText;
        }
    }
}
