using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace Quarry {

    // ReSharper disable once UnusedMember.Global
    public class PlaceWorker_Quarry : PlaceWorker {

        public override AcceptanceReport AllowsPlacing(
            BuildableDef building, IntVec3 location, Rot4 rotation, Map map, Thing thingToIgnore = null
        ) {
            // God Mode allows placing the quarry without the grid restriction
            if (DebugSettings.godMode)
                return true;

            QuarryGrid quarryGrid = map.GetComponent<QuarryGrid>();
            List<IntVec3> cellsUnderBuilding = GenAdj.CellsOccupiedBy(location, rotation, building.Size).ToList();

            int occupied = cellsUnderBuilding.Count;
            int rocky = cellsUnderBuilding.Count(cell => quarryGrid.GetCellBool(cell));

            // Require at least 60% rocky terrain
            if (rocky < 0.6 * occupied) {
                return Static.ReportNotEnoughStone;
            }

            return true;
        }


        public override void DrawGhost(ThingDef building, IntVec3 center, Rot4 rotation, Color color) {
            // God Mode allows placing the quarry without the need of grid
            if (DebugSettings.godMode)
                return;

            // Draw the placement areas
            Find.CurrentMap.GetComponent<QuarryGrid>().MarkForDraw();
            GenDraw.DrawFieldEdges(GenAdj.CellsOccupiedBy(center, rotation, building.Size).ToList());
        }

    }

}