using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Components;
using static ChaosFramework.Math.Clamping;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Graphics.OpenGl.ChaosShader;
using ChaosFramework.Math;
using ChaosFramework.Collections;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Platform;
using ChaosUtil.Reflection;
using SysCol = System.Collections.Generic;
using Rex = System.Text.RegularExpressions;

namespace LD44.Menu
{
    using ChaosFramework.Input;
    using Components.Weapons;
    using WeaponAttribute = Components.Weapons.Weapon.WeaponAttribute;

    public class Shop : HudScene
    {
        public enum UpdateLayers
        {
            UpdateCursor,
            PrepareUpdate,
            Input,
            Particles,
            Overlay
        }

        const float X_BUTTON_SIZE = 0.1f;
        const float CURSOR_SIZE = 0.075f;
        const float MOUSE_SENSITIVITY = 0.0025f;

        static readonly Rex.Regex LINE_BREAK_REGEX = new Rex.Regex(@"\r?\n", Rex.RegexOptions.Compiled);

        public Shader cursorShader;
        TextureContainer.Entry cursorTex;

        public Vector3f mousePosition, mouseDelta;
        Vector3f oldMouseData;

        Components.Characters.Brains.ChosenOne player;
        Button btnClose, btnSkill, btnWeapon, btnSpell;
        Control<Shop> lblDescription;
        System.Action onClose;
        byte menuMode;

        public Shop(Game game)
            : base(game, typeof(UpdateLayers), typeof(DrawLayers))
        {
            cursorShader = base.game.graphics.shaders.spriteEffect;
            cursorTex = base.game.textures.Load("Textures/Menu/Cursor.png", this);
            BuildMenu();
            base.game.graphics.windowsChanged += UpdateRatio;
        }

        void UpdateRatio()
        {
            BuildMenu();
            switch (menuMode)
            {
                case 0: btnWeapon.click(); break;
                case 1: btnSpell.click(); break;
                case 2: btnSkill.click(); break;
            }
        }

        void BuildMenu()
        {
            const float TARGET_RATIO = 16 / 9f;
            DisposeChildren();

            Bounds2f bounds;
            float ratioChange = game.window.Ratio() / TARGET_RATIO;
            if (game.window.Ratio() > TARGET_RATIO)
                bounds = new Bounds2f(-TARGET_RATIO, -1, TARGET_RATIO, 1);
            else
                bounds = new Bounds2f(-game.window.Ratio(), -ratioChange, game.window.Ratio(), ratioChange);

            btnClose = AddComponent<Button>();
            btnClose.bounds = new Bounds2f(
                bounds.left + 0.05f * bounds.height, bounds.top - X_BUTTON_SIZE * 0.75f * bounds.height,
                bounds.left + X_BUTTON_SIZE * 2f * bounds.height, bounds.top - X_BUTTON_SIZE * 0.25f
                );
            btnClose.text = new[] { new Text(this, "Close") };
            btnClose.click = Close;

            ButtonBox buttons = AddComponent<ButtonBox>();
            buttons.bounds = new Bounds2f(
                0.05f * bounds.height, bounds.bottom + 0.05f * bounds.height,
                bounds.right - 0.05f * bounds.height, bounds.top - 0.125f * bounds.height
                );
            buttons.btnHeight = 0.05f * bounds.height;

            (btnWeapon = AddComponent<Button>()).bounds = new Bounds2f(
                buttons.bounds.left,
                buttons.bounds.top,
                buttons.bounds.left + buttons.bounds.width / 3,
                buttons.bounds.top + 0.05f * bounds.height
                );
            (btnSpell = AddComponent<Button>()).bounds = new Bounds2f(
                buttons.bounds.left + buttons.bounds.width / 3,
                buttons.bounds.top,
                buttons.bounds.left + 2 * buttons.bounds.width / 3,
                buttons.bounds.top + 0.05f * bounds.height
                );
            (btnSkill = AddComponent<Button>()).bounds = new Bounds2f(
                buttons.bounds.left + 2 * buttons.bounds.width / 3,
                buttons.bounds.top,
                buttons.bounds.left + buttons.bounds.width,
                buttons.bounds.top + 0.05f * bounds.height
                );
            (lblDescription = AddComponent<Control<Shop>>()).bounds = new Bounds2f(
                bounds.left + 0.05f * bounds.height,
                bounds.bottom + 0.05f * bounds.height,
                buttons.bounds.left - 0.05f * bounds.height,
                btnSkill.bounds.top
                );
            lblDescription.text = new[] { new Text(this, "", Align.TopLeft) };
            lblDescription.textSz = 0.025f * bounds.height;
            lblDescription.margin = new Vector2f(0.025f * bounds.height, -0.05f * bounds.height);

            btnSkill.text = new[] { new Text(this, "Skills") };
            btnWeapon.text = new[] { new Text(this, "Weapons") };
            btnSpell.text = new[] { new Text(this, "Spells") };

            Button focused = null;
            btnSkill.click = () =>
            {
                btnWeapon.enabled = true;
                btnSpell.enabled = true;
                btnSkill.enabled = false;

                menuMode = 2;
                lblDescription.text[0].text = "";
                buttons.Clear();
                buttons.buttons = new Button[Components.Characters.PlayerStats.skills.length];
                for (int _it = 0; _it < buttons.buttons.Length; _it++)
                {
                    int i = _it;
                    string skillName = Components.Characters.PlayerStats.skills[i];
                    float val = player.myMan.stats.GetSkill(skillName);
                    float price = player.myMan.stats.GetPrice(val);
                    float max = player.myMan.stats.GetSkillMaximum(skillName);
                    if (val >= max)
                        price = float.MaxValue;

                    buttons.buttons[i] = buttons.AddComponent<Button>();
                    buttons.buttons[i].bounds = new Bounds2f(buttons.bounds.left, buttons.bounds.top, buttons.bounds.right, buttons.bounds.top);
                    buttons.buttons[i].text = new[] {
                        new Text(this, skillName, Align.Left),
                        new Text(this, ((int)(val * 10)).ToString()),
                        new Text(this, price == float.MaxValue ? "---" : price.ToString(), Align.Right)
                    };

                    if (price >= player.myMan.health)
                        buttons.buttons[i].text[2].color = new Rgba(0.75f, 0, 0, 1);

                    buttons.buttons[i].click = () =>
                    {
                        if (price == float.MaxValue)
                            return;

                        if (player.myMan.health > price)
                        {
                            player.myMan.DoDamage(price);
                            player.myMan.stats.Level(skillName);
                            val = player.myMan.stats.GetSkill(skillName);
                            price = player.myMan.stats.GetPrice(val);
                            buttons.buttons[i].text[1].text = ((int)(val * 10)).ToString();
                            buttons.buttons[i].text[2].color = price >= player.myMan.health ? new Rgba(0.75f, 0, 0, 1) : Rgba.OPAQUE_WHITE;
                        }

                        Bounds2f[] tmpBounds = new Bounds2f[buttons.buttons.Length];
                        for (int s = 0; s < tmpBounds.Length; s++)
                            tmpBounds[s] = buttons.buttons[s].bounds;
                        btnSkill.click();
                        for (int s = 0; s < tmpBounds.Length; s++)
                            buttons.buttons[s].bounds = tmpBounds[s];
                    };

                    buttons.buttons[i].hover = () =>
                    {
                        if (focused == buttons.buttons[i])
                            return;

                        lblDescription.text[0].text = FitToDescriptionLabel(Components.Characters.PlayerStats.GetDescription(skillName));
                    };
                }
            };

            btnWeapon.click = () =>
            {
                btnWeapon.enabled = false;
                btnSpell.enabled = true;
                btnSkill.enabled = true;

                menuMode = 0;
                lblDescription.text[0].text = "";
                buttons.Clear();
                buttons.buttons = new Button[WeaponAttribute.names.length];

                int _it = -1;
                foreach (SysCol.KeyValuePair<string, WeaponAttribute> weapon in WeaponAttribute.Enumerate())
                {
                    int i = ++_it;
                    buttons.buttons[i] = buttons.AddComponent<Button>();
                    buttons.buttons[i].bounds = new Bounds2f(buttons.bounds.left, buttons.bounds.top, buttons.bounds.right, buttons.bounds.top);
                    bool canAfford = player.myMan.health > weapon.Value.price;
                    bool owns = player.myMan.stats.accessibleWeapons.Contains(weapon.Key);
                    buttons.buttons[i].text = new[] {
                        new Text(this, weapon.Value.displayName, Align.Left),
                        new Text(this, owns
                            ? ((player.myMan.mainHandWeapon == null || player.myMan.mainHandWeapon.attr.displayName != weapon.Key) ? "equip" : "unequip")
                            : weapon.Value.price.ToString(), Align.Right
                            )
                        };

                    if (!owns && !canAfford)
                        buttons.buttons[i].text[1].color = new Rgba(0.75f, 0, 0, 1);

                    buttons.buttons[i].click = () =>
                    {
                        switch (buttons.buttons[i].text[1].text)
                        {
                            case "unequip":
                                player.myMan.mainHandWeapon?.Dispose();
                                player.myMan.mainHandWeapon = null;
                                break;

                            case "equip":
                                player.myMan.mainHandWeapon?.Dispose();
                                player.myMan.mainHandWeapon = player.myMan.AddComponent<Components.Weapons.Weapon>(
                                    CreateParameters.Create(weapon.Key)
                                    );
                                break;

                            default:
                                if (canAfford)
                                {
                                    player.myMan.DoDamage(weapon.Value.price);
                                    player.myMan.mainHandWeapon?.Dispose();
                                    player.myMan.mainHandWeapon = player.myMan.AddComponent<Components.Weapons.Weapon>(
                                        CreateParameters.Create(weapon.Key)
                                        );
                                    player.myMan.stats.accessibleWeapons.Add(weapon.Key);
                                }
                                break;
                        }

                        Bounds2f[] tmpBounds = new Bounds2f[buttons.buttons.Length];
                        for (int w = 0; w < tmpBounds.Length; w++)
                            tmpBounds[w] = buttons.buttons[w].bounds;
                        btnWeapon.click();
                        for (int w = 0; w < tmpBounds.Length; w++)
                            buttons.buttons[w].bounds = tmpBounds[w];
                    };

                    buttons.buttons[i].hover = () =>
                    {
                        if (focused == buttons.buttons[i])
                            return;

                        lblDescription.text[0].text = FitToDescriptionLabel(weapon.Value.shopText);
                    };
                }
            };

            btnSpell.click = () =>
            {
                btnWeapon.enabled = true;
                btnSpell.enabled = false;
                btnSkill.enabled = true;

                menuMode = 1;
                lblDescription.text[0].text = "";
                buttons.Clear();

                buttons.buttons = new Button[SpellAttribute.spells.length];
                for (int _it = 0; _it < SpellAttribute.spells.length; _it++)
                {
                    int i = _it;
                    float price = SpellAttribute.spells[i].attribute.price;
                    buttons.buttons[i] = buttons.AddComponent<Button>();
                    buttons.buttons[i].bounds = new Bounds2f(buttons.bounds.left, buttons.bounds.top, buttons.bounds.right, buttons.bounds.top);
                    bool canAfford = player.myMan.health > price;
                    bool owns = player.myMan.stats.knownSpells.Contains(SpellAttribute.spells[i].type);
                    buttons.buttons[i].text = new[] {
                        new Text(this, SpellAttribute.spells[i].attribute.displayName, Align.Left),
                        new Text(this,
                            owns ? (player.myMan.offHandWeapon == null || player.myMan.offHandWeapon.GetType() != SpellAttribute.spells[i].type ? "equip" : "unequip")
                                 : price.ToString(),
                            Align.Right
                            )};
                    if (!owns && !canAfford)
                        buttons.buttons[i].text[1].color = new Rgba(0.75f, 0, 0, 1);

                    buttons.buttons[i].click = () =>
                    {
                        switch (buttons.buttons[i].text[1].text)
                        {
                            case "unequip":
                                player.myMan.offHandWeapon?.Dispose();
                                player.myMan.offHandWeapon = null;
                                break;

                            case "equip":
                                player.myMan.offHandWeapon?.Dispose();
                                player.myMan.offHandWeapon = (Components.Weapons.OffHand)player.myMan.AddComponent(SpellAttribute.spells[i].type);
                                break;

                            default:
                                if (canAfford)
                                {
                                    player.myMan.DoDamage(price);
                                    player.myMan.offHandWeapon?.Dispose();
                                    player.myMan.offHandWeapon = (Components.Weapons.OffHand)player.myMan.AddComponent(SpellAttribute.spells[i].type);
                                    player.myMan.stats.knownSpells.Add(SpellAttribute.spells[i].type);
                                }
                                break;
                        }

                        Bounds2f[] rects = new Bounds2f[buttons.buttons.Length];
                        for (int s = 0; s < rects.Length; s++)
                            rects[s] = buttons.buttons[s].bounds;
                        btnSpell.click();
                        for (int s = 0; s < rects.Length; s++)
                            buttons.buttons[s].bounds = rects[s];
                    };

                    buttons.buttons[i].hover = () =>
                    {
                        if (focused == buttons.buttons[i])
                            return;

                        lblDescription.text[0].text = FitToDescriptionLabel(SpellAttribute.spells[i].attribute.shopText);
                    };
                }
            };
        }

        string FitToDescriptionLabel(string text)
        {
            float width = (lblDescription.bounds.width - lblDescription.margin.x * 2) / lblDescription.textSz;
            System.Text.StringBuilder bldr = new System.Text.StringBuilder();
            foreach (string line in LINE_BREAK_REGEX.Split(text))
                bldr.AppendLine(font.content.FitText(line, width));

            return bldr.ToString();
        }

        public void ShowDialog(Scene world, Components.Characters.Brains.ChosenOne toBeSkilled, System.Action callback)
        {
            world.doUpdate = false;
            doUpdate = doDraw = true;
            player = toBeSkilled;
            btnWeapon.click();
            onClose = () =>
            {
                player = null;
                doUpdate = doDraw = false;
                world.doUpdate = true;
                world.game.scenes.Remove(this);
                callback?.Invoke();
            };
            world.game.scenes.Add(this);
        }

        void Close()
            => onClose?.Invoke();

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            updateLayers[(int)UpdateLayers.UpdateCursor].Add(UpdateCursor);
        }

        void UpdateCursor()
        {
            Vector3f newMouseData = new Vector3f(game.MouseX(), game.MouseY(), game.Scroll() );
            mouseDelta = new Vector3f(MOUSE_SENSITIVITY * (newMouseData.x - oldMouseData.x), -MOUSE_SENSITIVITY * (newMouseData.y - oldMouseData.y), newMouseData.z - oldMouseData.z);
            mousePosition += mouseDelta;
            mousePosition.x = Clamp(-game.window.Ratio(), game.window.Ratio(), mousePosition.x);
            mousePosition.y = Clamp(-1, 1, mousePosition.y);
            oldMouseData = newMouseData;
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            drawLayers[(int)DrawLayers.Cursor].Add(DrawCursor);
        }

        void DrawCursor()
        {
            using (game.graphics.stateTracker.Start())
            {
                game.graphics.stateTracker.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.DepthTest, false);
                hudView.SetValues(cursorShader, Matrix.Scaling(CURSOR_SIZE, CURSOR_SIZE, 1) * Matrix.Translation(mousePosition.x, mousePosition.y, 0));
                cursorShader.SetValue("tex", cursorTex);
                cursorShader.BeginPass("Sprite");
                Sprite.DrawPositionOnly(game.graphics);
                cursorShader.EndPass();
            }
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            game.graphics.windowsChanged -= UpdateRatio;
        }
    }
}
