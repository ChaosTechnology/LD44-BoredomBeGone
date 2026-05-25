using ChaosFramework.Math;
using static ChaosFramework.Math.Clamping;

namespace LD44.Menu
{
    public class ButtonBox : Control<Shop>
    {
        const float POSITION_INTERPOLATE = 5f;
        const float BUTTON_MARGIN = 0.05f;

        public float btnHeight = 0.1f;
        int scrollIndex = 0;

        int numFittingButtons => (int)(bounds.height / (btnHeight + BUTTON_MARGIN / 2));

        Button[] _buttons;
        public Button[] buttons
        {
            get { return _buttons; }
            set
            {
                _buttons = value;
                if (_buttons != null)
                    foreach (Button b in _buttons)
                        if (b != null)
                            b.bounds = new Bounds2f(bounds.topLeft, bounds.topRight);
            }
        }

        public void Clear()
        {
            if (buttons != null)
                foreach (Button b in buttons)
                    b.Dispose();

            buttons = null;
            scrollIndex = 0;
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)Shop.UpdateLayers.Input].Add(UpdateScroll);
            scene.updateLayers[(int)Shop.UpdateLayers.Overlay].Add(ScrollButtons);
        }

        void UpdateScroll()
        {
            if (bounds.Contains(scene.mousePosition.xy) && buttons != null)
                scrollIndex = Clamp(0, Max(0, buttons.Length - numFittingButtons), (int)(scrollIndex - scene.mouseDelta.z * 1f));
        }

        void ScrollButtons()
        {
            if (ftime > 0 && buttons != null && buttons.Length > 0)
            {
                // scrolled out top
                for (int i = 0; i < scrollIndex; i++)
                {
                    buttons[i].bounds.low.x += ((bounds.left + BUTTON_MARGIN) - buttons[i].bounds.low.x) * ftime * POSITION_INTERPOLATE;
                    buttons[i].bounds.high.x += ((bounds.right - BUTTON_MARGIN) - buttons[i].bounds.high.x) * ftime * POSITION_INTERPOLATE;
                    buttons[i].bounds.high.y += (bounds.top - buttons[i].bounds.high.y) * ftime * POSITION_INTERPOLATE;
                    buttons[i].bounds.low.y += (bounds.top - buttons[i].bounds.low.y) * ftime * POSITION_INTERPOLATE;
                    buttons[i].enabled = false;
                }

                // visible
                for (int i = scrollIndex; i < Min(buttons.Length, scrollIndex + numFittingButtons); i++)
                {
                    buttons[i].bounds.low.x += ((bounds.left + BUTTON_MARGIN) - buttons[i].bounds.low.x) * ftime * POSITION_INTERPOLATE;
                    buttons[i].bounds.high.x += ((bounds.right - BUTTON_MARGIN) - buttons[i].bounds.high.x) * ftime * POSITION_INTERPOLATE;
                    buttons[i].bounds.high.y += ((bounds.top - (BUTTON_MARGIN / 2 + btnHeight) * (i - scrollIndex) - BUTTON_MARGIN) - buttons[i].bounds.high.y) * ftime * POSITION_INTERPOLATE;
                    buttons[i].bounds.low.y = buttons[i].bounds.top - btnHeight;
                    buttons[i].bounds.low.y = Max(bounds.bottom, buttons[i].bounds.bottom);
                    buttons[i].enabled = true;
                }

                // scrolled out bottom
                for (int i = Min(buttons.Length, scrollIndex + numFittingButtons); i < buttons.Length; i++)
                {
                    buttons[i].bounds.low.x += ((bounds.left + BUTTON_MARGIN) - buttons[i].bounds.low.x) * ftime * POSITION_INTERPOLATE;
                    buttons[i].bounds.high.x += ((bounds.right - BUTTON_MARGIN) - buttons[i].bounds.high.x) * ftime * POSITION_INTERPOLATE;
                    buttons[i].bounds.high.y += (bounds.bottom - buttons[i].bounds.high.y) * ftime * POSITION_INTERPOLATE;
                    buttons[i].bounds.low.y = Max(bounds.bottom, buttons[i].bounds.top - btnHeight);
                    buttons[i].enabled = false;
                }
            }
        }
    }
}
