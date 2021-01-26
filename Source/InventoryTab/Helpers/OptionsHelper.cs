
using UnityEngine;

using Verse;

namespace InventoryTab.Helpers
{
    public class OptionsHelper {

        public bool SearchWholeMap => _searchMap;
        public bool SearchPawns => _searchPawns;
        public bool LimitToStorage => _limitToStorage;
        public bool SearchShipChunk { get; } = false;

        public bool AutoUpdate => _autoUpdate;
        public float AutoUpdateTimeInterval { get; private set; } = 5;

        private const float _optionsHeight = 32;
        private const float _indexJumper = 30;
        //Options, all hard coded because thats going to be the simplest
        private bool _searchMap = false;
        private bool _searchPawns = false;
        private bool _limitToStorage = false;
        private bool _autoUpdate = true;
        private readonly MainTabWindow_Inventory _window;

        public OptionsHelper(MainTabWindow_Inventory window) {
            _window = window;
        }

        public void DrawOptions(Rect inRect) {
            var index = 1;
            float optionsX = 200;

            //Draw the option for searching the whole map
            Text.Anchor = TextAnchor.MiddleLeft;
            var rectStockpile = new Rect(0, _indexJumper, 128, _optionsHeight);
            Widgets.Label(rectStockpile, "IT_SearchMap".Translate());
            //This rect is created for the checkbox so when you mouse over it, it tells you what it does
            var checkBoxRect = new Rect(rectStockpile.x + optionsX, rectStockpile.y, 24, 24);
            Widgets.Checkbox(checkBoxRect.x, checkBoxRect.y, ref _searchMap);
            //add a tooltip for the searchMap option
            TooltipHandler.TipRegion(checkBoxRect, new TipSignal("IT_SearchMapToolTip".Translate()));
            index++;

            //Draw the option for searching the pawns
            var rectPawn = new Rect(0, index * _indexJumper, 128, _optionsHeight);
            Widgets.Label(rectPawn, "IT_SearchPawns".Translate());
            //This rect is created for the checkbox so when you mouse over it, it tells you what it does
            checkBoxRect = new Rect(rectPawn.x + optionsX, rectPawn.y, 24, 24);
            Widgets.Checkbox(checkBoxRect.x, checkBoxRect.y, ref _searchPawns);
            //add a tooltip for the searchMap option
            TooltipHandler.TipRegion(checkBoxRect, new TipSignal("IT_SearchPawnsToolTip".Translate()));
            index++;

            //Draw the option for autolimit to selected storage
            var rectstorage = new Rect(0, index * _indexJumper, 128, _optionsHeight);
            Widgets.Label(rectstorage, "IT_LimitToCurrentStorage".Translate());
            //This rect is created for the checkbox so when you mouse over it, it tells you what it does
            checkBoxRect = new Rect(rectstorage.x + optionsX, rectstorage.y, 24, 24);
            Widgets.Checkbox(checkBoxRect.x, checkBoxRect.y, ref _limitToStorage);
            //add a tooltip for the searchMap option
            TooltipHandler.TipRegion(checkBoxRect, new TipSignal("IT_LimitToCurrentStorageToolTip".Translate()));
            index++;

            var rectAutoUpdate = new Rect(0, index * _indexJumper, 128, _optionsHeight);
            Widgets.Label(rectAutoUpdate, "IT_AutoUpdate".Translate());
            checkBoxRect = new Rect(rectAutoUpdate.x + optionsX, rectAutoUpdate.y, 24, 24);
            Widgets.Checkbox(checkBoxRect.x, checkBoxRect.y, ref _autoUpdate);
            TooltipHandler.TipRegion(rectAutoUpdate, new TipSignal("IT_AutoUpdateToolTip".Translate()));
            index++;

            var rectTimeInterval = new Rect(0, index * _indexJumper, 256, _optionsHeight);
            Widgets.Label(rectTimeInterval, string.Format("IT_AutoUpdateTimeInterval".Translate() + ": {0}", AutoUpdateTimeInterval));
            var floatRect = new Rect(rectTimeInterval.x + optionsX, rectTimeInterval.y + 10, 128, _optionsHeight);
            AutoUpdateTimeInterval = Widgets.HorizontalSlider(floatRect, AutoUpdateTimeInterval, 0, 5);
            
            if (Widgets.ButtonInvisible(inRect) == true) {
                _window.Dirty();
            }
        }

    }
}
