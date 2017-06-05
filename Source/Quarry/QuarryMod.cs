﻿using System.Collections.Generic;

using Verse;

namespace Quarry {

  public sealed class QuarryMod : Mod {

    private List<ThingDef> database;
    private List<QuarryResource> resources;
    public static List<QuarryResource> Resources {
      get { return Instance.resources; }
    }

    private static QuarryMod Instance { get; set; }


    public QuarryMod(ModContentPack mcp) : base(mcp) {
      Instance = this;
      LongEventHandler.ExecuteWhenFinished(BuildResourceList);
      LongEventHandler.ExecuteWhenFinished(Echo);
    }


    private void BuildResourceList() {
      database = DefDatabase<ThingDef>.AllDefsListForReading;
      resources = new List<QuarryResource>();

      foreach (SimpleQuarryResource resource in QuarryDefOf.Resources.resources) {
        ThingDef tmpThing = database.Find(t => t.defName == resource.thingDef);
        if (tmpThing != null) {
          resources.Add(new QuarryResource(
            tmpThing,
            resource.probability,
            resource.stackCount));
        }
      }
    }


    private void Echo() {
      Log.Message("Quarry:: Loaded " + Resources.Count + " defs into list.");
    }
  }
}