using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

// ReSharper disable once CheckNamespace
namespace Quarry {

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    public class JobDriver_MineQuarry : JobDriver {

        // ReSharper disable once InconsistentNaming
        private const float MinMiningSpeedFactorForNPCs = 0.5f;
        private const int BaseTicksBetweenPickHits = 120;

        private const TargetIndex CellInd = TargetIndex.A;
        private const TargetIndex HaulableInd = TargetIndex.B;
        private const TargetIndex StorageCellInd = TargetIndex.C;

        private int _ticksToPickHit = -1000;
        private Effecter _effecter;
        private Building_Quarry _quarryBuilding;

        private Building_Quarry Quarry {
            get {
                if (_quarryBuilding == null) {
                    _quarryBuilding = job
                        .GetTarget(CellInd).Cell.GetThingList(Map)
                        .Find(q => q is Building_Quarry) as Building_Quarry;
                }

                return _quarryBuilding;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed) {
            return pawn.Reserve(job.GetTarget(CellInd), job);
        }

        protected override IEnumerable<Toil> MakeNewToils() {
            // Set up fail conditions
            this.FailOn(() => Quarry == null || Quarry.IsForbidden(pawn) || Quarry.Depleted);

            // Go to the quarry
            yield return Toils_Goto.Goto(CellInd, PathEndMode.OnCell);

            // Mine at the quarry. This is only for the delay
            yield return Mine();

            // Collect resources from the quarry
            yield return Collect();

            // Reserve the resource
            yield return Toils_Reserve.Reserve(HaulableInd);

            // Reserve the storage cell
            yield return Toils_Reserve.Reserve(StorageCellInd);

            // Go to the resource
            yield return Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch);

            // Pick up the resource
            yield return Toils_Haul.StartCarryThing(HaulableInd);

            // Carry the resource to the storage cell, then place it down
            Toil carry = Toils_Haul.CarryHauledThingToCell(StorageCellInd);
            yield return carry;

            yield return Toils_Haul.PlaceHauledThingInCell(StorageCellInd, carry, true);
        }

        private void ResetTicksToPickHit() {
            float miningSpeed = pawn.GetStatValue(StatDefOf.MiningSpeed);

            if (pawn.Faction != Faction.OfPlayer && miningSpeed < MinMiningSpeedFactorForNPCs) {
                miningSpeed = MinMiningSpeedFactorForNPCs;
            }

            _ticksToPickHit = Mathf.RoundToInt(BaseTicksBetweenPickHits / miningSpeed);
        }

        private void MineAction() {
            pawn.rotationTracker.Face(Quarry.Position.ToVector3Shifted());

            _ticksToPickHit--;
            if (_ticksToPickHit > 0)
                return;

            // TODO: remove magic number
            pawn.skills?.Learn(SkillDefOf.Mining, xp: 0.11f, direct: false);

            if (_effecter == null) {
                _effecter = EffecterDefOf.Mine.Spawn();
            }

            _effecter.Trigger(pawn, Quarry);

            ResetTicksToPickHit();
        }


        private Toil Mine() {
            float miningSpeed = pawn.GetStatValue(StatDefOf.MiningSpeed);

            return new Toil() {
                tickAction = MineAction,
                defaultDuration = (int) Mathf.Clamp(3000 / miningSpeed, 500, 10000),
                defaultCompleteMode = ToilCompleteMode.Delay,
                handlingFacing = true
            }.WithProgressBarToilDelay(HaulableInd, false, -0.5f);
        }

        private void CollectAction() {
            // Increment the record for how many cells this pawn has mined at quarry
            // TODO: Check nothing broken
            pawn.records.Increment(QuarryDefOf.QRY_CellsMined);

            // Use the mineModeToggle to determine the request
            ResourceRequest requestedResources = Quarry.MineModeToggle
                ? ResourceRequest.Resources
                : ResourceRequest.Blocks;

            // Get the resource from the quarry
            ThingDef givenResourceType = Quarry.GiveResources(requestedResources, out MoteType mote,
                out bool singleSpawn, out bool eventTriggered);

            // If something went wrong, bail out
            if (givenResourceType?.thingClass == null) {
                // This shouldn't happen at all, but if it does let's add a little reward
                // instead of just giving rubble
                givenResourceType = ThingDefOf.ChunkSlagSteel;
                mote = MoteType.None;
                singleSpawn = true;

                Log.Warning("Quarry:: Tried to quarry mineable ore, but the ore given was null.");
            }

            bool givenResourceIsComponent = givenResourceType == ThingDefOf.ComponentIndustrial;
            // TODO: rework - filth does not nave categories
            bool givenResourceIsChunk = givenResourceType.thingCategories?.Contains(QuarryDefOf.StoneChunks) == true;

            int stackCount;

            if (singleSpawn) {
                stackCount = 1;
            }
            else if (givenResourceIsComponent) {
                stackCount = Rand.Range(1, 2);
            }
            else {
                // TODO: Check if BaseMarketValue could be negative, maybe replace with
                // int sub = (int) Math.Min(givenResourceDefinition.BaseMarketValue / 2f, 10f);
                int sub = (int) (givenResourceType.BaseMarketValue / 2f);
                sub = Mathf.Clamp(sub, 0, 10);

                stackCount = Math.Min(Rand.RangeInclusive(15 - sub, 40 - 2 * sub), givenResourceType.stackLimit);
            }

            if (stackCount >= 30) {
                mote = MoteType.LargeVein;
            }

            Thing givenResource = ThingMaker.MakeThing(givenResourceType);
            givenResource.stackCount = stackCount;

            // Adjust quality for items that use it
            bool usesQuality = false;
            CompQuality qualityProperty = givenResource.TryGetComp<CompQuality>();
            if (qualityProperty != null) {
                usesQuality = true;
                qualityProperty.SetQuality(QualityUtility.GenerateQualityTraderItem(), ArtGenerationContext.Outsider);
            }

            // Adjust hitpoints, this was just mined from under the ground after all
            if (givenResourceType.useHitPoints && !givenResourceIsChunk && !givenResourceIsComponent) {
                float minHpThresh = usesQuality
                    ? Mathf.Clamp((float) qualityProperty.Quality / 10f, 0.1f, 0.7f)
                    : 0.25f;

                int hitPoints = Mathf.RoundToInt(Rand.Range(minHpThresh, 1f) * givenResource.MaxHitPoints);
                givenResource.HitPoints = Mathf.Max(1, hitPoints);
            }

            // Place the resource near the pawn
            GenPlace.TryPlaceThing(givenResource, pawn.Position, Map, ThingPlaceMode.Near);

            // If the resource had a mote, throw it
            switch (mote) {
                case MoteType.LargeVein:
                    MoteMaker.ThrowText(givenResource.DrawPos, Map, Static.TextMote_LargeVein, Color.green, 3f);
                    break;
                case MoteType.Failure:
                    MoteMaker.ThrowText(givenResource.DrawPos, Map, Static.TextMote_MiningFailed, Color.red, 3f);
                    break;
                case MoteType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // If the sinkhole event was triggered, damage the pawn and end this job
            // Even if the sinkhole doesn't incapacitate the pawn, they will probably want to seek medical attention
            if (eventTriggered) {
                NamedArgument pawnName = new NamedArgument(0, pawn.Name.ToStringShort);
                Messages.Message("QRY_MessageSinkhole".Translate(pawnName), pawn, MessageTypeDefOf.NegativeEvent);

                DamageInfo damageInfo =
                    new DamageInfo(DamageDefOf.Crush, 9, category: DamageInfo.SourceCategory.Collapse);
                damageInfo.SetBodyRegion(BodyPartHeight.Bottom, BodyPartDepth.Inside);
                pawn.TakeDamage(damageInfo);

                EndJobWith(JobCondition.Succeeded);
            }

            // Prevent the colonists from trying to haul rubble, which just makes them visit the platform
            else if (givenResourceType == ThingDefOf.Filth_RubbleRock) {
                EndJobWith(JobCondition.Succeeded);
            }

            else if (Quarry.AutoHaul) {
                // If this is a chunk or slag, mark it as haulable if allowed to
                if (givenResourceType.designateHaulable) {
                    Designation haulDesignation = new Designation(givenResource, DesignationDefOf.Haul);
                    Map.designationManager.AddDesignation(haulDesignation);
                }

                // Try to find a suitable storage spot for the resource, removing it from the quarry
                // If there are no platforms with free space, or if the resource is a chunk,
                // try to haul it to a storage area
                bool platformFound =
                    Quarry.TryFindBestPlatformCell(givenResource, pawn, Map, pawn.Faction, out IntVec3 cell);

                if (platformFound && !givenResourceIsChunk) {
                    job.SetTarget(HaulableInd, givenResource);
                    job.count = givenResource.stackCount;
                    job.SetTarget(StorageCellInd, cell);
                }

                else {
                    StoragePriority resourceStoragePriority = StoreUtility.CurrentStoragePriorityOf(givenResource);

                    bool storageFound = StoreUtility.TryFindBestBetterStorageFor(givenResource, pawn, Map,
                        resourceStoragePriority, pawn.Faction, out cell, out IHaulDestination haulDestination, true);

                    if (!storageFound) {
                        JobFailReason.Is("NoEmptyPlaceLower".Translate(), null);
                    }

                    else if (haulDestination is ISlotGroupParent) {
                        HaulAIUtility.HaulToCellStorageJob(pawn, givenResource, cell, false);
                    }

                    else {
                        job.SetTarget(HaulableInd, givenResource);
                        job.count = givenResource.stackCount;
                        job.SetTarget(StorageCellInd, cell);
                    }
                }
            }

            // If there is no spot to store the resource, end this job
            else {
                EndJobWith(JobCondition.Succeeded);
            }
        }

        private Toil Collect() {
            return new Toil {
                initAction = CollectAction,
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look<int>(ref _ticksToPickHit, "ticksToPickHit", 0, false);
        }

    }

}