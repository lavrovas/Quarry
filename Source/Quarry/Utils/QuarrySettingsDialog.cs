using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace Quarry {

    // ReSharper disable once UnusedMember.Global
    public sealed partial class QuarryMod {

        private static void DrawBuildingHealthControls(Listing listing) {

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

        private static void DrawJunkChanceControls(Listing listing) {

            Rect junkRect = listing.GetRect(Text.LineHeight);

            Widgets.Label(junkRect.LeftHalf().Rounded(),
                "QRY_SettingsJunkChance".Translate(QuarrySettings.junkChance));

            Rect junkSliderOffset = junkRect.RightHalf().Rounded();
            QuarrySettings.junkChance = Widgets.HorizontalSlider(
                junkSliderOffset, QuarrySettings.junkChance, 0f, 100f, true
            ).RoundToAsInt(5);

            if (Mouse.IsOver(junkRect)) {
                Widgets.DrawHighlight(junkRect);
            }

            TooltipHandler.TipRegion(junkRect, Static.ToolTipJunkChance);
        }

        private static void DrawChunckChanceControls(Listing listing) {

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

        private static void DrawLetterSendControls(Listing listing) {

            Rect letterRect = listing.GetRect(Text.LineHeight).LeftHalf().Rounded();

            Widgets.CheckboxLabeled(letterRect, Static.LetterSent, ref QuarrySettings.letterSent);

            if (Mouse.IsOver(letterRect)) {
                Widgets.DrawHighlight(letterRect);
            }

            TooltipHandler.TipRegion(letterRect, Static.ToolTipLetter);
        }

        private static void DrawTableHeader(Listing listing) {

            Rect labelRect = listing.GetRect(32).Rounded();

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;

            Widgets.Label(labelRect, Static.LabelDictionary);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DrawButtons(Listing listing) {

            Rect buttonsRect = listing.GetRect(Text.LineHeight).Rounded();

            // Add an entry to the dictionary
            DrawAddButton(buttonsRect);

            // Remove an entry from the dictionary
            DrawRemoveButton(buttonsRect);

            // Reset the dictionary
            DrawResetButton(buttonsRect);
        }

        private static void DrawAddButton(Rect buttonsRect) {

            Rect addRect = buttonsRect.LeftThird();
            if (!Widgets.ButtonText(addRect, Static.LabelAddThing))
                return;

            List<FloatMenuOption> addMenuOptions = QuarryUtility.PossibleThingDefs()
                .Where(thing => !QuarrySettings.oreDictionary.ContainsKey(thing))
                .OrderBy(thing => thing.label)
                .Select(thing =>
                    new FloatMenuOption(thing.LabelCap, () => QuarrySettings.oreDictionary.Add(thing, 1)))
                .ToList();

            FloatMenu menu = new FloatMenu(addMenuOptions);
            Find.WindowStack.Add(menu);
        }

        private static void DrawRemoveButton(Rect buttonsRect) {

            Rect rmvRect = buttonsRect.MiddleThird();
            if (!Widgets.ButtonText(
                    rmvRect, Static.LabelRemoveThing, active: QuarrySettings.oreDictionary.Count >= 2
                )
            )
                return;

            List<FloatMenuOption> removeMenuOptions = QuarrySettings.oreDictionary
                .Select(pair => pair.Key)
                .OrderBy(thing => thing.label)
                .Select(thing =>
                    new FloatMenuOption(thing.LabelCap, () => QuarrySettings.oreDictionary.Remove(thing)))
                .ToList();

            FloatMenu menu = new FloatMenu(removeMenuOptions);
            Find.WindowStack.Add(menu);
        }

        private static void DrawResetButton(Rect buttonsRect) {

            Rect resRect = buttonsRect.RightThird();
            if (Widgets.ButtonText(resRect, Static.LabelResetList)) {
                OreDictionary.Build();
            }
        }

        private void DrawTable(Listing listing, float height) {

            Rect listRect = listing.GetRect(height).Rounded();

            Rect position = listRect.ContractedBy(10f);
            // Rect position = new Rect(cRect.x, cRect.y, cRect.width, cRect.height);

            Rect outRect = new Rect(0f, 0f, position.width, position.height);
            Rect viewRect = new Rect(0f, 0f, position.width - 16f, scrollViewHeight);

            GUI.BeginGroup(position);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);

            var indexedOres = QuarrySettings.oreDictionary.Select((pair, index) => new {pair, index});

            foreach (var ore in indexedOres) {
                int index = ore.index;
                ThingDef definition = ore.pair.Key;
                int weight = ore.pair.Value;

                height = 32f;

                Rect row = new Rect(0f, height * index, viewRect.width, height);

                Rect iconRect = row.LeftPartPixels(height);
                Rect labelRect = new Rect(
                    row.LeftThird().x + 33f,
                    row.y,
                    row.LeftThird().width - 33f,
                    row.height
                );
                Rect texEntryRect = new Rect(
                    row.LeftHalf().RightPartPixels(103).x,
                    row.y,
                    60f,
                    row.height);
                Rect pctRect = new Rect(
                    row.LeftHalf().RightPartPixels(41).x,
                    row.y,
                    40f,
                    row.height
                );
                Rect sliderRect = new Rect(
                    row.RightHalf().x,
                    row.y,
                    row.RightHalf().width,
                    row.height
                );

                Widgets.ThingIcon(iconRect, definition);
                Widgets.Label(labelRect, definition.LabelCap);
                Widgets.Label(pctRect,
                    $"{QuarrySettings.oreDictionary.WeightAsPercentageOf(weight).ToStringDecimal()}%");

                string nullString = null;
                Widgets.TextFieldNumeric(
                    texEntryRect,
                    ref weight,
                    ref nullString,
                    0, OreDictionary.MaxWeight);

                weight = Widgets.HorizontalSlider(
                    sliderRect,
                    weight, 0f, OreDictionary.MaxWeight, true
                ).RoundToAsInt(1);

                QuarrySettings.oreDictionary[definition] = weight;

                if (Mouse.IsOver(row)) {
                    Widgets.DrawHighlight(row);
                }

                TooltipHandler.TipRegion(row.LeftThird(), definition.description);

                scrollViewHeight += height * index;
            }

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

    }

}