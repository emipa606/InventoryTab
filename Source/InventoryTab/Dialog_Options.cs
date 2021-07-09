using InventoryTab.Helpers;
using UnityEngine;
using Verse;

namespace InventoryTab
{
    public class Dialog_Options : Window
    {
        private readonly OptionsHelper _options;

        public Dialog_Options(OptionsHelper options)
        {
            _options = options;

            doCloseButton = true;
            doCloseX = true;
        }

        public override Vector2 InitialSize => new Vector2(512f, (float) UI.screenHeight / 2);

        public override void DoWindowContents(Rect inRect)
        {
            var anchor = Text.Anchor;
            var font = Text.Font;
            //Header
            var titleRect = new Rect(0, 0, inRect.width, 25);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Medium;

            Widgets.Label(titleRect, "IT_InventoryOptions".Translate());

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Small;
            //Draw all the options
            var optionsRects = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - 25);
            _options.DrawOptions(optionsRects);

            Text.Anchor = anchor;
            Text.Font = font;
        }
    }
}