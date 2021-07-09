using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace InventoryTab
{
    public class Slot : IComparable<Slot>
    {
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

        //This is the thing that is in this slot
        public Thing ThingInSlot { get; }
        public MainTabWindow_Inventory.Tabs Tab { get; }

        //Used for when List<T>.Sort is called
        //1 means the object is greater then what it's being compared to
        //-1 means the object is less then what it's being compared to
        // 0 means they are equal
        public int CompareTo(Slot other)
        {
            var firstThingLabel = ThingInSlot.LabelNoCount;
            if (ThingInSlot.TryGetQuality(out var firstQuality))
            {
                firstThingLabel = RemoveQualityInLabel(firstThingLabel, firstQuality);
            }

            var otherThingLabel = other.ThingInSlot.LabelNoCount;
            if (ThingInSlot.TryGetQuality(out var secondQuality))
            {
                otherThingLabel = RemoveQualityInLabel(other.ThingInSlot.LabelNoCount, secondQuality);
            }

            var nameCompared = string.Compare(firstThingLabel, otherThingLabel, StringComparison.CurrentCulture);
            if (ThingInSlot.def.IsWithinCategory(ThingCategoryDefOf.Corpses) &&
                other.ThingInSlot.def.IsWithinCategory(ThingCategoryDefOf.Corpses))
            {
                if (ThingInSlot is Corpse a && other.ThingInSlot is Corpse b && a.InnerPawn.def.race.Humanlike &&
                    b.InnerPawn.def.race.Humanlike)
                {
                    nameCompared = string.Compare(a.InnerPawn.Label, b.InnerPawn.Label,
                        StringComparison.CurrentCulture);
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

        private string RemoveQualityInLabel(string thingLabel, QualityCategory quality)
        {
            var qualityIndex = thingLabel.IndexOf($"({quality.GetLabel()}", StringComparison.Ordinal);
            if (qualityIndex > 0)
            {
                return thingLabel.Substring(0, qualityIndex).Trim();
            }

            return thingLabel;
        }
    }
}