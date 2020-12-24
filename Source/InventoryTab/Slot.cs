using System;
using System.Collections.Generic;

using RimWorld;
using Verse;

namespace InventoryTab
{
    public class Slot : IComparable<Slot>
    {
        //This is the thing that is in this slot
        public Thing ThingInSlot { get; private set; }
        public MainTabWindow_Inventory.Tabs Tab { get; private set; }

        //This is all of the thing stack that was found
        public List<Thing> groupedThings;
        public int stackSize;

        public Slot(Thing thing, MainTabWindow_Inventory.Tabs tab)
        {
            ThingInSlot = thing;
            Tab = tab;

            groupedThings = new List<Thing>
            {
                thing
            };
            stackSize = thing.stackCount;
        }

        //Used for when List<T>.Sort is called
        //1 means the object is greater then what it's being compared to
        //-1 means the object is less then what it's being compared to
        // 0 means they are equal
        public int CompareTo(Slot other)
        {
            var firstThingLabel = ThingInSlot.LabelNoCount;
            if (ThingInSlot.TryGetQuality(out _))
            {
                firstThingLabel = RemoveQualityInLabel(firstThingLabel);
            }
            var otherThingLabel = other.ThingInSlot.LabelNoCount;
            if (ThingInSlot.TryGetQuality(out _))
            {
                otherThingLabel = RemoveQualityInLabel(other.ThingInSlot.LabelNoCount);
            }
            var nameCompared = string.Compare(firstThingLabel, otherThingLabel, StringComparison.CurrentCulture);
            if (ThingInSlot.def.IsWithinCategory(ThingCategoryDefOf.Corpses) == true && other.ThingInSlot.def.IsWithinCategory(ThingCategoryDefOf.Corpses) == true)
            {
                if (ThingInSlot is Corpse a && other.ThingInSlot is Corpse b && a.InnerPawn.def.race.Humanlike == true && b.InnerPawn.def.race.Humanlike == true)
                {
                    nameCompared = string.Compare(a.InnerPawn.Label, b.InnerPawn.Label, StringComparison.CurrentCulture);
                }
            }
            if (nameCompared != 0)
            {
                return nameCompared;
            }

            if (ThingInSlot.MarketValue > other.ThingInSlot.MarketValue)
            {
                return 1;
            }
            if (ThingInSlot.MarketValue < other.ThingInSlot.MarketValue)
            {
                return -1;
            }
            return 0;
        }

        private string RemoveQualityInLabel(string thingLabel)
        { 
            return thingLabel.Split('(')[0].Trim();
        }
    }
}
