using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace InventoryTab.Helpers
{
    public class ItemFinderHelper
    {
        //This is how we get all the items on the map
        public static List<Thing> GetAllMapItems(Map map, OptionsHelper options)
        {
            var results = new List<Thing>();
            var allThings = map.listerThings.AllThings;
            var allSelectedStorages = new List<Building>();
            if (options.LimitToStorage)
            {
                foreach (var building in Find.Selector.SelectedObjectsListForReading.OfType<Building>())
                {
                    if (building.GetInspectTabs().OfType<ITab_Storage>() != null)
                    {
                        allSelectedStorages.Add(building);
                    }
                }
            }

            var checkInStorage = options.LimitToStorage && allSelectedStorages.Count > 0;

            foreach (var thing in allThings)
            {
                //If the thing is a item and is not in the fog then continue to the next
                if (thing.def.category != ThingCategory.Item || thing.Position.Fogged(thing.Map))
                {
                    continue;
                }

                if (checkInStorage)
                {
                    foreach (var building in allSelectedStorages)
                    {
                        if (thing.OccupiedRect().Intersect(building.OccupiedRect()).Any())
                        {
                            results.Add(thing);
                        }
                    }

                    continue;
                }

                if (options.SearchWholeMap == false)
                {
                    //If it's not in a storage continue to the next
                    if (thing.IsInAnyStorage() == false)
                    {
                        continue;
                    }

                    //Check for apparel and if it has some add it to the list
                    CorpseApparelHandler(thing, ref results, options.SearchPawns);
                    //add the thing to the list
                    results.Add(thing);
                }
                else
                {
                    //Check for apparel and if it has some add it to the list
                    CorpseApparelHandler(thing, ref results, options.SearchPawns);
                    //add the thing to the list
                    results.Add(thing);
                }
            }

            //Handled all the searching all the pawns inventorys
            if (options.SearchPawns != true)
            {
                return results;
            }

            //Get all the pawn. AllPawnsSpawned is the only list i could find that didn't
            //return null
            var pawns = Find.CurrentMap.mapPawns.AllPawnsSpawned;
            foreach (var pawn in pawns)
            {
                //Check if pawn is not null and if not an animal and is apart of the colony as a colonist or a prisoner
                if (pawn == null || !pawn.def.race.Animal && !pawn.IsColonist && !pawn.IsPrisonerOfColony)
                {
                    continue;
                }

                ThingOwner things;
                //Add all the thing from the pawns inventory
                if (pawn.inventory != null)
                {
                    things = pawn.inventory.GetDirectlyHeldThings();
                    foreach (var thing in things)
                    {
                        results.Add(thing);
                    }
                }

                //Add all the thing the pawn has equiped
                if (pawn.equipment != null)
                {
                    things = pawn.equipment.GetDirectlyHeldThings();
                    foreach (var thing in things)
                    {
                        results.Add(thing);
                    }
                }

                //Add all the thing the pawn is wearing Apperal
                if (pawn.apparel == null)
                {
                    continue;
                }

                things = pawn.apparel.GetDirectlyHeldThings();
                foreach (var thing in things)
                {
                    results.Add(thing);
                }
            }

            return results;
        }

        private static void CorpseApparelHandler(Thing thing, ref List<Thing> things, bool searchPawns)
        {
            if (searchPawns == false || thing.def.IsWithinCategory(ThingCategoryDefOf.Corpses) == false)
            {
                return;
            }

            //if thing is not a corpse or an animal or an mechanoid then it dosen't wear apparel so skip it
            if (!(thing is Corpse corpse) || corpse.InnerPawn.def.race.Animal || corpse.InnerPawn.def.race.IsMechanoid)
            {
                return;
            }

            //Add pawns apparel to list
            var pawnApparel = corpse.InnerPawn.apparel.GetDirectlyHeldThings();
            foreach (var item in pawnApparel)
            {
                things.Add(item);
            }
        }
    }
}