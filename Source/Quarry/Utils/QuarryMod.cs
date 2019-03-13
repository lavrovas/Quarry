using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace Quarry {

    // ReSharper disable once UnusedMember.Global
    public sealed class QuarryMod : Mod {

        private Vector2 scrollPosition = Vector2.zero;
        private float scrollViewHeight = 0f;

        public QuarryMod(ModContentPack mcp) : base(mcp) {
            LongEventHandler.ExecuteWhenFinished(GetSettings);
            LongEventHandler.ExecuteWhenFinished(PushDatabase);
            LongEventHandler.ExecuteWhenFinished(BuildDictionary);
        }

        private void GetSettings() {
            GetSettings<QuarrySettings>();
        }

        private void PushDatabase() {
            QuarrySettings.database = DefDatabase<ThingDef>.AllDefsListForReading;
        }

        private static void BuildDictionary() {
            if (QuarrySettings.oreDictionary == null) {
                QuarrySettings.oreDictionary = OreDictionary.Build();
            }
        }

        public override string SettingsCategory() {
            return Static.Quarry;
        }

        public override void DoSettingsWindowContents(Rect rect) {

            Listing_Standard listing = new Listing_Standard {
                ColumnWidth = rect.width
            };

            listing.Begin(rect);

            listing.Gap(10);
            {
                {
                    Rect fullRect = listing.GetRect(Text.LineHeight);
                    Rect leftRect = fullRect.LeftHalf().Rounded();
                    Rect rightRect = fullRect.RightHalf().Rounded();

                    string depletionLabel = QuarrySettings.quarryMaxHealth <= 10000
                        ? "QRY_DepletionLabel".Translate(QuarrySettings.quarryMaxHealth.ToString("N0"))
                        : "QRY_DepletionLabel".Translate("Infinite");
                    Widgets.Label(leftRect, depletionLabel);

                    //Increment timer value by -100 (button).
                    Rect minusButtonRectangle =
                        new Rect(rightRect.xMin, rightRect.y, rightRect.height, rightRect.height);
                    bool minusButtonPressed = Widgets.ButtonText(minusButtonRectangle, "-", true, false, true);
                    if (minusButtonPressed) {
                        if (QuarrySettings.quarryMaxHealth >= 200) {
                            QuarrySettings.quarryMaxHealth -= 100;
                        }
                    }

                    Rect maxHealthSliderRectangle = new Rect(
                        rightRect.xMin + rightRect.height + 10f, rightRect.y,
                        rightRect.width - rightRect.height * 2 - 20f, rightRect.height
                    );
                    QuarrySettings.quarryMaxHealth = Widgets.HorizontalSlider(maxHealthSliderRectangle,
                        QuarrySettings.quarryMaxHealth, 100f, 10100f, true
                    ).RoundToAsInt(100);

                    //Increment timer value by +100 (button).
                    Rect plusButtonRectangle =
                        new Rect(rightRect.xMax - rightRect.height, rightRect.y, rightRect.height, rightRect.height);
                    bool plusButtonPressed = Widgets.ButtonText(plusButtonRectangle, "+", true, false, true);
                    if (plusButtonPressed) {
                        if (QuarrySettings.quarryMaxHealth < 10100) {
                            QuarrySettings.quarryMaxHealth += 100;
                        }
                    }

                    // TODO: no tooltip here, why?
                }

                listing.Gap(10);
                {
                    Rect junkRect = listing.GetRect(Text.LineHeight);
                    
                    Widgets.Label(junkRect.LeftHalf().Rounded(), "QRY_SettingsJunkChance".Translate(QuarrySettings.junkChance));

                    Rect junkSliderOffset = junkRect.RightHalf().Rounded();
                    QuarrySettings.junkChance = Widgets.HorizontalSlider(
                        junkSliderOffset,QuarrySettings.junkChance, 0f, 100f, true
                    ).RoundToAsInt(5);

                    if (Mouse.IsOver(junkRect)) {
                        Widgets.DrawHighlight(junkRect);
                    }

                    TooltipHandler.TipRegion(junkRect, Static.ToolTipJunkChance);
                }

                listing.Gap(10);
                {
                    Rect chunkRect = listing.GetRect(Text.LineHeight);
                    Rect chunkLabelOffset = chunkRect.LeftHalf().Rounded();
                    Rect chunkSliderOffset = chunkRect.RightHalf().Rounded();

                    Widgets.Label(chunkLabelOffset, "QRY_SettingsChunkChance".Translate(QuarrySettings.chunkChance));
                    QuarrySettings.chunkChance = Widgets.HorizontalSlider(
                        chunkSliderOffset,
                        QuarrySettings.chunkChance, 0f, 100f, true
                    ).RoundToAsInt(5);

                    if (Mouse.IsOver(chunkRect)) {
                        Widgets.DrawHighlight(chunkRect);
                    }

                    TooltipHandler.TipRegion(chunkRect, Static.ToolTipChunkChance);
                }

                // TODO: consider if it is nessessary to allow user resend letter
                listing.Gap(10);
                {
                    Rect letterRect = listing.GetRect(Text.LineHeight).LeftHalf().Rounded();

                    Widgets.CheckboxLabeled(letterRect, Static.LetterSent, ref QuarrySettings.letterSent);

                    if (Mouse.IsOver(letterRect)) {
                        Widgets.DrawHighlight(letterRect);
                    }

                    TooltipHandler.TipRegion(letterRect, Static.ToolTipLetter);
                }

                listing.Gap(15);
                {
                    Rect labelRect = listing.GetRect(32).Rounded();

                    Text.Font = GameFont.Medium;
                    Text.Anchor = TextAnchor.MiddleCenter;

                    Widgets.Label(labelRect, Static.LabelDictionary);

                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                }

                listing.Gap(1);
                {
                    Rect buttonsRect = listing.GetRect(Text.LineHeight).Rounded();
                    Rect addRect = buttonsRect.LeftThird();
                    Rect rmvRect = buttonsRect.MiddleThird();
                    Rect resRect = buttonsRect.RightThird();

                    // Add an entry to the dictionary
                    if (Widgets.ButtonText(addRect, Static.LabelAddThing)) {
                        List<FloatMenuOption> thingList = new List<FloatMenuOption>();
                        foreach (ThingDef current in from t in QuarryUtility.PossibleThingDefs()
                            orderby t.label
                            select t) {
                            bool skip = false;
                            for (int i = 0; i < QuarrySettings.oreDictionary.Count; i++) {
                                if (QuarrySettings.oreDictionary[i].thingDef == current) {
                                    skip = true;
                                    break;
                                }
                            }

                            ;
                            if (skip) continue;

                            thingList.Add(new FloatMenuOption(current.LabelCap,
                                delegate { QuarrySettings.oreDictionary.Add(new ThingCountExposable(current, 1)); }));
                        }

                        FloatMenu menu = new FloatMenu(thingList);
                        Find.WindowStack.Add(menu);
                    }

                    // Remove an entry from the dictionary
                    if (Widgets.ButtonText(rmvRect, Static.LabelRemoveThing) &&
                        QuarrySettings.oreDictionary.Count >= 2) {
                        List<FloatMenuOption> thingList = new List<FloatMenuOption>();
                        foreach (ThingCountExposable current in from t in QuarrySettings.oreDictionary
                            orderby t.thingDef.label
                            select t) {
                            ThingDef localTd = current.thingDef;
                            thingList.Add(new FloatMenuOption(localTd.LabelCap, delegate {
                                for (int i = 0; i < QuarrySettings.oreDictionary.Count; i++) {
                                    if (QuarrySettings.oreDictionary[i].thingDef == localTd) {
                                        QuarrySettings.oreDictionary.Remove(QuarrySettings.oreDictionary[i]);
                                        break;
                                    }
                                }

                                ;
                            }));
                        }

                        FloatMenu menu = new FloatMenu(thingList);
                        Find.WindowStack.Add(menu);
                    }

                    // Reset the dictionary
                    if (Widgets.ButtonText(resRect, Static.LabelResetList)) {
                        OreDictionary.Build();
                    }
                }

                listing.Gap(5);
                {
                    float height = rect.height - listing.CurHeight;
                    Rect listRect = listing.GetRect(height).Rounded();
                    
                    Rect cRect = listRect.ContractedBy(10f);
                    Rect position = new Rect(cRect.x, cRect.y, cRect.width, cRect.height);
                    Rect outRect = new Rect(0f, 0f, position.width, position.height);
                    Rect viewRect = new Rect(0f, 0f, position.width - 16f, scrollViewHeight);

                    float num = 0f;
                    List<ThingCountExposable> dict = new List<ThingCountExposable>(QuarrySettings.oreDictionary);

                    GUI.BeginGroup(position);
                    Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);

                    foreach (var tc in dict.Select((value, index) => new {index, value})) {
                        Rect entryRect = new Rect(0f, num, viewRect.width, 32);
                        Rect iconRect = entryRect.LeftPartPixels(32);
                        Rect labelRect = new Rect(entryRect.LeftThird().x + 33f, entryRect.y,
                            entryRect.LeftThird().width - 33f, entryRect.height);
                        Rect texEntryRect = new Rect(entryRect.LeftHalf().RightPartPixels(103).x, entryRect.y, 60f,
                            entryRect.height);
                        Rect pctRect = new Rect(entryRect.LeftHalf().RightPartPixels(41).x, entryRect.y, 40f,
                            entryRect.height);
                        Rect sliderRect = new Rect(entryRect.RightHalf().x, entryRect.y, entryRect.RightHalf().width,
                            entryRect.height);

                        Widgets.ThingIcon(iconRect, tc.value.thingDef);
                        Widgets.Label(labelRect, tc.value.thingDef.LabelCap);
                        Widgets.Label(pctRect,
                            $"{QuarrySettings.oreDictionary.WeightAsPercentageOf(tc.value.count).ToStringDecimal()}%");

                        string nullString = null;

                        Widgets.TextFieldNumeric(
                            texEntryRect,
                            ref QuarrySettings.oreDictionary[tc.index].count,
                            ref nullString,
                            0, OreDictionary.MaxWeight);

                        int val = Widgets.HorizontalSlider(
                            sliderRect,
                            QuarrySettings.oreDictionary[tc.index].count, 0f, OreDictionary.MaxWeight, true
                        ).RoundToAsInt(1);

                        QuarrySettings.oreDictionary[tc.index].count = val;

                        if (Mouse.IsOver(entryRect)) {
                            Widgets.DrawHighlight(entryRect);
                        }

                        TooltipHandler.TipRegion(entryRect.LeftThird(), tc.value.thingDef.description);

                        num += 32f;
                        scrollViewHeight = num;
                    }

                    Widgets.EndScrollView();
                    GUI.EndGroup();
                }

                listing.End();
            }
        }

    }

}