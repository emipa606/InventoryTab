using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using RimWorld;
using Verse;
using InventoryTab.Helpers;
using System;
using System.Linq;

namespace InventoryTab
{
    public class MainTabWindow_Inventory : MainTabWindow
    {

        //This is used for convenience sake
        public enum Tabs
        {
            All,
            Foods,
            Manufactured,
            RawResources,
            Items,
            Weapons,
            Apperal,
            Building,
            Chunks,
            Corpses
        }
        //Sets the size of the window
        public override Vector2 RequestedTabSize => new Vector2(600f, 750f);

        //Used to define the height of the slot for the items
        private const float _slotHeight = 32;
        //Hold the position of the scroll
        private Vector2 _scrollPosition;
        //What tab is currently being viewed
        private Tabs _currentTab = Tabs.All;

        //Used for searching items
        private string _searchFor;

        //Chached list of all the corpses found
        private readonly List<Corpse> _corpses = new List<Corpse>();

        private List<Thing> _things;
        private List<Slot> _slots;
        private List<Slot> _total;
        private List<Slot> _found;
        private Dictionary<string, int> _qualityCount;

        private float _timer;

        private bool _dirty = false;

        //A holder for all the options
        private readonly OptionsHelper _options;

        public MainTabWindow_Inventory()
        {
            _options = new OptionsHelper(this);
        }

        public override void PostOpen()
        {
            base.PostOpen();
            //Reset this to zero, because it retains it;s position form the
            //previous time it was opened
            _scrollPosition = Vector2.zero;

            //Set it so it's in the map view, no point seeing the items you have in world view
            //plus the selector might not work right in planet view(untested)
            Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.None;

            UpdateThings();
        }

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);
            _timer -= Time.deltaTime;

            //Cache the font and anchor before changing it, so 
            //later we can set it back to what it was before.
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            //Clear the cached corpses
            _corpses.Clear();

            if (_options.AutoUpdate == true && _timer < 0)
            {
                //Cache all items based on options
                _dirty = true;
                _timer = _options.AutoUpdateTimeInterval;
            }

            if (_dirty == true)
            {
                UpdateThings();
            }

            _found = new List<Slot>();
            _qualityCount = new Dictionary<string, int>();
            if (!string.IsNullOrEmpty(_searchFor))
            {
                _found = GetSearchForList(_total);
                UpdateQualityDictionary(_found);
                if (_found.Count == 0)
                {
                    _total = _found;
                }
            }
            else
            {
                UpdateQualityDictionary(_total);
            }
            //Draw the header; options, search and how many items were found
            DrawHeader(inRect, _total.Count, _found.Count);
            //Draws the tabs
            DrawTabs(inRect);
            //Draw all the items based on tabs and options
            DrawMainRect(inRect, _slots);

            //Reset the font and anchor after we are done drawing all our stuff
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        public override void PostClose()
        {
            if (Find.WindowStack.IsOpen<Dialog_Options>() == true)
            {
                Find.WindowStack.TryRemove(typeof(Dialog_Options));
            }

            base.PostClose();
        }

        public void UpdateThings()
        {
            //Do an inital find of all the items on the map based on the options
            _things = ItemFinderHelper.GetAllMapItems(Find.CurrentMap, _options);
            _slots = SortSlotsWithCategory(CombineThings(_things.ToArray()), _currentTab);
            _dirty = false;
            _total = SortSlotsWithCategory(MakeSlotsFromThings(_things), _currentTab);
        }

        public void UpdateQualityDictionary(List<Slot> slots)
        {
            foreach (var slot in slots)
            {
                if (!slot.ThingInSlot.TryGetQuality(out var quality))
                {
                    continue;
                }
                if (_qualityCount.ContainsKey(quality.GetLabel()))
                {
                    _qualityCount[quality.GetLabel()]++;
                }
                else
                {
                    _qualityCount[quality.GetLabel()] = 1;
                }
            }
        }

        public void Dirty()
        {
            _dirty = true;
        }

        private void DrawHeader(Rect inRect, int itemCount, int filtered = 0)
        {
            //Draw the search bar
            var searchOptions = new Rect(0, 0, 200, 30);
            _searchFor = Widgets.TextArea(searchOptions, _searchFor);

            //Draw a label for all the items found
            var label = new Rect(210, 5, 256, 128);
            Text.Font = GameFont.Small;
            if (filtered > 0 && !string.IsNullOrEmpty(_searchFor))
            {
                Widgets.Label(label, $"{"IT_TotalFound".Translate()}: {filtered}/{itemCount}");
            }
            else
            {
                Widgets.Label(label, $"{"IT_TotalFound".Translate()}: {itemCount}");
            }
            if (_qualityCount.Count > 0)
            {
                var qualityLabel = new Rect(0, 30, 466, 30);
                var qualityString = string.Empty;
                foreach (var qualityType in Enum.GetValues(typeof(QualityCategory)).Cast<QualityCategory>())
                {
                    if (!_qualityCount.ContainsKey(qualityType.GetLabel()))
                    {
                        continue;
                    }
                    if (!string.IsNullOrEmpty(qualityString))
                    {
                        qualityString += " ";
                    }
                    qualityString += $"{GenText.CapitalizeFirst(qualityType.GetLabelShort())}: {_qualityCount[qualityType.GetLabel()]}";
                }
                Widgets.Label(qualityLabel, qualityString);
            }

            var optionsRect = new Rect(inRect.width - 25, 0, 25, 25);
            TooltipHandler.TipRegion(optionsRect, new TipSignal("IT_Options".Translate()));
            if (Widgets.ButtonImage(optionsRect, ContentFinder<Texture2D>.Get("UI/settings", true)) == true)
            {
                if (Find.WindowStack.IsOpen<Dialog_Options>() == true)
                {
                    Find.WindowStack.TryRemove(typeof(Dialog_Options));
                }
                else
                {
                    Find.WindowStack.Add(new Dialog_Options(_options));
                }
            }

            var searchRect = new Rect(optionsRect.x - 35, 0, 25, 25);
            TooltipHandler.TipRegion(searchRect, new TipSignal("IT_Search".Translate()));
            if (Widgets.ButtonImage(searchRect, ContentFinder<Texture2D>.Get("UI/search", true)) == true)
            {
                _dirty = true;
            }

        }

        private void DrawTabs(Rect rect)
        {
            var tabRect = new Rect(rect);
            //Need to give it a minY or they get drawn one pixel tall
            tabRect.yMin += 120f;

            var tabs = new List<TabRecord>();

            //Creating all the tabs, we have to reCreate all these at runtime because they don't update
            var tabRec_All = new TabRecord("IT_TabAll".Translate(), delegate () { TabClick(Tabs.All); }, _currentTab == Tabs.All);

            var tabRec_Foods = new TabRecord("IT_TabFoods".Translate(), delegate () { TabClick(Tabs.Foods); }, _currentTab == Tabs.Foods);
            var tabRec_Manufactured = new TabRecord("IT_TabManufactured".Translate(), delegate () { TabClick(Tabs.Manufactured); }, _currentTab == Tabs.Manufactured);
            var tabRec_RawResources = new TabRecord("IT_TabRawResources".Translate(), delegate () { TabClick(Tabs.RawResources); }, _currentTab == Tabs.RawResources);
            var tabRec_Items = new TabRecord("IT_TabItems".Translate(), delegate () { TabClick(Tabs.Items); }, _currentTab == Tabs.Items);

            var tabRec_Weapon = new TabRecord("IT_TabWeapons".Translate(), delegate () { TabClick(Tabs.Weapons); }, _currentTab == Tabs.Weapons);
            var tabRec_Apperal = new TabRecord("IT_TabApparel".Translate(), delegate () { TabClick(Tabs.Apperal); }, _currentTab == Tabs.Apperal);
            var tabRec_Buildings = new TabRecord("IT_TabBuildings".Translate(), delegate () { TabClick(Tabs.Building); }, _currentTab == Tabs.Building);
            var tabRec_Chunks = new TabRecord("IT_TabChunks".Translate(), delegate () { TabClick(Tabs.Chunks); }, _currentTab == Tabs.Chunks);
            var tabRec_Corpses = new TabRecord("IT_TabCorpses".Translate(), delegate () { TabClick(Tabs.Corpses); }, _currentTab == Tabs.Corpses);

            //Add them to the list
            tabs.Add(tabRec_All);
            tabs.Add(tabRec_Foods);
            tabs.Add(tabRec_Manufactured);
            tabs.Add(tabRec_RawResources);
            tabs.Add(tabRec_Items);

            tabs.Add(tabRec_Weapon);
            tabs.Add(tabRec_Apperal);
            tabs.Add(tabRec_Buildings);
            tabs.Add(tabRec_Chunks);
            tabs.Add(tabRec_Corpses);

            //Draw the tabs, the last argument is how many rows you want
            TabDrawer.DrawTabs(tabRect, tabs, 2);
        }

        private void DrawMainRect(Rect inRect, List<Slot> slots)
        {
            var mainRect = new Rect(inRect.x, inRect.y + 37f + (_slotHeight * 3), inRect.width, inRect.height - 37f);
            //Creats slots for all the items; combines, sorts into catergorys and checks for searches all in one line 
            List<Slot> categorizedSlots = GetSearchForList(slots);
            //Sort based on market value
            categorizedSlots.Sort();

            //This is for the scrolling
            var viewRect = new Rect(0, 0, mainRect.width - 16f, (categorizedSlots.Count * _slotHeight) + 6f + (_slotHeight * 3));
            Widgets.BeginScrollView(mainRect, ref _scrollPosition, viewRect);
            {
                for (var i = 0; i < categorizedSlots.Count; i++)
                {
                    var slotRect = new Rect(0, i * _slotHeight, viewRect.width, _slotHeight);

                    //For every second slot hightlight it to make it a bit easier to see
                    if (i % 2 == 1)
                    {
                        Widgets.DrawLightHighlight(slotRect);
                    }

                    Widgets.DrawHighlightIfMouseover(slotRect);

                    //Draw the slot
                    DrawThingSlot(categorizedSlots[i], slotRect);
                }
            }
            Widgets.EndScrollView();
        }

        private void DrawThingSlot(Slot slot, Rect slotRect)
        {
            Thing thing = slot.ThingInSlot;

            //Draw the image of the thing
            var imageRect = new Rect(0f, slotRect.y, 32f, 32f);
            Widgets.ThingIcon(imageRect, thing);

            Widgets.InfoCardButton(slotRect.x + imageRect.width + 5, slotRect.y, thing.def);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            var labelRect = new Rect(slotRect);
            labelRect.x += imageRect.width + 35;

            //Set the label for the thing, we use custom stacksize so we have to set it here
            var thingLabel = thing.LabelCapNoCount + " (x" + slot.stackSize + ")";
            //If item is a humanlike corpse we want to display their name
            if (slot.Tab == Tabs.Corpses && (thing as Corpse) != null && (thing as Corpse).InnerPawn.def.race.Humanlike == true)
            {
                thingLabel = thing.Label;
            }

            if (Widgets.ButtonInvisible(labelRect) == true)
            {
                //Handles clicking of the slot, this was a bitch to get working correctly
                HandleClick(slot.groupedThings);
            }

            Widgets.Label(labelRect, thingLabel);
        }

        private void TabClick(Tabs tab)
        {
            _currentTab = tab;
            _scrollPosition = Vector2.zero;
            _dirty = true;
        }

        //Disclaimer i hate how i had to handle the corpses in this method
        private void HandleClick(List<Thing> things)
        {

            Find.Selector.ClearSelection();
            //Set this so when we are looping we only jump to one thing
            CameraJumperHelper.alreadyJumpedThisLoop = false;

            for (var i = 0; i < things.Count; i++)
            {
                Corpse corpse;
                Pawn pawn;
                //Checks the thing to find out if its in a pawn inventory
                if ((things[i].ParentHolder as Pawn_EquipmentTracker) != null)
                {
                    pawn = (things[i].ParentHolder as Pawn_EquipmentTracker).pawn;
                    //we need to check if the pawn is dead beacuase we can't selected a dead pawn
                    //we need to select it's corpse otherwise just select the pawn
                    if (CheckForCorpse(pawn, out corpse) == true)
                    {
                        Find.Selector.Select(corpse);
                    }
                    else
                    {
                        Find.Selector.Select(pawn);
                    }
                }
                else if ((things[i].ParentHolder as Pawn_ApparelTracker) != null)
                {
                    pawn = (things[i].ParentHolder as Pawn_ApparelTracker).pawn;
                    if (CheckForCorpse(pawn, out corpse) == true)
                    {
                        Find.Selector.Select(corpse);
                    }
                    else
                    {
                        Find.Selector.Select(pawn);
                    }
                }
                else if ((things[i].ParentHolder as Pawn_InventoryTracker) != null)
                {
                    pawn = (things[i].ParentHolder as Pawn_InventoryTracker).pawn;
                    if (CheckForCorpse(pawn, out corpse) == true)
                    {
                        Find.Selector.Select(corpse);
                    }
                    else
                    {
                        Find.Selector.Select(pawn);
                    }
                }
                else
                {
                    //And if it's not attach to a pawn or a pawn's corpse it's most likly just a thing
                    Find.Selector.Select(things[i]);
                }

                //Try to jump to the thing
                CameraJumperHelper.Jump(things.ToArray());
                //Flag it so we don't jump after this
                CameraJumperHelper.alreadyJumpedThisLoop = true;
            }
        }

        //This is for checking a pawn against a corpse to see if it belongs to it
        private bool CheckForCorpse(Pawn pawn, out Corpse corpse)
        {
            if (pawn.Dead == false)
            {
                corpse = null;
                return false;
            }

            for (var i = 0; i < _corpses.Count; i++)
            {
                if (pawn == _corpses[i].InnerPawn)
                {
                    corpse = _corpses[i];
                    return true;
                }
            }
            corpse = null;
            return false;
        }

        private List<Slot> MakeSlotsFromThings(List<Thing> things)
        {
            var result = new List<Slot>();
            foreach (var thing in things)
            {
                result.Add(new Slot(thing, AssignTab(thing)));
            }
            return result;
        }

        //Combines all the things into easier to manage slots
        private List<Slot> CombineThings(Thing[] things)
        {
            var slotMap = new Dictionary<string, Slot>();

            for (var i = 0; i < things.Length; i++)
            {
                var tId = things[i].LabelNoCount;

                //If a thing is a corpse then we need to added it to the _corpse
                //so later we can check it against pawn
                if (things[i].def.IsWithinCategory(ThingCategoryDefOf.Corpses))
                {
                    //Had to do a check to make sure the corpse is actually a corpse,
                    //Beacause mods like "Thanks for all the Fish" have things that are 
                    //catergorized as corpses but arent actually corpsese...
                    if (things[i] is Corpse cor && cor.InnerPawn.def.race.Humanlike == true)
                    {

                        _corpses.Add(things[i] as Corpse);
                    }
                }

                //If a slot already exists for the thing then add it to it
                if (slotMap.ContainsKey(tId) == true)
                {
                    slotMap[tId].groupedThings.Add(things[i]);
                    slotMap[tId].stackSize += things[i].stackCount;

                    continue;
                }

                if (slotMap.ContainsKey(tId))
                {
                    Log.ErrorOnce("Some how we are attempting to add " + tId + " again...", 5);
                    break;
                }

                //Create a new slot
                var s = new Slot(things[i], AssignTab(things[i]));
                slotMap.Add(tId, s);
            }

            //Create and fill the return value
            var result = new List<Slot>();
            foreach (Slot s in slotMap.Values)
            {
                result.Add(s);
            }

            return result;
        }

        //Sorts slots by catergory
        private List<Slot> SortSlotsWithCategory(List<Slot> slots, Tabs tab)
        {
            if (tab == Tabs.All)
            {
                return slots;
            }

            var result = new List<Slot>();

            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i].Tab == tab)
                {
                    result.Add(slots[i]);
                }
            }

            return result;
        }

        //Get a list of slots based on the _searchOf string
        private List<Slot> GetSearchForList(List<Slot> slots)
        {

            if (string.IsNullOrEmpty(_searchFor))
            {
                return slots;
            }

            var res = new List<Slot>();

            var escapedSearchText = _searchFor;
            if(escapedSearchText.Trim().Contains(" "))
            {
                escapedSearchText = escapedSearchText.Trim();
            }
            escapedSearchText = Regex.Escape(escapedSearchText);
            if(escapedSearchText.Contains(@"\ "))
            {
                escapedSearchText = escapedSearchText.Replace(escapedSearchText.Trim(), escapedSearchText.Trim().Replace(@"\ ", ".*"));
            }
            for (var i = 0; i < slots.Count; i++)
            {
                var searchText = slots[i].ThingInSlot.Label + " " + slots[i].ThingInSlot.LabelNoCount;
                if (Regex.IsMatch(searchText, escapedSearchText, RegexOptions.IgnoreCase))
                {
                    res.Add(slots[i]);
                }
            }
            return res;
        }

        private Tabs AssignTab(Thing thing)
        {

            //For some reason a thing that's been minifide dosen't have a thing categories
            //but as far as i know it;s the only thing that dosen't so just add it to the builing tab
            if (thing.def.thingCategories == null)
            {
                //Log.ErrorOnce("For some reason the thing we are trying to assing a tab to its thingCatergories is null. thing: " + thing.Label, 2);
                return Tabs.Building;
            }

            List<ThingCategoryDef> catDefs = thing.def.thingCategories;
            for (var i = 0; i < catDefs.Count; i++)
            {

                if (thing.def.IsWithinCategory(ThingCategoryDefOf.Foods))
                {
                    return Tabs.Foods;
                }

                if (thing.def.IsWithinCategory(ThingCategoryDefOf.Manufactured))
                {
                    return Tabs.Manufactured;
                }

                if (thing.def.IsWithinCategory(ThingCategoryDefOf.ResourcesRaw))
                {
                    return Tabs.RawResources;
                }

                if (thing.def.IsWithinCategory(ThingCategoryDefOf.Items))
                {
                    return Tabs.Items;
                }

                if (thing.def.IsWithinCategory(ThingCategoryDefOf.Weapons))
                {
                    return Tabs.Weapons;
                }

                if (thing.def.IsWithinCategory(ThingCategoryDefOf.Apparel))
                {
                    return Tabs.Apperal;
                }

                if (thing.def.IsWithinCategory(ThingCategoryDefOf.Buildings))
                {
                    return Tabs.Building;
                }

                if (thing.def.IsWithinCategory(ThingCategoryDefOf.Chunks))
                {
                    return Tabs.Chunks;
                }

                if (thing.def.IsWithinCategory(ThingCategoryDefOf.Corpses))
                {
                    return Tabs.Corpses;
                }

            }

            return Tabs.All;
        }


    }
}
