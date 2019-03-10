﻿using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Quarry {

    public class Designator_ReclaimSoil : Designator {

        public override int DraggableDimensions => 2;
        public override bool DragDrawMeasurements => true;

        public Designator_ReclaimSoil() {
            defaultLabel = Static.LabelReclaimSoil;
            defaultDesc = Static.DescriptionReclaimSoil;
            icon = Static.DesignationReclaimSoil;
            useMouseIcon = true;
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            soundSucceeded = SoundDefOf.Designate_SmoothSurface;
        }


        public override AcceptanceReport CanDesignateCell(IntVec3 c) {
            AcceptanceReport result;
            if (!c.InBounds(Map)) {
                result = false;
            }
            else if (c.Fogged(Map)) {
                result = false;
            }
            else if (Map.designationManager.DesignationAt(c, QuarryDefOf.QRY_Designator_ReclaimSoil) != null ||
                     Map.designationManager.DesignationAt(c, DesignationDefOf.SmoothFloor) != null) {
                result = false;
            }
            else if (c.InNoBuildEdgeArea(Map)) {
                result = "TooCloseToMapEdge".Translate();
            }
            else {
                Building edifice = c.GetEdifice(Map);
                if (edifice != null && (edifice.def.Fillage == FillCategory.Full ||
                                        edifice.def.passability == Traversability.Impassable)) {
                    result = false;
                }
                else {
                    TerrainDef terrain = c.GetTerrain(Map);
                    if (terrain != QuarryDefOf.QRY_QuarriedGround) {
                        result = Static.MessageMustDesignateQuarriedGround;
                    }
                    else {
                        result = AcceptanceReport.WasAccepted;
                    }
                }
            }

            return result;
        }


        public override void DesignateSingleCell(IntVec3 c) {
            Map.designationManager.AddDesignation(new Designation(c, DesignationDefOf.SmoothFloor));
        }


        public override void SelectedUpdate() {
            GenUI.RenderMouseoverBracket();
        }

        public override void RenderHighlight(List<IntVec3> dragCells) {
            DesignatorUtility.RenderHighlightOverSelectableCells(this, dragCells);
        }

    }

}