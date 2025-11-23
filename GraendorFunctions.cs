
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using static Obeliskial_Essentials.Essentials;
using System;
using static Graendor.CustomFunctions;
using static Graendor.Plugin;
using static Graendor.Traits;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;


// Make sure your namespace is the same everywhere
namespace Graendor
{

    // [HarmonyPatch] //DO NOT REMOVE/CHANGE

    public class DescriptionFunctions
    {
        // To create a patch, you need to declare either a prefix or a postfix. 
        // Prefixes are executed before the original code, postfixes are executed after
        // Then you need to tell Harmony which method to patch.
        // public static string itemStem = $"{subclassname}";
        public static List<string> cardsWithCustomDescriptions = [];
        public static List<string> cardsToAppendDescription = [];
        public static List<string> cardsToPrependDescription = ["SubclassToReplaceformulaa", "SubclassToReplaceformulab"];

        public static string NumFormatItem(int num, bool plus = false, bool percent = false)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(" <nobr>");
            if (num > 0)
            {
                stringBuilder.Append("<color=#263ABC><size=+.1>");
                if (plus)
                    stringBuilder.Append("+");
            }
            else
            {
                stringBuilder.Append("<color=#720070><size=+.1>");
                if (plus)
                    stringBuilder.Append("-");
            }
            stringBuilder.Append(Mathf.Abs(num));
            if (percent)
                stringBuilder.Append("%");
            stringBuilder.Append("</color></size></nobr>");
            return stringBuilder.ToString();
        }

        public static void BinbinNormalizeDescription(ref CardData __instance, StringBuilder stringBuilder)
        {
            stringBuilder.Replace("<c>", "<color=#5E3016>");
            stringBuilder.Replace("</c>", "</color>");
            stringBuilder.Replace("<nb>", "<nobr>");
            stringBuilder.Replace("</nb>", "</nobr>");
            stringBuilder.Replace("<br1>", "<br><line-height=15%><br></line-height>");
            stringBuilder.Replace("<br2>", "<br><line-height=30%><br></line-height>");
            stringBuilder.Replace("<br3>", "<br><line-height=50%><br></line-height>");
            string descriptionNormalized = stringBuilder.ToString();
            descriptionNormalized = Regex.Replace(descriptionNormalized, "[,][ ]*(<(.*?)>)*(.)", (MatchEvaluator)(m => m.ToString().ToLower()));
            descriptionNormalized = Regex.Replace(descriptionNormalized, "<br>\\w", (MatchEvaluator)(m => m.ToString().ToUpper()));
            Globals.Instance.CardsDescriptionNormalized[__instance.Id] = stringBuilder.ToString();
            __instance.DescriptionNormalized = descriptionNormalized;
            Traverse.Create(__instance).Field("descriptionNormalized").SetValue(descriptionNormalized);
        }

        public static void HandleDamagePercentDescription(ref StringBuilder stringBuilder, ItemData itemData, Enums.DamageType damageType, float percentIncrease)
        {
            if (itemData.Id == "battleaxerare")
            {
                LogDebug(" battleaxerare");
            }

            if (damageType == Enums.DamageType.None || damageType == Enums.DamageType.All || percentIncrease == 0f)
                return;

            // LogDebug("itemAllDamages text string - " + Texts.Instance.GetText("itemAllDamages"));
            string dt = damageType.ToString().ToLower();
            // string dt = "All";
            int percentDamageIncrease = Functions.FuncRoundToInt(itemData.DamagePercentBonusValue);
            LogDebug("damage type - " + dt);
            string damageTypeText = "item" + dt + "Damages";
            LogDebug("medsText - " + medsTexts[damageTypeText]);
            LogDebug("GetText - " + Texts.Instance.GetText(damageTypeText));
            // stringBuilder.Append(string.Format(medsTexts[damageTypeText], (object)NumFormatItem(percentDamageIncrease, true, true)));

            // this should use Texts.Instance.GetText(damageTypeText)) for translation. Don't know how to get it working. Need to add MedsTexts to Texts
            string toAdd = string.Format(medsTexts[damageTypeText], (object)NumFormatItem(percentDamageIncrease, true, true)) + "\n";

            // Adds it either to the start of the stringbuilder or immediately after
            // string searchPhrase = $"<space=.3><size=+.1><sprite name={dt}></size> damage ";
            // int insertIndex = stringBuilder.ToString().IndexOf(searchPhrase);
            // if (insertIndex != -1)
            // {
            //     stringBuilder.Insert(insertIndex + searchPhrase.Length, toAdd);
            // }
            // else
            // {
            //     stringBuilder.Insert(0,toAdd);
            // }
            stringBuilder.Insert(0, toAdd);

            // "<space=.3><size=+.1><sprite name=fire></size> damage  <nobr><color=#263ABC><size=+.1>+3</color></size></nobr>"
            // stringBuilder.Append(string.Format(Texts.Instance.GetText("itemAllDamages"), (object)NumFormatItem(percentDamageIncrease, true, true)));
            // stringBuilder.Append("\n");
        }

        public static void HandleAllDamagePercentDescriptions(ref CardData __instance)
        {
            StringBuilder stringBuilder1 = new();
            if (__instance.Item == null)
                return;

            ItemData itemData = __instance.Item;

            if (itemData.DamagePercentBonus == Enums.DamageType.None && itemData.DamagePercentBonus2 == Enums.DamageType.None && itemData.DamagePercentBonus3 == Enums.DamageType.None)
                return;

            // if (__instance.Id == "burningorbrare" || __instance.Id == "frozenorbrare")
            // {
            //     LogDebug("Setting Description for " + __instance.Id);
            // LogDebug("Original CardDescription - " + Globals.Instance.CardsDescriptionNormalized[__instance.Id]);
            // LogDebug("");
            // }

            stringBuilder1.Append(Globals.Instance.CardsDescriptionNormalized[__instance.Id]);
            HandleDamagePercentDescription(ref stringBuilder1, itemData, __instance.Item.DamagePercentBonus, __instance.Item.DamagePercentBonusValue);
            HandleDamagePercentDescription(ref stringBuilder1, itemData, __instance.Item.DamagePercentBonus2, __instance.Item.DamagePercentBonusValue2);
            HandleDamagePercentDescription(ref stringBuilder1, itemData, __instance.Item.DamagePercentBonus3, __instance.Item.DamagePercentBonusValue3);
            // stringBuilder1.Append("\n Testing\n");
            BinbinNormalizeDescription(ref __instance, stringBuilder1);
        }



        public static void UpdateMaxMadnessChargesByItem(ref AuraCurseData __result, Character characterOfInterest, string itemID)
        {
            if (__result == null)
            {
                LogDebug("null AuraCurse");
                return;
            }
            // if(itemID == "ringoffire")
            // {
            //     LogDebug("UpdateChargesByItem: " + itemID );
            //     LogDebug($"Team have: {itemID} {AtOManager.Instance.TeamHaveItem(itemID)} ");
            //     LogDebug($"Character have: {itemID} {IfCharacterHas(characterOfInterest, CharacterHas.Item, itemID, AppliesTo.Global)} ");
            // }


            AppliesTo appliesTo = __result.IsAura ? AppliesTo.Heroes : AppliesTo.Monsters;

            if (IfCharacterHas(characterOfInterest, CharacterHas.Item, itemID + "rare", appliesTo))
            {
                LogDebug("UpdateChargesByItem: " + itemID + "rare");
                ItemData itemData = Globals.Instance.GetItemData(itemID + "rare");
                if (itemData == null)
                    return;

                if (__result.MaxCharges != -1)
                {
                    __result.MaxCharges += itemData.AuracurseCustomModValue1;
                }
                if (__result.MaxMadnessCharges != -1)
                {
                    __result.MaxMadnessCharges += itemData.AuracurseCustomModValue1;
                }
            }
            else if (IfCharacterHas(characterOfInterest, CharacterHas.Item, itemID, appliesTo))
            {
                LogDebug("UpdateChargesByItem: " + itemID);
                ItemData itemData = Globals.Instance.GetItemData(itemID);
                if (itemData == null)
                    return;

                if (__result.MaxCharges != -1)
                {
                    __result.MaxCharges += itemData.AuracurseCustomModValue1;
                }
                if (__result.MaxMadnessCharges != -1)
                {
                    __result.MaxMadnessCharges += itemData.AuracurseCustomModValue1;
                }

                LogDebug($"UpdateChargesByItem: {itemID} - post update max charges {__result.MaxMadnessCharges}");

            }

        }



        public static string SpriteText(string sprite)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string text = sprite.ToLower().Replace(" ", "");
            switch (text)
            {
                case "block":
                case "card":
                    stringBuilder.Append("<space=.2>");
                    break;
                case "piercing":
                    stringBuilder.Append("<space=.4>");
                    break;
                case "bleed":
                    stringBuilder.Append("<space=.1>");
                    break;
                case "bless":
                    stringBuilder.Append("<space=.1>");
                    break;
                default:
                    stringBuilder.Append("<space=.3>");
                    break;
            }
            stringBuilder.Append(" <space=-.2>");
            stringBuilder.Append("<size=+.1><sprite name=");
            stringBuilder.Append(text);
            stringBuilder.Append("></size>");
            switch (text)
            {
                case "bleed":
                    stringBuilder.Append("<space=-.4>");
                    break;
                case "card":
                    stringBuilder.Append("<space=-.2>");
                    break;
                case "powerful":
                case "fury":
                    stringBuilder.Append("<space=-.1>");
                    break;
                default:
                    stringBuilder.Append("<space=-.2>");
                    break;
                case "reinforce":
                case "fire":
                    break;
            }
            return stringBuilder.ToString();
        }



        public static string ColorTextArray(string type, params string[] text)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("<nobr>");
            switch (type)
            {
                case "":
                    int num = 0;
                    foreach (string str in text)
                    {
                        if (num > 0)
                            stringBuilder.Append(" ");
                        stringBuilder.Append(str);
                        ++num;
                    }
                    if (type != "")
                        stringBuilder.Append("</color>");
                    stringBuilder.Append("</nobr> ");
                    return stringBuilder.ToString();
                case "damage":
                    stringBuilder.Append("<color=#B00A00>");
                    goto case "";
                case "heal":
                    stringBuilder.Append("<color=#1E650F>");
                    goto case "";
                case "aura":
                    stringBuilder.Append("<color=#263ABC>");
                    goto case "";
                case "curse":
                    stringBuilder.Append("<color=#720070>");
                    goto case "";
                case "system":
                    stringBuilder.Append("<color=#5E3016>");
                    goto case "";
                default:
                    stringBuilder.Append("<color=#5E3016");
                    stringBuilder.Append(">");
                    goto case "";
            }
        }

        public static void AppendDescriptionsToCards(CardData __instance, ref StringBuilder stringBuilder1)
        {
            if (cardsToAppendDescription.Contains(__instance.Id))
            {
                LogDebug("Creating description for " + __instance.Id);
                LogDebug($"Current description {__instance.Id}: {stringBuilder1}");

                string descriptionId = itemStem + __instance.Id;
                stringBuilder1.Append(Functions.FormatStringCard(Texts.Instance.GetText(descriptionId)));
            }
        }

        public static void PrependDescriptionsToCards(CardData __instance, ref StringBuilder stringBuilder1)
        {
            if (cardsToPrependDescription.Contains(__instance.Id))
            {
                LogDebug("Creating description for " + __instance.Id);
                LogDebug($"Current description {__instance.Id}: {stringBuilder1}");

                string descriptionId = itemStem + __instance.Id;
                stringBuilder1.Insert(0, Functions.FormatStringCard(Texts.Instance.GetText(descriptionId)));
            }
        }




    }
    public class CharacterFunctions
    {
        public static void HandleOverflowingMaliceTrait2a(ref Character __instance, int amountOverhealed, string traitOfInterest)
        {
            // trait2a:
            // When overhealed, deal 6 Shadow Damage to a random Monster and gain 1 Regeneration. (6 times/round)
            // int amountOverhealed = __result - __instance.GetHpLeftForMax();
            if (amountOverhealed <= 0) { return; }
            __instance.SetAura(__instance, GetAuraCurseData("regeneration"), 1, useCharacterMods: true);
            Character randomNPC = GetRandomCharacter(MatchManager.Instance.GetTeamNPC());
            if (IsLivingNPC(randomNPC))
            {
                int damage = __instance.DamageWithCharacterBonus(6, Enums.DamageType.Shadow, Enums.CardClass.Item);
                randomNPC?.IndirectDamage(Enums.DamageType.Shadow, damage, sourceCharacterId: __instance.Id, sourceCharacterName: __instance.SourceName);
            }

            IncrementTraitActivations(traitOfInterest, useRound: true);
        }
        public static void HandleEbonGuardTrait2b(ref Character __instance, int amountOverhealed)
        {
            // trait 2b:  
            // Block on you cannot be Purged. 
            // When overhealed, gain Block equal to that amount. - Does not gain bonuses -
            // int amountOverhealed = __result - __instance.GetHpLeftForMax();
            if (amountOverhealed <= 0) { return; }
            __instance.SetAura(__instance, GetAuraCurseData("block"), amountOverhealed, useCharacterMods: false);
        }

        public static void HandleOverhealTraits(ref Character hero, int heal, string function)
        {
            if (AtOManager.Instance.TeamHaveTrait(trait4b) && IsLivingHero(hero))
            {
                LogDebug($"{function} - trait4b overheal - adding shield");
                int amountOverhealed = heal - hero.GetHpLeftForMax();
                if (amountOverhealed <= 0) { return; }
                hero.SetAura(null, GetAuraCurseData("shield"), amountOverhealed, useCharacterMods: false);
            }
        }
    }
}