using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Obeliskial_Content;
using UnityEngine;
using static Graendor.CustomFunctions;
using static Graendor.Plugin;
using static Graendor.DescriptionFunctions;
using static Graendor.CharacterFunctions;
using System.Text;
using TMPro;
using Obeliskial_Essentials;
using System.Data.Common;

namespace Graendor
{
    [HarmonyPatch]
    internal class Traits
    {
        // list of your trait IDs

        public static string[] simpleTraitList = ["trait0", "trait1a", "trait1b", "trait2a", "trait2b", "trait3a", "trait3b", "trait4a", "trait4b"];

        public static string[] myTraitList = simpleTraitList.Select(trait => subclassname.ToLower() + trait).ToArray(); // Needs testing

        public static string trait0 = myTraitList[0];
        // static string trait1b = myTraitList[1];
        public static string trait2a = myTraitList[3];
        public static string trait2b = myTraitList[4];
        public static string trait4a = myTraitList[7];
        public static string trait4b = myTraitList[8];

        // public static int infiniteProctection = 0;
        // public static int bleedInfiniteProtection = 0;
        public static bool isDamagePreviewActive = false;

        public static bool isCalculateDamageActive = false;
        public static int infiniteProctection = 0;

        public static string debugBase = "Binbin - Testing " + heroName + " ";


        public static void DoCustomTrait(string _trait, ref Trait __instance)
        {
            // get info you may need
            Enums.EventActivation _theEvent = Traverse.Create(__instance).Field("theEvent").GetValue<Enums.EventActivation>();
            Character _character = Traverse.Create(__instance).Field("character").GetValue<Character>();
            Character _target = Traverse.Create(__instance).Field("target").GetValue<Character>();
            int _auxInt = Traverse.Create(__instance).Field("auxInt").GetValue<int>();
            string _auxString = Traverse.Create(__instance).Field("auxString").GetValue<string>();
            CardData _castedCard = Traverse.Create(__instance).Field("castedCard").GetValue<CardData>();
            Traverse.Create(__instance).Field("character").SetValue(_character);
            Traverse.Create(__instance).Field("target").SetValue(_target);
            Traverse.Create(__instance).Field("theEvent").SetValue(_theEvent);
            Traverse.Create(__instance).Field("auxInt").SetValue(_auxInt);
            Traverse.Create(__instance).Field("auxString").SetValue(_auxString);
            Traverse.Create(__instance).Field("castedCard").SetValue(_castedCard);
            TraitData traitData = Globals.Instance.GetTraitData(_trait);
            List<CardData> cardDataList = [];
            List<string> heroHand = MatchManager.Instance.GetHeroHand(_character.HeroIndex);
            Hero[] teamHero = MatchManager.Instance.GetTeamHero();
            NPC[] teamNpc = MatchManager.Instance.GetTeamNPC();

            if (!IsLivingHero(_character))
            {
                return;
            }
            string traitName = traitData.TraitName;
            string traitId = _trait;


            if (_trait == trait0)
            {
                // trait0:
                LogDebug($"Handling Trait {traitId}: {traitName}");
                _character.SetAuraTrait(_character, "evade", 1);
            }


            else if (_trait == trait2a)
            {
                // trait2a
                if (CanIncrementTraitActivations(traitId) && _castedCard.HasCardType(Enums.CardType.Defense))// && MatchManager.Instance.energyJustWastedByHero > 0)
                {
                    LogDebug($"Handling Trait {traitId}: {traitName}");
                    // _character?.ModifyEnergy(1);
                    // DrawCards(1);
                    IncrementTraitActivations(traitId);
                }
            }



            else if (_trait == trait2b)
            {
                // trait2b:
                LogDebug($"Handling Trait {traitId}: {traitName}");

            }

            else if (_trait == trait4a)
            {
                // trait 4a;

                LogDebug($"Handling Trait {traitId}: {traitName}");
            }

            else if (_trait == trait4b)
            {
                // trait 4b:
                LogDebug($"Handling Trait {traitId}: {traitName}");
            }

        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Trait), "DoTrait")]
        public static bool DoTrait(Enums.EventActivation _theEvent, string _trait, Character _character, Character _target, int _auxInt, string _auxString, CardData _castedCard, ref Trait __instance)
        {
            if ((UnityEngine.Object)MatchManager.Instance == (UnityEngine.Object)null)
                return false;
            Traverse.Create(__instance).Field("character").SetValue(_character);
            Traverse.Create(__instance).Field("target").SetValue(_target);
            Traverse.Create(__instance).Field("theEvent").SetValue(_theEvent);
            Traverse.Create(__instance).Field("auxInt").SetValue(_auxInt);
            Traverse.Create(__instance).Field("auxString").SetValue(_auxString);
            Traverse.Create(__instance).Field("castedCard").SetValue(_castedCard);
            if (Content.medsCustomTraitsSource.Contains(_trait) && myTraitList.Contains(_trait))
            {
                DoCustomTrait(_trait, ref __instance);
                return false;
            }
            return true;
        }



        [HarmonyPostfix]
        [HarmonyPatch(typeof(AtOManager), "GlobalAuraCurseModificationByTraitsAndItems")]
        // [HarmonyPriority(Priority.Last)]
        public static void GlobalAuraCurseModificationByTraitsAndItemsPostfix(ref AtOManager __instance, ref AuraCurseData __result, string _type, string _acId, Character _characterCaster, Character _characterTarget)
        {
            // LogInfo($"GACM {subclassName}");

            Character characterOfInterest = _type == "set" ? _characterTarget : _characterCaster;
            string traitOfInterest;
            switch (_acId)
            {
                // trait2a:

                // trait2b:

                // trait 4a;

                // trait 4b:

                case "evasion":
                    traitOfInterest = trait2a;
                    if (IfCharacterHas(characterOfInterest, CharacterHas.Trait, traitOfInterest, AppliesTo.ThisHero))
                    {
                    }
                    break;
                case "stealth":
                    traitOfInterest = trait2b;
                    if (IfCharacterHas(characterOfInterest, CharacterHas.Trait, traitOfInterest, AppliesTo.Heroes))
                    {
                    }
                    break;
            }
        }

        // [HarmonyPrefix]
        // [HarmonyPatch(typeof(Character), "HealAuraCurse")]
        // public static void HealAuraCursePrefix(ref Character __instance, AuraCurseData AC, ref int __state)
        // {
        //     LogInfo($"HealAuraCursePrefix {subclassName}");
        //     string traitOfInterest = trait4b;
        //     if (IsLivingHero(__instance) && __instance.HaveTrait(traitOfInterest) && AC == GetAuraCurseData("stealth"))
        //     {
        //         __state = Mathf.FloorToInt(__instance.GetAuraCharges("stealth") * 0.25f);
        //         // __instance.SetAuraTrait(null, "stealth", 1);

        //     }

        // }

        // [HarmonyPostfix]
        // [HarmonyPatch(typeof(Character), "HealAuraCurse")]
        // public static void HealAuraCursePostfix(ref Character __instance, AuraCurseData AC, int __state)
        // {
        //     LogInfo($"HealAuraCursePrefix {subclassName}");
        //     string traitOfInterest = trait4b;
        //     if (IsLivingHero(__instance) && __instance.HaveTrait(traitOfInterest) && AC == GetAuraCurseData("stealth") && __state > 0)
        //     {
        //         // __state = __instance.GetAuraCharges("stealth");
        //         __instance.SetAuraTrait(null, "stealth", __state);
        //     }

        // }




        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterItem), nameof(CharacterItem.CalculateDamagePrePostForThisCharacter))]
        public static void CalculateDamagePrePostForThisCharacterPrefix()
        {
            isDamagePreviewActive = true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterItem), nameof(CharacterItem.CalculateDamagePrePostForThisCharacter))]
        public static void CalculateDamagePrePostForThisCharacterPostfix()
        {
            isDamagePreviewActive = false;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MatchManager), nameof(MatchManager.SetDamagePreview))]
        public static void SetDamagePreviewPrefix()
        {
            isDamagePreviewActive = true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MatchManager), nameof(MatchManager.SetDamagePreview))]
        public static void SetDamagePreviewPostfix()
        {
            isDamagePreviewActive = false;
        }

        // [HarmonyPostfix]
        // [HarmonyPatch(typeof(Character), nameof(Character.SetEvent))]
        // public static void SetEventPostfix(
        //     Enums.EventActivation theEvent,
        //     Character target = null,
        //     int auxInt = 0,
        //     string auxString = "")
        // {
        //     if (theEvent == Enums.EventActivation.BeginTurnCardsDealt && AtOManager.Instance.TeamHaveTrait(trait2b))
        //     {
        //         string cardToPlay = "tacticianexpectedprophecy";
        //         PlayCardForFree(cardToPlay);
        //     }

        // }

        // [HarmonyPostfix]
        // [HarmonyPatch(typeof(Character), nameof(Character.GetTraitAuraCurseModifiers))]
        // public static void GetTraitAuraCurseModifiersPostfix(ref Character __instance, ref Dictionary<string, int> __result)
        // {
        //     // trait2a:
        //     // Block charges applied +1 for every 3 Dark on you.
        //     // trait4b Shield of Nazarick increases Block charges for every 2 Dark on you.

        //     string traitOfInterest = trait2a;
        //     if (IsLivingHero(__instance) && __instance.HaveTrait(traitOfInterest))
        //     {
        //         LogDebug($"Handling Trait {traitOfInterest}");
        //         int nDark = __instance.EffectCharges("dark");
        //         int bonusBlockCharges = nDark / (__instance.HaveTrait(trait4b) ? 2 : 3);

        //         if (bonusBlockCharges != 0) { __result["block"] = bonusBlockCharges; }
        //     }

        // }





    }
}

