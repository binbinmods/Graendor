using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Obeliskial_Content;
using Obeliskial_Essentials;
using System.IO;
using static UnityEngine.Mathf;
using UnityEngine.TextCore.LowLevel;
using static Graendor.Plugin;
using System.Collections.ObjectModel;
using BepInEx;
using UnityEngine;

namespace Graendor
{
    public class CustomFunctions
    {
        /// <summary>
        /// This is just used to help find the debugging
        /// </summary>
        // public static string debugBase = "<RenameThis>";

        /// <summary>
        /// This is used as the base when naming perks in a perk mod. Ignore if you aren't making a perk mod/using this syntax.
        /// </summary>
        public static string perkBase = $"{subclassname}";
        // public static string itemStem = $"{subclassname}";

        /// <summary>
        /// Shortcut for Plugin.Log.LogDebug(debugBase). debugBase set in CustomFunctions
        /// </summary>
        /// <param name="s">String to be output to debug log</param>
        public static void PLog(string s)
        {
            Plugin.Log.LogDebug(debugBase + s);
        }

        /// <summary>
        /// Indirect healing for Traits (Ottis's Shielder, Malukah's Voodoo etc)
        /// </summary>
        /// <param name="_character">Source of Healing</param>
        /// <param name="_target">Target to Heal</param>
        /// <param name="healAmount">Amount to Heal</param>
        /// <param name="traitName">Trait it is attributed to</param>
        public static void TraitHeal(ref Character _character, Character _target, int healAmount, string traitName)
        {
            // Used to have a ref for _target. Need to make sure that it works without the ref
            int _hp = healAmount;
            if (_target.GetHpLeftForMax() < healAmount)
                _hp = _target.GetHpLeftForMax();
            if (_hp <= 0)
                return;
            _target.ModifyHp(_hp);
            CastResolutionForCombatText _cast = new CastResolutionForCombatText();
            _cast.heal = _hp;
            if (_target.HeroItem != null)
            {
                _target.HeroItem.ScrollCombatTextDamageNew(_cast);
                EffectsManager.Instance.PlayEffectAC("healimpactsmall", true, _target.HeroItem.CharImageT, false);
            }
            else
            {
                _target.NPCItem.ScrollCombatTextDamageNew(_cast);
                EffectsManager.Instance.PlayEffectAC("healimpactsmall", true, _target.NPCItem.CharImageT, false);
            }
            _target.SetEvent(Enums.EventActivation.Healed);
            _character.SetEvent(Enums.EventActivation.Heal, _target);
            _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_" + traitName), Enums.CombatScrollEffectType.Trait);
        }

        /// <summary>
        /// TraitHeal but for Heroes (needed if ref is required)
        /// </summary>
        /// <param name="_character"></param>
        /// <param name="_target"></param>
        /// <param name="healAmount"></param>
        /// <param name="traitName"></param>
        public static void TraitHealHero(ref Character _character, ref Hero _target, int healAmount, string traitName)
        {
            if (_target == null || !_target.IsHero || !_target.Alive)
            {
                return;
            }

            int _hp = healAmount;
            if (_target.GetHpLeftForMax() < healAmount)
                _hp = _target.GetHpLeftForMax();
            if (_hp <= 0)
                return;
            _target.ModifyHp(_hp);
            CastResolutionForCombatText _cast = new CastResolutionForCombatText();
            _cast.heal = _hp;
            if (_target.HeroItem != null)
            {
                _target.HeroItem.ScrollCombatTextDamageNew(_cast);
                EffectsManager.Instance.PlayEffectAC("healimpactsmall", true, _target.HeroItem.CharImageT, false);
            }
            else
            {
                _target.NPCItem.ScrollCombatTextDamageNew(_cast);
                EffectsManager.Instance.PlayEffectAC("healimpactsmall", true, _target.NPCItem.CharImageT, false);
            }
            _target.SetEvent(Enums.EventActivation.Healed);
            _character.SetEvent(Enums.EventActivation.Heal, _target);
            _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_" + traitName), Enums.CombatScrollEffectType.Trait);
        }

        /// <summary>
        /// Grants AuraCurse to a character whenever they gain a different AuraCurse. I.e. When you gain Vitality, gain 2x Regen.
        /// </summary>
        /// <param name="ACGained">AuraCurse that triggers this</param>
        /// <param name="targetAC">AuraCurse that you are looking for</param>
        /// <param name="ACtoApply">AuraCurse that will be applied</param>
        /// <param name="nGained"> charges of AC that was gained by the target</param>
        /// <param name="nToApply"> charges of ACtoApply to apply</param>
        /// <param name="multiplier"> multiplier for nToApply </param>
        /// <param name="_character"> Character that will gain the AC</param>
        /// <param name="traitName">Trait this is attributed to< /param>
        public static void WhenYouGainXGainY(string ACGained, string targetAC, string ACtoApply, int nGained, int nToApply, float multiplier, ref Character _character, string traitName)
        {
            // Grants a multiplier or bonus charged amount of a second auraCurse given a first auraCurse

            // Makes sure it is a valid target (a living hero)
            if (MatchManager.Instance == null && ACGained == null && !IsLivingHero(_character))
                return;

            // Prevents infinite loop
            if (targetAC == ACtoApply)
                return;

            if (ACGained == targetAC)
            {
                int toApply = RoundToInt((nGained + nToApply) * multiplier);
                _character.SetAuraTrait(_character, ACtoApply, toApply);
                _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_" + traitName), Enums.CombatScrollEffectType.Trait);
            }

        }

        public static void WhenYouPlayXGainY(Enums.CardType desiredCardType, string desiredAuraCurse, int n_charges, CardData castedCard, ref Character _character, string traitName)
        {
            // Grants n_charges of desiredAuraCurse to self when you play a desired cardtype
            //Plugin.Log.LogDebug("WhenYouPlayXGainY Debug Start");
            if (MatchManager.Instance != null && castedCard != null && _character.HeroData != null)
            {
                //Plugin.Log.LogDebug("WhenYouPlayXGainY inside conditions 1");
                if (castedCard.GetCardTypes().Contains(desiredCardType))
                {
                    //Plugin.Log.LogDebug("WhenYouPlayXGainY inside conditions 2");

                    _character.SetAuraTrait(_character, desiredAuraCurse, n_charges);
                    _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_" + traitName), Enums.CombatScrollEffectType.Trait);
                }
            }
        }

        /// <summary>
        /// Function to reduce the cost of all cards of cardType by 1 for ever nCharges of an AuraCurse on them. Works like Zek's Dark Feast.
        /// </summary>
        /// <param name="cardType">Card type to reduce</param>
        /// <param name="auraCurseName">AuraCurse that determines the number of charges</param>
        /// <param name="nCharges">The number of charges needed to reduce the cost by 1</param>
        /// <param name="_character">Character</param>
        /// <param name="heroHand"></param>
        /// <param name="cardDataList">Cards in hand</param>
        /// <param name="traitName">Name of the trait that this is attributable to</param>
        /// <param name="applyToAllCards">Flag to change it from applying only to one card type to applying to all card types </param>
        public static void ReduceCostByStacks(Enums.CardType cardType, string auraCurseName, int nCharges, ref Character _character, ref List<string> heroHand, ref List<CardData> cardDataList, string traitName, bool applyToAllCards)
        {
            // Reduces the cost of all cards of cardType by 1 for every n_charges of the auraCurse
            if (!((UnityEngine.Object)_character.HeroData != (UnityEngine.Object)null))
                return;
            int num = FloorToInt((float)(_character.EffectCharges(auraCurseName) / nCharges));
            if (num <= 0)
                return;
            for (int index = 0; index < heroHand.Count; ++index)
            {
                CardData cardData = MatchManager.Instance.GetCardData(heroHand[index]);
                if ((cardData.GetCardFinalCost() > 0) && (cardData.GetCardTypes().Contains(cardType) || applyToAllCards)) //previous .Contains(Enums.CardType.Attack)
                    cardDataList.Add(cardData);
            }
            for (int index = 0; index < cardDataList.Count; ++index)
            {
                cardDataList[index].EnergyReductionTemporal += num;
                MatchManager.Instance.UpdateHandCards();
                CardItem fromTableByIndex = MatchManager.Instance.GetCardFromTableByIndex(cardDataList[index].InternalId);
                fromTableByIndex.PlayDissolveParticle();
                fromTableByIndex.ShowEnergyModification(-num);
                _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_" + traitName), Enums.CombatScrollEffectType.Trait);
                MatchManager.Instance.CreateLogCardModification(cardDataList[index].InternalId, MatchManager.Instance.GetHero(_character.HeroIndex));
            }
        }

        /// <summary>
        /// Reduces the cost of a card by nCharges until end of turn.
        /// </summary>
        /// <param name="cardType">Card Type To Reduce</param>
        /// <param name="amountToReduce">Cost reduction</param>
        /// <param name="_character">character to reduce cards for</param>
        /// <param name="heroHand">character's hand</param>
        /// <param name="cardDataList">cards in the character's hand</param>
        /// <param name="traitName">name of the trait used in the combat log (i.e. "Defense Mastery")</param>
        public static void ReduceCardTypeCostUntilDiscarded(Enums.CardType cardType, int amountToReduce, ref Character _character, ref List<string> heroHand, ref List<CardData> cardDataList, string traitName)
        {
            if (!(_character.HeroData != null))
                return;
            int num = amountToReduce;
            if (num <= 0)
                return;
            // List<string> heroHand = MatchManager.Instance.GetHeroHand(this.character.HeroIndex);
            // List<CardData> cardDataList = new List<CardData>();
            for (int index = 0; index < heroHand.Count; ++index)
            {
                CardData cardData = MatchManager.Instance.GetCardData(heroHand[index]);
                if (cardData != null && cardData.GetCardFinalCost() > 0 && cardData.HasCardType(cardType))
                    cardDataList.Add(cardData);
            }
            for (int index = 0; index < cardDataList.Count; ++index)
            {
                CardData cardData = cardDataList[index];
                if (cardData != null)
                {
                    cardData.EnergyReductionTemporal += num;
                    MatchManager.Instance.UpdateHandCards();
                    CardItem fromTableByIndex = MatchManager.Instance.GetCardFromTableByIndex(cardData.InternalId);
                    fromTableByIndex.PlayDissolveParticle();
                    fromTableByIndex.ShowEnergyModification(-num);
                    _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_" + traitName), Enums.CombatScrollEffectType.Trait);
                    MatchManager.Instance.CreateLogCardModification(cardData.InternalId, MatchManager.Instance.GetHero(_character.HeroIndex));
                }
            }
        }

        /// <summary>
        /// Adds an immunity to a character.
        /// </summary>
        /// <param name="immunity">AuraCurse to be immmune to</param>
        /// <param name="_character">Character to gain the immmunity</param>
        public static void AddImmunityToHero(string immunity, ref Hero _character)
        {
            if (_character == null)
                return;
            if (!_character.AuracurseImmune.Contains(immunity))
                _character.AuracurseImmune.Add(immunity);
        }

        public static void IncreaseChargesByStacks(string auraCurseToModify, float stacks_per_bonus, string auraCurseDependent, ref Character _character)
        {
            // increases the amount of ACtoModify that by. 
            // For instance if you want to increase the amount of burn you apply by 1 per 10 stacks of spark, then IncreaseChargesByStacks("burn",10,"spark",..)
            // Currently does not output anything to the combat log, because I don't know if it should
            int n_stacks = _character.GetAuraCharges(auraCurseDependent);
            int toIncrease = FloorToInt(n_stacks / stacks_per_bonus);
            _character.ModifyAuraCurseQuantity(auraCurseToModify, toIncrease);
        }

        /// <summary>
        /// Gets the AuraCurseData. Syntatic sugar for Globals.Instance.GetAuraCurseData(ac) since its easily forgotten.
        /// </summary>
        /// <param name="auraCurse">AuraCurse you are looking for</param>
        /// <returns></returns>
        public static AuraCurseData GetAuraCurseData(string auraCurse)
        {
            return Globals.Instance.GetAuraCurseData(auraCurse);
        }

        /// <summary>
        /// Formats the text that will appear when you have a certain number of charges remaining.
        /// </summary>
        /// <param name="currentCharges"></param>
        /// <param name="chargesTotal"></param>
        /// <returns>A fraction A/B</returns>
        public static string TextChargesLeft(int currentCharges, int chargesTotal)
        {
            int cCharges = currentCharges;
            int cTotal = chargesTotal;
            return "<br><color=#FFF>" + cCharges.ToString() + "/" + cTotal.ToString() + "</color>";
        }

        /// <summary>
        /// A Duality trait. Includes everything needed for the duality, no need to do anything else.
        /// </summary>
        /// <param name="_character">Character casting the card</param>
        /// <param name="_castedCard">Card that was class</param>
        /// <param name="class1">Card Class that could be reduced</param>
        /// <param name="class2">Card Class that could be reduced</param>
        /// <param name="traitId">Trait this is attributable to</param>
        public static void Duality(ref Character _character, ref CardData _castedCard, Enums.CardClass class1, Enums.CardClass class2, string traitId, int bonusActivations = 0)
        {
            if (!(MatchManager.Instance != null) || !(_castedCard != null))
                return;
            TraitData traitData = Globals.Instance.GetTraitData(traitId);
            if (MatchManager.Instance.activatedTraits != null && MatchManager.Instance.activatedTraits.ContainsKey(traitId) && MatchManager.Instance.activatedTraits[traitId] > (traitData.TimesPerTurn - 1 + bonusActivations))
                return;
            for (int index1 = 0; index1 < 2; ++index1)
            {
                Enums.CardClass cardClass1;
                Enums.CardClass cardClass2;
                if (index1 == 0)
                {
                    cardClass1 = class1;
                    cardClass2 = class2;
                }
                else
                {
                    cardClass1 = class2;
                    cardClass2 = class1;
                }
                if (_castedCard.CardClass == cardClass1)
                {
                    if (MatchManager.Instance.CountHeroHand() == 0 || !(_character.HeroData != null))
                        break;
                    List<CardData> cardDataList = new List<CardData>();
                    List<string> heroHand = MatchManager.Instance.GetHeroHand(_character.HeroIndex);
                    int num1 = 0;
                    for (int index2 = 0; index2 < heroHand.Count; ++index2)
                    {
                        CardData cardData = MatchManager.Instance.GetCardData(heroHand[index2]);
                        if (cardData != null && cardData.CardClass == cardClass2 && _character.GetCardFinalCost(cardData) > num1)
                            num1 = _character.GetCardFinalCost(cardData);
                    }
                    if (num1 <= 0)
                        break;
                    for (int index3 = 0; index3 < heroHand.Count; ++index3)
                    {
                        CardData cardData = MatchManager.Instance.GetCardData(heroHand[index3]);
                        if (cardData != null && cardData.CardClass == cardClass2 && _character.GetCardFinalCost(cardData) >= num1)
                            cardDataList.Add(cardData);
                    }
                    if (cardDataList.Count <= 0)
                        break;
                    CardData cardData1 = cardDataList.Count != 1 ? cardDataList[MatchManager.Instance.GetRandomIntRange(0, cardDataList.Count, "trait")] : cardDataList[0];
                    if (!(cardData1 != null))
                        break;
                    if (!MatchManager.Instance.activatedTraits.ContainsKey(traitId))
                        MatchManager.Instance.activatedTraits.Add(traitId, 1);
                    else
                        ++MatchManager.Instance.activatedTraits[traitId];
                    MatchManager.Instance.SetTraitInfoText();
                    int num2 = 1;
                    cardData1.EnergyReductionTemporal += num2;
                    MatchManager.Instance.GetCardFromTableByIndex(cardData1.InternalId).ShowEnergyModification(-num2);
                    MatchManager.Instance.UpdateHandCards();
                    _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_" + traitData.TraitName) + TextChargesLeft(MatchManager.Instance.activatedTraits[traitId], traitData.TimesPerTurn + bonusActivations), Enums.CombatScrollEffectType.Trait);

                    MatchManager.Instance.CreateLogCardModification(cardData1.InternalId, MatchManager.Instance.GetHero(_character.HeroIndex));
                    break;
                }
            }
        }

        /// <summary>
        /// For the rest of combat, reduces the cost of a card type when you play a different card type.
        /// </summary>
        /// <param name="_character">Character casting the card</param>
        /// <param name="_castedCard">Card that was cast</param>
        /// <param name="reduceThis">Card type to rduce</param>
        /// <param name="whenYouPlayThis"> Card type to trigger the effect</param>
        /// <param name="amountToReduce"> Amount of energy reduction per time this triggers</param>
        /// <param name="_trait"> Trait this is attributed to</param>
        public static void PermanentyReduceXWhenYouPlayY(ref Character _character, ref CardData _castedCard, Enums.CardType reduceThis, Enums.CardType whenYouPlayThis, int amountToReduce, string _trait)
        {
            if (!(MatchManager.Instance != null) || !(_castedCard != null))
                return;
            TraitData traitData = Globals.Instance.GetTraitData(_trait);
            if (MatchManager.Instance.activatedTraits != null && MatchManager.Instance.activatedTraits.ContainsKey(_trait) && MatchManager.Instance.activatedTraits[_trait] > traitData.TimesPerTurn - 1)
                return;

            if (!_castedCard.GetCardTypes().Contains(whenYouPlayThis))
                return;

            if (MatchManager.Instance.CountHeroHand() == 0 || !(_character.HeroData != null))
                return;


            List<CardData> cardDataList = new List<CardData>();
            List<string> heroHand = MatchManager.Instance.GetHeroHand(_character.HeroIndex);

            if (reduceThis == Enums.CardType.None)
            {
                for (int handIndex = 0; handIndex < heroHand.Count; ++handIndex)
                {
                    CardData cardData = MatchManager.Instance.GetCardData(heroHand[handIndex]);
                    if (cardData != null)
                        cardDataList.Add(cardData);
                }
            }
            else
            {
                for (int handIndex = 0; handIndex < heroHand.Count; ++handIndex)
                {
                    CardData cardData = MatchManager.Instance.GetCardData(heroHand[handIndex]);
                    if (cardData != null && cardData.GetCardTypes().Contains(reduceThis))
                        cardDataList.Add(cardData);
                }
            }

            if (!MatchManager.Instance.activatedTraits.ContainsKey(_trait))
                MatchManager.Instance.activatedTraits.Add(_trait, 1);
            else
                ++MatchManager.Instance.activatedTraits[_trait];

            CardData selectedCard = cardDataList[MatchManager.Instance.GetRandomIntRange(0, cardDataList.Count, "trait")];
            selectedCard.EnergyReductionPermanent += amountToReduce;
            MatchManager.Instance.GetCardFromTableByIndex(selectedCard.InternalId).ShowEnergyModification(-amountToReduce);
            MatchManager.Instance.UpdateHandCards();
            _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_" + _trait) + TextChargesLeft(MatchManager.Instance.activatedTraits[_trait], traitData.TimesPerTurn), Enums.CombatScrollEffectType.Trait);
            MatchManager.Instance.CreateLogCardModification(selectedCard.InternalId, MatchManager.Instance.GetHero(_character.HeroIndex));
        }


        /// <summary>
        /// Counts all stacks of a given Aura or Curse
        /// </summary>
        /// <param name="auraCurse">AuraCurse to check</param>
        /// <param name="teamHero">Optional team of heroes</param>
        /// <param name="teamNpc">Optional team of Npcs</param>
        /// <param name="includeHeroes">Optional flag to turn off counting heroes</param>
        /// <param name="includeNpcs">Optional flag to turn off counting npcs</param>
        /// <returns></returns>
        public static int CountAllStacks(string auraCurse, Hero[] teamHero = null, NPC[] teamNpc = null, bool includeHeroes = true, bool includeNpcs = true)
        {
            if (MatchManager.Instance == null)
                return 0;

            // Assigns teamHero and teamNpc if null
            teamHero ??= MatchManager.Instance.GetTeamHero();
            teamNpc ??= MatchManager.Instance.GetTeamNPC();

            int stacks = 0;
            if (includeHeroes)
            {
                for (int index = 0; index < teamHero.Length; ++index)
                {
                    if (IsLivingHero(teamHero[index]))
                    {
                        stacks += teamHero[index].GetAuraCharges(auraCurse);
                    }
                }
            }
            if (includeNpcs)
            {
                for (int index = 0; index < teamNpc.Length; ++index)
                {
                    if (IsLivingNPC(teamNpc[index]))
                    {
                        stacks += teamNpc[index].GetAuraCharges(auraCurse);
                    }
                }
            }
            return stacks;
        }

        /// <summary>
        /// Deals indirect damage to all NPCs
        /// </summary>
        /// <param name="damageType">Damage type to deal</param>
        /// <param name="amount">Amount of damage to deal</param>
        public static void DealIndirectDamageToAllMonsters(Enums.DamageType damageType, int amount)
        {
            Plugin.Log.LogDebug(debugBase + "Dealing Indirect Damage");
            if (MatchManager.Instance == null)
                return;
            NPC[] teamNpc = MatchManager.Instance.GetTeamNPC();
            for (int index = 0; index < teamNpc.Length; ++index)
            {
                NPC npc = teamNpc[index];
                if (IsLivingNPC(npc))
                {
                    npc.IndirectDamage(damageType, amount);
                }
            }
        }

        /// <summary>
        /// Creates a list of all valid characters
        /// </summary>
        /// <param name="array">Array to get the hero from</param>
        /// <returns>The random character</returns>

        public static List<int> GetValidCharacters(Character[] characters)
        {
            List<int> output_list = [];
            for (int index = 0; index < characters.Length; ++index)
            {
                Character character = characters[index];
                if (character.Alive && character != null)
                {
                    output_list.Add(index);
                }
            }
            return output_list;
        }
        /// <summary>
        /// Gets the front character from a character array (either heroes or NPCs)
        /// </summary>
        /// <param name="array">Array to get the hero from</param>
        /// <returns>The random character</returns>

        public static Character GetFrontCharacter(Character[] characters)
        {
            List<int> validCharacters = GetValidCharacters(characters);
            int frontIndex = validCharacters.First(); //Might throw error if no valid characters, but that shouldn't be possible
            Character frontChar = characters[frontIndex];
            return frontChar;
        }

        /// <summary>
        /// Gets the back character from a character array (either heroes or NPCs)
        /// </summary>
        /// <param name="array">Array to get the hero from</param>
        /// <returns>The random character</returns>

        public static Character GetBackCharacter(Character[] characters)
        {
            List<int> validCharacters = GetValidCharacters(characters);
            int backIndex = validCharacters.Last(); //Might throw error if no valid characters, but that shouldn't be possible
            Character backChar = characters[backIndex];
            return backChar;
        }

        /// <summary>
        /// Gets a random character from a character array (either heroes or NPCs)
        /// </summary>
        /// <param name="array">Array to get the hero from</param>
        /// <returns>The random character</returns>
        public static Character GetRandomCharacter(Character[] array)
        {
            if (array == null)
            {
                Plugin.Log.LogDebug(debugBase + "Null Array");

            }
            List<Character> validCharacters = [];
            for (int index = 0; index < array.Length; index++)
            {
                if (array[index] == null)
                {
                    Plugin.Log.LogDebug(debugBase + "Null index");
                    continue;
                }
                Character _character = array[index];
                if (_character.Alive && _character != null)
                {
                    validCharacters.Add(_character);
                }
            }
            if (validCharacters.Count == 0)
                return null;

            int i = MatchManager.Instance.GetRandomIntRange(0, validCharacters.Count);

            if (i < validCharacters.Count)
                return validCharacters[i];
            if (validCharacters[i] == null)
                return null;
            else
                return validCharacters[0];
        }

        /// <summary>
        /// Checks to see if the character is a living hero
        /// </summary>
        /// <param name="_character">Character to check</param>
        /// <returns></returns>
        public static bool IsLivingHero(Character _character)
        {
            return _character != null && _character.Alive && _character.IsHero;
        }

        /// <summary>
        /// Checks to see if the character is a living npc.
        /// </summary>
        /// <param name="_character">Character to check</param>
        /// <returns></returns>
        public static bool IsLivingNPC(Character _character)
        {
            return _character != null && _character.Alive && !_character.IsHero;
        }

        /// <summary>
        /// Defines what perks/traits can apply to. Used for GlobalAuraCurseModification mostly.
        /// </summary>
        public enum AppliesTo
        {
            None,
            Global,
            Monsters,
            Heroes,
            ThisHero
        }

        /// <summary>
        /// Defines the modification type. Used for GlobalAuraCurseModification mostly.
        /// </summary>
        public enum CharacterHas
        {
            Perk,
            Item,
            Trait,
            Enchantment
        }

        /// <summary>
        /// DEPRECATED Checks to see if a character has a perk for setting in AtOManager.GlobalAuraCurseModificationByTraitsAndItems
        /// </summary>
        /// <param name="perkName">Perk to check</param>
        /// <param name="flag">Checks if it applies to heroes, monsters, or globally</param>
        /// <param name="__instance">AtO instance</param>
        /// <param name="_characterTarget">The CharacterTarget</param>
        /// <returns>true if the character has the perk </returns>
        public static bool CharacterHasPerkForSet(string perkName, bool flag, AtOManager __instance, Character _characterTarget)
        {
            return flag && _characterTarget != null && __instance.CharacterHavePerk(_characterTarget.SubclassName, perkBase + perkName);
        }

        /// <summary>
        /// DEPRECATED Checks to see if a character has a perk for consuming in AtOManager.GlobalAuraCurseModificationByTraitsAndItems
        /// </summary>
        /// <param name="perkName">Perk to check</param>
        /// <param name="flag">Checks if it applies to heroes, monsters, or globally</param>
        /// <param name="__instance">AtO instance</param>
        /// <param name="_characterTarget">The CharacterTarget</param>
        /// <returns>true if the character has the perk </returns>
        public static bool CharacterHasPerkForConsume(string perkName, bool flag, AtOManager __instance, Character _characterCaster)
        {
            return flag && _characterCaster != null && __instance.CharacterHavePerk(_characterCaster.SubclassName, perkBase + perkName);
        }


        /// <summary>
        /// Checks to see if your team has a perk.
        /// </summary>
        /// <param name="perkName">Perk to check</param>
        /// <returns>true if the team has the perk</returns>
        public static bool TeamHasPerk(string perkName)
        {
            if (AtOManager.Instance == null)
                return false;
            return AtOManager.Instance.TeamHavePerk(perkBase + perkName) || AtOManager.Instance.TeamHavePerk(perkName);
        }


        /// <summary>
        /// Checks to see if your team has an item/trait/perk in the global aura curse modification function.
        /// </summary>
        /// <param name="characterOfInterest">Character we are checking</param>
        /// <param name="characterHas">Whether we are looking for a perk, item, or trait</param>
        /// <param name="id">Trait/Item/Perk to check</param>
        /// <param name="appliesTo">Who the Perk applies to. If unspecifid, returns Global</param>
        /// <param name="_type">Optional: the value of if the AC is being set or consumed</param>
        /// <param name="onlyThisType">Optional: Only potentially true if _type equals this type</param>
        /// <returns>true if the team has the item/trait/perk</returns>
        public static bool IfCharacterHas(Character characterOfInterest, CharacterHas characterHas, string id, AppliesTo appliesTo = AppliesTo.Global, string _type = "", string onlyThisType = "")
        {

            if (appliesTo == AppliesTo.None || characterOfInterest == null || AtOManager.Instance == null)
                return false;

            if (_type != "" && onlyThisType != _type)
                return false;

            bool correctCharacterType = (characterOfInterest.IsHero && appliesTo == AppliesTo.Heroes) || (!characterOfInterest.IsHero && appliesTo == AppliesTo.Monsters) || (appliesTo == AppliesTo.Global) || (appliesTo == AppliesTo.ThisHero && characterOfInterest.IsHero);

            bool hasX = false;
            if (appliesTo == AppliesTo.ThisHero)
            {
                switch (characterHas)
                {
                    case CharacterHas.Item:
                        hasX = AtOManager.Instance.CharacterHaveItem(characterOfInterest.SubclassName, perkBase + id) || AtOManager.Instance.CharacterHaveItem(characterOfInterest.SubclassName, id);
                        break;
                    case CharacterHas.Perk:
                        hasX = AtOManager.Instance.CharacterHavePerk(characterOfInterest.SubclassName, perkBase + id) || AtOManager.Instance.CharacterHavePerk(characterOfInterest.SubclassName, id);
                        break;
                    case CharacterHas.Trait:
                        hasX = AtOManager.Instance.CharacterHaveTrait(characterOfInterest.SubclassName, perkBase + id) || AtOManager.Instance.CharacterHaveTrait(characterOfInterest.SubclassName, id);
                        break;
                    case CharacterHas.Enchantment:
                        hasX = CharacterHaveEnchantment(characterOfInterest, id);
                        break;

                }
            }
            else
            {
                switch (characterHas)
                {
                    case CharacterHas.Item:
                        hasX = AtOManager.Instance.TeamHaveItem(perkBase + id) || AtOManager.Instance.TeamHaveItem(id);
                        break;
                    case CharacterHas.Perk:
                        hasX = AtOManager.Instance.TeamHavePerk(perkBase + id) || AtOManager.Instance.TeamHavePerk(id);
                        break;
                    case CharacterHas.Trait:
                        hasX = AtOManager.Instance.TeamHaveTrait(perkBase + id) || AtOManager.Instance.TeamHaveTrait(id);
                        break;
                    case CharacterHas.Enchantment:
                        hasX = AtOManager.Instance.TeamHaveItem(itemStem + id) ||
                               AtOManager.Instance.TeamHaveItem(itemStem + id + "a") ||
                               AtOManager.Instance.TeamHaveItem(itemStem + id + "b") ||
                               AtOManager.Instance.TeamHaveItem(id) ||
                               AtOManager.Instance.TeamHaveItem(id + "a") ||
                               AtOManager.Instance.TeamHaveItem(id + "b");
                        break;
                }
            }

            return hasX && correctCharacterType;
        }

        /// <summary>
        /// Checks to see if your team has a perk in the global aura curse modification function.
        /// </summary>
        /// <param name="characterOfInterest">Character who has perk</param>
        /// <param name="perkName">Perk to check</param>
        /// <param name="appliesTo">Who the Perk applies to. If unspecifid, returns Global</param>
        /// <param name="_type">Optional: the value of if the AC is being set or consumed</param>
        /// <param name="onlyThisType">Optional: Only potentially true if _type equals this type</param>
        /// <returns>true if the team has the perk</returns>
        public static bool TeamHasPerkGACM(Character characterOfInterest, string perkName, AppliesTo appliesTo = AppliesTo.Global, string _type = "", string onlyThisType = "")
        {

            if (appliesTo == AppliesTo.None || characterOfInterest == null || AtOManager.Instance == null)
                return false;

            if (_type != "" && onlyThisType != _type)
                return false;

            bool correctCharacterType = (characterOfInterest.IsHero && appliesTo == AppliesTo.Heroes) || (!characterOfInterest.IsHero && appliesTo == AppliesTo.Monsters) || appliesTo == AppliesTo.Global;
            bool hasPerk = AtOManager.Instance.TeamHavePerk(perkBase + perkName) || AtOManager.Instance.TeamHavePerk(perkName);

            return hasPerk && correctCharacterType;
        }


        /// <summary>
        /// Checks to see if your team has a perk in the global aura curse modification function.
        /// </summary>
        /// <param name="perkName">Perk to check</param>
        /// <param name="appliesTo">Who the Perk applies to</param>
        /// <returns>true if the team has the perk</returns>
        public static bool CharacterHasPerkGACM(Character characterOfInterest, string perkName, AppliesTo appliesTo = AppliesTo.Global)
        {
            if (appliesTo == AppliesTo.None || characterOfInterest == null || AtOManager.Instance == null)
                return false;

            // bool flag = appliesTo==AppliesTo.Global ? true :false;

            bool correctCharacterType = (characterOfInterest.IsHero && appliesTo == AppliesTo.Heroes) || (!characterOfInterest.IsHero && appliesTo == AppliesTo.Monsters) || appliesTo == AppliesTo.Global;
            bool hasPerk = AtOManager.Instance.CharacterHavePerk(characterOfInterest.SubclassName, perkBase + perkName) || AtOManager.Instance.CharacterHavePerk(characterOfInterest.SubclassName, perkName);
            //disarm1b - cannot be dispelled unless specified, increases resists by 10%

            return hasPerk && correctCharacterType;
        }

        /// <summary>
        /// Checks to see if your team has a perk in the global aura curse modification function.
        /// </summary>
        /// <param name="characterOfInterest">Character who has perk</param>
        /// <param name="perkName">Perk to check</param>
        /// <param name="appliesTo">Who the Perk applies to. If unspecifid, returns Global</param>
        /// <param name="_type">Optional: the value of if the AC is being set or consumed</param>
        /// <param name="onlyThisType">Optional: Only potentially true if _type equals this type</param>
        /// <returns>true if the team has the perk</returns>
        public static bool TeamHasTraitGACM(Character characterOfInterest, string perkName, AppliesTo appliesTo = AppliesTo.Global, string _type = "", string onlyThisType = "")
        {

            if (appliesTo == AppliesTo.None || characterOfInterest == null || AtOManager.Instance == null)
                return false;

            if (_type != "" && onlyThisType != _type)
                return false;

            bool correctCharacterType = (characterOfInterest.IsHero && appliesTo == AppliesTo.Heroes) || (!characterOfInterest.IsHero && appliesTo == AppliesTo.Monsters) || appliesTo == AppliesTo.Global;
            bool hasPerk = AtOManager.Instance.TeamHaveTrait(perkBase + perkName) || AtOManager.Instance.TeamHavePerk(perkName);

            return hasPerk && correctCharacterType;
        }


        /// <summary>
        /// Checks to see if your team has a perk in the global aura curse modification function.
        /// </summary>
        /// <param name="perkName">Perk to check</param>
        /// <param name="appliesTo">Who the Perk applies to</param>
        /// <returns>true if the team has the perk</returns>
        public static bool CharacterHasTraitGACM(Character characterOfInterest, string perkName, AppliesTo appliesTo = AppliesTo.Global, string _type = "", string onlyThisType = "")
        {
            if (appliesTo == AppliesTo.None || characterOfInterest == null || AtOManager.Instance == null)
                return false;

            if (_type != "" && onlyThisType != _type)
                return false;

            bool correctCharacterType = (characterOfInterest.IsHero && appliesTo == AppliesTo.Heroes) || (!characterOfInterest.IsHero && appliesTo == AppliesTo.Monsters) || appliesTo == AppliesTo.Global;
            bool hasPerk = AtOManager.Instance.CharacterHaveTrait(characterOfInterest.SubclassName, perkBase + perkName) || AtOManager.Instance.TeamHavePerk(perkName);

            return hasPerk && correctCharacterType;
        }

        /// <summary>
        /// DEPRECATED: Checks if your team has a perk for the set function in AtOManager.GlobalAuraCurseModificationByTraitsAndItems
        /// </summary>
        /// <param name="perkName">Perk to check</param>
        /// <param name="flag">Checks if it applies to heroes, monsters, or globally</param>
        /// <param name="__instance">AtO instance</param>
        /// <param name="_characterTarget">The CharacterTarget</param>
        /// <returns>true if the team has the perk</returns>

        public static bool TeamHasPerkForSet(string perkName, bool flag, AtOManager __instance, Character _characterTarget)
        {
            return _characterTarget != null && __instance.TeamHavePerk(perkBase + perkName) && flag;
        }

        /// <summary>
        /// DEPRECATED: Checks if your team has a perk for the consume function in AtOManager.GlobalAuraCurseModificationByTraitsAndItems
        /// </summary>
        /// <param name="perkName">Perk to check</param>
        /// <param name="flag">Checks if it applies to heroes, monsters, or globally</param>
        /// <param name="__instance">AtO instance</param>
        /// <param name="_characterCaster">The CharacterCaster</param>
        /// <returns>true if the team has the perk</returns>
        public static bool TeamHasPerkForConsume(string perkName, bool flag, AtOManager __instance, Character _characterCaster)
        {
            if (__instance == null)
                return false;
            return _characterCaster != null && (__instance.TeamHavePerk(perkBase + perkName) || __instance.TeamHavePerk(perkName)) && flag;
        }

        /// <summary>
        /// Checks to see if a character has a perk.
        /// </summary>
        /// <param name="_character">Character to check</param>
        /// <param name="_perkID">String id of the perk</param>
        /// <returns></returns>
        public static bool CharacterObjectHavePerk(Character _character, string _perkID)
        {
            if (_character == null || AtOManager.Instance == null)
                return false;
            // if (_perkID.StartsWith(perkBase))
            //     AtOManager.Instance.CharacterHavePerk(_character.SubclassName, _perkID);

            return AtOManager.Instance.CharacterHavePerk(_character.SubclassName, perkBase + _perkID) || AtOManager.Instance.CharacterHavePerk(_character.SubclassName, _perkID);
        }

        /// <summary>
        /// Plays/Casts a card. Triggers all effects playing the card normally would. Card is treated as costing 0.
        /// </summary>
        /// <param name="cardToCast">string id of the card you want to cast</param>
        public static void PlayCardForFree(string cardToCast)
        {
            //Plugin.Log.LogDebug("Binbin PestilyBiohealer - trying to cast card: "+cardToCast);
            if (cardToCast == null || Globals.Instance == null)
                return;
            CardData card = Globals.Instance.GetCardData(cardToCast);

            if (card == null)
            {
                PLog("Invalid CardName");
                return;
            }

            MatchManager.Instance.StartCoroutine(MatchManager.Instance.CastCard(_automatic: true, _card: card, _energy: 0));

            //MatchManager.Instance.CastCard(_card: card);
        }

        /// <summary>
        /// Plays a sound effect.
        /// </summary>
        /// <param name="_character">Character that is the source of the sound</param>
        /// <param name="ACeffect">Name of the sound effect to play</param>

        public static void PlaySoundEffect(Character _character, string ACeffect)
        {
            if (_character.HeroItem != null)
            {
                EffectsManager.Instance.PlayEffectAC(ACeffect, true, _character.HeroItem.CharImageT, false, 0f);
            }
        }

        /// <summary>
        /// Displays the remaining charges for a trait. Might lead to errors (isn't well protected). Common to play sound effects after.
        /// </summary>
        /// <param name="_character">Character we are checking if it has the trait</param>
        /// <param name="traitData">The Trait we are checking</param>

        public static void DisplayRemainingChargesForTrait(ref Character _character, TraitData traitData)
        {
            if (_character.HeroItem != null)
            {
                _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_" + traitData.TraitName, "") + TextChargesLeft(MatchManager.Instance.activatedTraits[traitData.TraitName], traitData.TimesPerTurn), Enums.CombatScrollEffectType.Trait);
            }
        }

        /// <summary>
        /// Displays the remaining charges for a trait. Might lead to errors (isn't well protected). Common to play sound effects after.
        /// </summary>
        /// <param name="_character">Character we are checking if it has the trait</param>
        /// <param name="traitData">The Trait we are checking</param>

        public static void DisplayRemainingChargesForTraitRound(ref Character _character, TraitData traitData)
        {
            if (_character.HeroItem != null)
            {
                _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_" + traitData.TraitName, "") + TextChargesLeft(MatchManager.Instance.activatedTraitsRound[traitData.TraitName], traitData.TimesPerRound), Enums.CombatScrollEffectType.Trait);
            }
        }

        /// <summary>
        /// Places a little text scroll of the trait's name when a character's trait activates.
        /// </summary>
        /// <param name="_character"> Character the trait comes from</param>
        /// <param name="traitData"> Trait to display the name of</param>
        public static void DisplayTraitScroll(Character _character, TraitData traitData, string effectName = "", Character effectTarget = null)
        {
            _character.HeroItem?.ScrollCombatText(traitData.TraitName, Enums.CombatScrollEffectType.Trait);
            if (effectName.IsNullOrWhiteSpace())
            {
                return;
            }
            Transform sourceImage = effectTarget?.HeroItem?.CharImageT ?? effectTarget?.NPCItem?.CharImageT;
            if (sourceImage == null)
                return;
            EffectsManager.Instance.PlayEffectAC(effectName, true, sourceImage, flip: false, delay: 0f);
        }

        /// <summary>
        /// Places a little text scroll of the trait's name when a character's trait activates.
        /// </summary>
        /// <param name="_character"> Character the trait comes from</param>
        /// <param name="traitName"> Name to Display</param>

        public static void DisplayTraitScroll(ref Character _character, string traitName)
        {
            _character.HeroItem?.ScrollCombatText(Texts.Instance.GetText("traits_" + traitName, ""), Enums.CombatScrollEffectType.Trait);
        }

        /// <summary>
        /// Checks to see if a trait can be incremented. If so, it increments it.
        /// </summary>
        /// <param name="traitData">The Trait we are incrementing</param>
        public static void IncrementTraitActivations(TraitData traitData, bool useRound = false)
        {
            string traitId = traitData.Id;
            if (useRound)
            {

                if (!MatchManager.Instance.activatedTraitsRound.ContainsKey(traitId))
                    MatchManager.Instance.activatedTraitsRound.Add(traitId, 1);
                else
                    ++MatchManager.Instance.activatedTraitsRound[traitId];
            }
            else
            {

                if (!MatchManager.Instance.activatedTraits.ContainsKey(traitId))
                {
                    MatchManager.Instance.activatedTraits.Add(traitId, 1);
                }
                else
                {
                    Dictionary<string, int> activatedTraits = MatchManager.Instance.activatedTraits;
                    activatedTraits[traitId] = activatedTraits[traitId] + 1;
                }

            }
            MatchManager.Instance.SetTraitInfoText();
        }

        /// <summary>
        /// Checks to see if a trait can be incremented. If so, it increments it.
        /// </summary>
        /// <param name="traitId">The Id of the trait we are incrementing</param>
        public static void IncrementTraitActivations(string traitId, bool useRound = false)
        {
            TraitData traitData = Globals.Instance.GetTraitData(traitId);
            IncrementTraitActivations(traitData, useRound);
        }



        /// <summary>
        /// Checks to see if you can increment a Trait's activations
        /// </summary>
        /// <param name="traitData">The Trait we are checking</param>
        public static bool CanIncrementTraitActivations(TraitData traitData, int bonusActivations = 0, bool useRound = false)
        {
            LogDebug("canIncrementTraitActivations");
            if (traitData == null)
            {
                return false;
            }
            string traitId = traitData.Id;
            if ((UnityEngine.Object)MatchManager.Instance == (UnityEngine.Object)null)
            {
                return false;
            }
            if (MatchManager.Instance.activatedTraits == null)
            {
                return false;
            }
            // if(!IsLivingHero())
            // {
            //     return false;
            // }
            int activations;
            if (useRound)
            {

                activations = traitData.TimesPerRound - 1 + bonusActivations;
                if (MatchManager.Instance.activatedTraitsRound != null && MatchManager.Instance.activatedTraitsRound.ContainsKey(traitId) && MatchManager.Instance.activatedTraitsRound[traitId] > activations)
                    return false;
                return true;
            }

            activations = traitData.TimesPerTurn - 1 + bonusActivations;
            if (MatchManager.Instance.activatedTraits.ContainsKey(traitId) && MatchManager.Instance.activatedTraits[traitId] > activations)
            {
                // LogDebug("False v2");
                // LogDebug($"Activation Dict - {CollectionToString(MatchManager.Instance.activatedTraits)}");
                // LogDebug($"Activated Traits - {MatchManager.Instance.activatedTraits[traitId]}");
                return false;
            }
            if (!MatchManager.Instance.activatedTraits.ContainsKey(traitId))
            {
                MatchManager.Instance.activatedTraits.Add(traitId, 0);
            }
            return true;
        }

        /// <summary>
        /// Checks to see if you can increment a Trait's activations
        /// </summary>
        /// <param name="traitId">The id of the trait we are checking</param>
        public static bool CanIncrementTraitActivations(string traitId, int bonusActivations = 0, bool useRound = false)
        {
            TraitData traitData = Globals.Instance.GetTraitData(traitId);
            return CanIncrementTraitActivations(traitData, bonusActivations, useRound);
        }
        /// <summary>
        /// Specifies whether should apply to Auras, Curses, or Both (used when modifying AuraCurses)
        /// </summary>
        public enum IsAuraOrCurse
        {
            Aura,
            Curse,
            Both
        }

        /// <summary>
        /// Modifies Auras or Curses by a percentage. To specify "Reduce" use negative values for percentToModify
        /// </summary>
        /// <param name="percentToModify">Percentage to modify. +20 will increase all Auras/Curses by 20%. -10 will reduce all Auras/Curses by 10%.</param>
        /// <param name="isAuraOrCurse">Specifies whether you are applying to Auras, Curse, or Both</param>
        /// <param name="_characterTarget">Target to modify AuraCurses on</param>
        /// <param name="_characterCaster">Source of the modification</param>
        public static void ModifyAllAurasOrCursesByPercent(int percentToModify, IsAuraOrCurse isAuraOrCurse, Character _characterTarget, Character _characterCaster)
        {
            if (percentToModify == 0 || _characterTarget == null || !_characterTarget.Alive) { return; }
            // if (_characterCaster==null){return;} // might need this too, unsure

            int increaseAuras = 0;
            int reduceAuras = 0;
            int increaseCurses = 0;
            int reduceCurses = 0;

            if ((isAuraOrCurse == IsAuraOrCurse.Aura || isAuraOrCurse == IsAuraOrCurse.Both) && percentToModify > 0) { increaseAuras = percentToModify; }
            if ((isAuraOrCurse == IsAuraOrCurse.Aura || isAuraOrCurse == IsAuraOrCurse.Both) && percentToModify < 0) { reduceAuras = Math.Abs(percentToModify); }
            if ((isAuraOrCurse == IsAuraOrCurse.Curse || isAuraOrCurse == IsAuraOrCurse.Both) && percentToModify > 0) { increaseCurses = percentToModify; }
            if ((isAuraOrCurse == IsAuraOrCurse.Curse || isAuraOrCurse == IsAuraOrCurse.Both) && percentToModify < 0) { reduceCurses = Math.Abs(percentToModify); }

            if (_characterTarget == null || !_characterTarget.Alive) { return; }
            for (int index1 = 0; index1 < 4; ++index1)
            {
                if ((index1 != 0 || increaseAuras > 0) && (index1 != 1 || increaseCurses > 0) && (index1 != 2 || reduceAuras > 0) && (index1 != 3 || reduceCurses > 0))
                {
                    List<string> stringList = new List<string>();
                    List<int> intList = new List<int>();
                    for (int index2 = 0; index2 < _characterTarget.AuraList.Count; ++index2)
                    {
                        if (_characterTarget.AuraList[index2] != null && (UnityEngine.Object)_characterTarget.AuraList[index2].ACData != (UnityEngine.Object)null && _characterTarget.AuraList[index2].GetCharges() > 0 && !(_characterTarget.AuraList[index2].ACData.Id == "furnace"))
                        {
                            bool flag = false;
                            if ((index1 == 0 || index1 == 2) && _characterTarget.AuraList[index2].ACData.IsAura)
                                flag = true;
                            else if ((index1 == 1 || index1 == 3) && !_characterTarget.AuraList[index2].ACData.IsAura)
                                flag = true;
                            if (flag)
                            {
                                stringList.Add(_characterTarget.AuraList[index2].ACData.Id);
                                intList.Add(_characterTarget.AuraList[index2].GetCharges());
                            }
                        }
                    }
                    if (stringList.Count > 0)
                    {
                        for (int index3 = 0; index3 < stringList.Count; ++index3)
                        {
                            int num;
                            switch (index1)
                            {
                                case 0:
                                    num = Functions.FuncRoundToInt((float)((double)intList[index3] * (double)increaseAuras / 100.0));
                                    break;
                                case 1:
                                    num = Functions.FuncRoundToInt((float)((double)intList[index3] * (double)increaseCurses / 100.0));
                                    break;
                                case 2:
                                    num = intList[index3] - Functions.FuncRoundToInt((float)((double)intList[index3] * (double)reduceAuras / 100.0));
                                    break;
                                default:
                                    num = intList[index3] - Functions.FuncRoundToInt((float)((double)intList[index3] * (double)reduceCurses / 100.0));
                                    break;
                            }
                            switch (index1)
                            {
                                case 0:
                                case 1:
                                    AuraCurseData _acData = AtOManager.Instance.GlobalAuraCurseModificationByTraitsAndItems("set", stringList[index3], _characterCaster, _characterTarget);
                                    if ((UnityEngine.Object)_acData != (UnityEngine.Object)null)
                                    {
                                        int maxCharges = _acData.GetMaxCharges();
                                        if (maxCharges > -1 && intList[index3] + num > maxCharges)
                                            num = maxCharges - intList[index3];
                                        _characterTarget.SetAura(_characterCaster, _acData, num, useCharacterMods: false, canBePreventable: false);
                                        break;
                                    }
                                    break;
                                case 2:
                                case 3:
                                    if (num <= 0)
                                        num = 1;
                                    _characterTarget.ModifyAuraCurseCharges(stringList[index3], num);
                                    _characterTarget.UpdateAuraCurseFunctions();
                                    break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Applies an Aura or curse to all of something
        /// </summary>
        /// <param name="appliesTo">Who to apply the AC to</param>
        /// <param name="acToApply">aura or curse to apply</param>
        /// <param name="nToApply">amount to apply</param>
        /// <param name="sourceCharacter">who is applying the AC</param>
        /// <param name="useCharacterMods">whether or not to use the bonus modifiers for the AC</param>
        /// <param name="useCharacterMods">whether or not to use the AC can be buffered</param>
        public static void ApplyAuraCurseToAll(string acToApply, int nToApply, AppliesTo appliesTo, Character sourceCharacter = null, bool useCharacterMods = false, bool isPreventable = true)
        {
            LogInfo("ApplyAuraCurseToAll");
            if (MatchManager.Instance == null) { LogError("No MatchManager"); return; }
            if (sourceCharacter == null && useCharacterMods) { LogError("No Source Character"); return; }

            AuraCurseData acData = GetAuraCurseData(acToApply);
            if (acData == null) { LogError("Improper AuraCurse"); return; }

            Hero[] heroes = MatchManager.Instance.GetTeamHero();
            NPC[] npcs = MatchManager.Instance.GetTeamNPC();

            switch (appliesTo)
            {
                case AppliesTo.Heroes:

                    foreach (Hero hero in heroes)
                    {
                        if (IsLivingHero(hero))
                        {
                            hero.SetAura(sourceCharacter, acData, nToApply, useCharacterMods: useCharacterMods, canBePreventable: isPreventable);
                        }
                    }
                    break;
                case AppliesTo.Global:
                    foreach (Hero hero in heroes)
                    {
                        if (IsLivingHero(hero))
                        {
                            hero.SetAura(sourceCharacter, acData, nToApply, useCharacterMods: useCharacterMods, canBePreventable: isPreventable);
                        }
                    }
                    foreach (NPC npc in npcs)
                    {
                        if (IsLivingNPC(npc))
                        {
                            npc.SetAura(sourceCharacter, acData, nToApply, useCharacterMods: useCharacterMods, canBePreventable: isPreventable);
                        }
                    }

                    break;
                case AppliesTo.Monsters:
                    foreach (NPC npc in npcs)
                    {
                        if (IsLivingNPC(npc))
                        {
                            npc.SetAura(sourceCharacter, acData, nToApply, useCharacterMods: useCharacterMods, canBePreventable: isPreventable);
                        }
                    }
                    break;
            }

        }

        public static string CollectionToString(Collection<object> values)
        {
            return string.Join(",", values);
        }

        public static string CollectionToString(Dictionary<string, int> dictionary)
        {
            return "{" + string.Join(",", dictionary.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}";

        }

        public static void DrawCards(int numToDraw)
        {
            MatchManager.Instance.NewCard(numToDraw, Enums.CardFrom.Deck);
        }

        /// <summary>
        /// Gains Energy. Optional to specify if it comes from a trait or not.
        /// </summary>
        /// <param name="_character"> Character to gain energy </param>
        /// <param name="energyToGain"> Amount of energy to gain</param>
        /// <param name="traitData"> Optional: only used to specify the trait that is causing the effect for the purposes of scrolling text related to the remaining charges on a Trait.</param>
        public static void GainEnergy(Character _character, int energyToGain, TraitData traitData = null)
        {
            if (!IsLivingHero(_character))
            {
                LogDebug("GainEnergy - Invalid Character");
                return;
            }
            LogDebug("GainEnergy - Modifying Energy");

            _character.ModifyEnergy(energyToGain, true);

            if ((_character.HeroItem == null))
                return;

            LogDebug("GainEnergy - Setting Effect AC");

            EffectsManager.Instance.PlayEffectAC("energy", true, _character.HeroItem.CharImageT, false);

            if (traitData == null)
                return;

            // LogDebug("GainEnergy - Setting Combat Text");

            _character.HeroItem.ScrollCombatText(traitData.TraitName, Enums.CombatScrollEffectType.Trait);

            // LogDebug("GainEnergy - DONE");

        }

        /// <summary>
        /// Steals an aura or a curse from the target.
        /// </summary>
        /// <param name="characterStealing"> Source Character (the one stealing) </param>
        /// <param name="characterToStealFrom"> Target Character (the one being stolen from) </param>
        /// <param name="nToSteal"> Number of Auras or curses to steal</param>
        /// <param name="isAuraOrCurse"> Whether to steal an Aura or a Curse. Must be either Aura or Curse. Specifiying Both does nothing</param>
        public static void StealAuraCurses(ref Character characterStealing, ref Character characterToStealFrom, int nToSteal, IsAuraOrCurse isAuraOrCurse = IsAuraOrCurse.Aura)
        {
            if (isAuraOrCurse == IsAuraOrCurse.Both)
            {
                LogDebug("Must Specify Aura or Curse to Steal");
                return;
            }
            if (characterStealing == null || characterToStealFrom == null || !characterStealing.Alive || !characterToStealFrom.Alive)
            {
                LogDebug("Character to Steal from or Character Stealing is not a valid living character");
                return;
            }
            List<string> curseList = new List<string>();
            List<int> intList = new List<int>();
            int num = 0;
            // Character characterToStealFrom = (Character)null;
            // if (_hero != null && _hero.Alive)
            //     characterToStealFrom = (Character)_hero;
            // else if (_npc != null && _npc.Alive)
            //     characterToStealFrom = (Character)_npc;
            if (characterToStealFrom != null)
            {
                for (int index = 0; index < characterToStealFrom.AuraList.Count && num < nToSteal; ++index)
                {
                    bool charsAreNonNull = characterToStealFrom.AuraList[index] != null && (UnityEngine.Object)characterToStealFrom.AuraList[index].ACData != (UnityEngine.Object)null;
                    bool acHasCorrectType = isAuraOrCurse == IsAuraOrCurse.Aura ? characterToStealFrom.AuraList[index].ACData.IsAura : !characterToStealFrom.AuraList[index].ACData.IsAura;
                    bool acIsRemovable = characterToStealFrom.AuraList[index].ACData.Removable && characterToStealFrom.AuraList[index].GetCharges() > 0;

                    if (charsAreNonNull && acHasCorrectType && acIsRemovable)
                    {
                        curseList.Add(characterToStealFrom.AuraList[index].ACData.Id);
                        intList.Add(characterToStealFrom.AuraList[index].GetCharges());
                        ++num;
                    }
                }
            }
            if (num > 0)
            {
                characterToStealFrom.HealCursesName(curseList);
                for (int index = 0; index < curseList.Count; ++index)
                {
                    if (characterStealing != null && characterStealing.Alive)
                        characterStealing.SetAura(characterToStealFrom, Globals.Instance.GetAuraCurseData(curseList[index]), intList[index]);
                }
            }
        }



        /// <summary>
        ///  Gets the cards in the characters deck as strings
        /// </summary>
        /// <param name="character">Character to get the Deck of</param>
        /// <returns>A list of strings representing all cards in the characters deck</returns>
        public static List<string> GetDeck(Character character)
        {
            return character.Cards;
        }

        /// <summary>
        ///  Gets the cards in the characters deck as CardData objects
        /// </summary>
        /// <param name="character">Character to get the Deck of</param>
        /// <returns>A list of CardDatas representing all cards in the characters deck</returns>
        public static List<CardData> GetDeckCardData(Character character)
        {
            List<CardData> deck = [];
            foreach (string card in character.Cards)
            {
                deck.Add(Globals.Instance.GetCardData(card));
            }
            return deck;
        }

        /// <summary>
        /// Counts the auras or curses on a target character.
        /// </summary>
        /// <param name="character">Character to count</param>
        /// <param name="isAuraOrCurse">Specifies if we should count only auras, only curses, or both</param>
        /// <returns>The total count of all the auras or curses or both on a character</returns>
        public static int CountAllACOnCharacter(Character character, IsAuraOrCurse isAuraOrCurse)
        {
            int sum = 0;
            foreach (Aura aura in character.AuraList)
            {
                bool correctType = true; ;
                switch (isAuraOrCurse)
                {
                    case IsAuraOrCurse.Aura:
                        correctType = aura.ACData.IsAura;
                        break;
                    case IsAuraOrCurse.Curse:
                        correctType = !aura.ACData.IsAura;
                        break;
                    case IsAuraOrCurse.Both:
                        correctType = true;
                        break;
                }

                if (correctType)
                {
                    sum += aura.AuraCharges;
                }
            }
            return sum;
        }

        /// <summary>
        /// Reduces the cost of a card.
        /// </summary>
        /// <param name="cardData">Card to Reduce.</param>
        /// <param name="currentCharacter">character to reduce the cards for. If null, gets the current active hero.</param>
        /// <param name="amountToReduce">Amount to reduce the card's cost by</param>
        /// <param name="isPermanent">If true, makes the reduction permanent.</param>
        public static void ReduceCardCost(ref CardData cardData, Character currentCharacter = null, int amountToReduce = 1, bool isPermanent = false)
        {
            if (MatchManager.Instance == null || cardData == null)
            {
                LogError("Null MatchManager/card");
                return;
            }

            LogDebug("Reducing card Cost");
            currentCharacter ??= MatchManager.Instance.GetHeroHeroActive();

            if (currentCharacter == null)
            {
                LogError("Null Current Character");
                return;
            }

            if (isPermanent)
            {
                LogDebug("Reducing card Cost - permanent");
                cardData.EnergyReductionPermanent += amountToReduce;
            }
            else
            {
                LogDebug("Reducing card Cost - temporary");
                cardData.EnergyReductionTemporal += amountToReduce;
            }
            LogDebug("Reducing card Cost - updates");
            MatchManager.Instance.GetCardFromTableByIndex(cardData.InternalId).ShowEnergyModification(-amountToReduce);
            MatchManager.Instance.UpdateHandCards();
            // this.character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_Scholar") + TextChargesLeft(MatchManager.Instance.activatedTraits[nameof (scholar)], traitData.TimesPerTurn), Enums.CombatScrollEffectType.Trait);
            MatchManager.Instance.CreateLogCardModification(cardData.InternalId, MatchManager.Instance.GetHero(currentCharacter.HeroIndex));
            LogDebug("Reducing card Cost - END");
        }


        /// <summary>
        /// Gets the highest cost card in your hand. If multiple of the same cost, randomly chooses one.
        /// </summary>
        /// <param name="heroHand"> The hero's hand. If null, gets the current active hero's hand.</param>
        /// <param name="cardType">The cardType you are looking for. Use None to specify any card.</param>
        /// <returns>A random card with the highest cost of cardType. Returns null if card is not found.</returns>
        public static CardData GetRandomHighestCostCard(Enums.CardType cardType, List<string> heroHand = null)
        {
            if (MatchManager.Instance == null)
            {
                LogError("Null MatchManager");
                return null;
            }
            heroHand ??= MatchManager.Instance.GetHeroHand(MatchManager.Instance.GetHeroActive());

            int num1 = 0;
            List<CardData> cardDataList = new();
            for (int index = 0; index < heroHand.Count; ++index)
            {
                CardData cardData = MatchManager.Instance.GetCardData(heroHand[index]);
                if (cardData != null && (cardData.GetCardTypes().Contains(cardType) || cardType == Enums.CardType.None) && cardData.GetCardFinalCost() > num1)
                    num1 = cardData.GetCardFinalCost();
            }
            if (num1 <= 0)
                return null;
            for (int index = 0; index < heroHand.Count; ++index)
            {
                CardData cardData = MatchManager.Instance.GetCardData(heroHand[index]);
                if (cardData != null && (cardData.GetCardTypes().Contains(cardType) || cardType == Enums.CardType.None) && cardData.GetCardFinalCost() >= num1)
                    cardDataList.Add(cardData);
            }
            if (cardDataList.Count <= 0)
                return null;

            CardData cardData1 = cardDataList.Count != 1 ? cardDataList[MatchManager.Instance.GetRandomIntRange(0, cardDataList.Count)] : cardDataList[0];

            return cardData1;
        }

        /// <summary>
        /// Gets cards of a certain type from the active character's hand.
        /// </summary>
        /// <param name="cardType">The card type</param>
        /// <param name="equalOrAboveCertainCost">The cards must be greater than or equal to this amount</param>
        /// <param name="lessThanOrEqualToThisCost">The cards must be less than or equal to this amount</param>
        /// <returns></returns>
        public static List<CardData> GetCardsFromHand(Enums.CardType cardType = Enums.CardType.None, int equalOrAboveCertainCost = 0, int lessThanOrEqualToThisCost = 100)
        {
            Character character = MatchManager.Instance.GetHeroHeroActive();
            List<string> heroHand = MatchManager.Instance.GetHeroHand(character.HeroIndex);

            List<CardData> cardDataList = new List<CardData>();
            for (int index = 0; index < heroHand.Count; ++index)
            {
                CardData cardData = MatchManager.Instance.GetCardData(heroHand[index]);
                if ((UnityEngine.Object)cardData != (UnityEngine.Object)null && cardData.GetCardFinalCost() > 0 && (cardData.GetCardTypes().Contains(cardType) || cardType == Enums.CardType.None) && cardData.GetCardFinalCost() >= equalOrAboveCertainCost && cardData.GetCardFinalCost() <= lessThanOrEqualToThisCost)
                    cardDataList.Add(cardData);
            }
            return cardDataList;
        }

        /// <summary>
        /// Gets cards of a certain type from your hand.
        /// </summary>
        /// <param name="heroHand">The hand</param>
        /// <param name="cardType">The card type</param>
        /// <param name="equalOrAboveCertainCost">The cards must be greater than or equal to this amount</param>
        /// <param name="lessThanOrEqualToThisCost">The cards must be less than or equal to this amount</param>
        /// <returns></returns>
        public static List<CardData> GetCardsFromHand(List<string> heroHand, Enums.CardType cardType = Enums.CardType.None, int equalOrAboveCertainCost = 0, int lessThanOrEqualToThisCost = 100)
        {
            // List<string> heroHand = MatchManager.Instance.GetHeroHand(character.HeroIndex);

            List<CardData> cardDataList = new List<CardData>();
            for (int index = 0; index < heroHand.Count; ++index)
            {
                CardData cardData = MatchManager.Instance.GetCardData(heroHand[index]);
                if ((UnityEngine.Object)cardData != (UnityEngine.Object)null && cardData.GetCardFinalCost() > 0 && (cardData.GetCardTypes().Contains(cardType) || cardType == Enums.CardType.None) && cardData.GetCardFinalCost() >= equalOrAboveCertainCost && cardData.GetCardFinalCost() <= lessThanOrEqualToThisCost)
                    cardDataList.Add(cardData);
            }
            return cardDataList;
        }


        /// <summary>
        /// Gets the rightmost card in a hand. Syntatic Sugar for MatchManager.Instance.GetCardData(heroHand.Last());
        /// </summary>
        /// <param name="heroHand">Hand to get the card from.</param>
        /// <returns>The rightmore card</returns>
        public static CardData GetRightmostCard(List<string> heroHand)
        {
            // MatchManager.Instance.GetCardData(heroHand.Last());
            return MatchManager.Instance.GetCardData(heroHand.Last());
        }

        /// <summary>
        /// Checks to see if a character has an enchantment
        /// </summary>
        /// <param name="character">character you are checking</param>
        /// <param name="id">id of the enchantment</param>
        /// <returns></returns>
        public static bool CharacterHaveEnchantment(Character character, string id)
        {
            if (AtOManager.Instance == null)
            {
                return false;
            }

            string characterId = character.SubclassName;
            return CharacterHaveEnchantment(characterId, id);
        }

        /// <summary>
        /// Checks to see if a character has an enchantment
        /// </summary>
        /// <param name="characterId">character you are checking</param>
        /// <param name="id">id of the enchantment</param>
        /// <returns></returns>
        public static bool CharacterHaveEnchantment(string characterId, string id)
        {
            if (AtOManager.Instance == null)
            {
                return false;
            }

            return AtOManager.Instance.CharacterHaveItem(characterId, itemStem + id) ||
                    AtOManager.Instance.CharacterHaveItem(characterId, itemStem + id + "a") ||
                    AtOManager.Instance.CharacterHaveItem(characterId, itemStem + id + "b") ||
                    AtOManager.Instance.CharacterHaveItem(characterId, id) ||
                    AtOManager.Instance.CharacterHaveItem(characterId, id + "a") ||
                    AtOManager.Instance.CharacterHaveItem(characterId, id + "b");
        }


        /// <summary>
        /// Checks to see if the team has a particular enchantment
        /// </summary>
        /// <param name="id">Enchantment to check</param>
        /// <returns></returns>
        public static bool TeamHaveEnchantment(string id)
        {
            if (AtOManager.Instance == null)
            {
                return false;
            }

            return AtOManager.Instance.TeamHaveItem(itemStem + id) ||
                   AtOManager.Instance.TeamHaveItem(itemStem + id + "a") ||
                   AtOManager.Instance.TeamHaveItem(itemStem + id + "b") ||
                   AtOManager.Instance.TeamHaveItem(id) ||
                   AtOManager.Instance.TeamHaveItem(id + "a") ||
                   AtOManager.Instance.TeamHaveItem(id + "b");
        }

        public static int SafeRandomInt(int min, int max, string type = "default", string seed = "")
        {
            if (MatchManager.Instance)
            {
                return MatchManager.Instance.GetRandomIntRange(min, max, type, seed);
            }
            if (MapManager.Instance)
            {
                return MapManager.Instance.GetRandomIntRange(min, max);
            }
            // if(MapManager.Instance)
            // {
            //     return Functions.Random(min, max, seed);
            // }
            if (seed != "")
                UnityEngine.Random.InitState(Functions.GetDeterministicHashCode(seed));
            return UnityEngine.Random.Range(min, max);
        }

        /// <summary>
        /// Adds a card to the currently active hero's hand.
        /// </summary>
        /// <param name="cardId">Id of the card to add</param>
        /// <param name="randomlyUpgraded">Whether or not you want the card to be randomly upgraded.</param>
        /// <param name="vanish">Whether the card should vanish</param>
        /// <param name="costZero">Whether the card should cost 0.</param>
        public static void AddCardToHand(string cardId, bool randomlyUpgraded = true, bool vanish = true, bool costZero = true)
        {
            if (MatchManager.Instance.CountHeroHand() == 10)
            {
                LogDebug("[TRAIT EXECUTION] Broke because player at max cards");
            }
            string str = cardId;
            string cardInDictionary = cardId;
            if (randomlyUpgraded)
            {
                int randomIntRange = MatchManager.Instance.GetRandomIntRange(0, 100, "trait");
                cardInDictionary = MatchManager.Instance.CreateCardInDictionary(randomIntRange >= 45 ? (randomIntRange >= 90 ? str + "rare" : str + "b") : str + "a");

            }
            CardData cardData = MatchManager.Instance.GetCardData(cardInDictionary);
            cardData.Vanish = vanish;
            cardData.EnergyReductionToZeroPermanent = costZero;
            MatchManager.Instance.GenerateNewCard(1, cardInDictionary, false, Enums.CardPlace.Hand);
            MatchManager.Instance.CreateLogCardModification(cardData.InternalId, MatchManager.Instance.GetHeroHeroActive());
        }

        /// <summary>
        /// A Duality trait. Includes everything needed for the duality, no need to do anything else.
        /// </summary>
        /// <param name="_character">Character casting the card</param>
        /// <param name="_castedCard">Card that was class</param>
        /// <param name="class1">Card Class that could be reduced</param>
        /// <param name="class2">Card Class that could be reduced</param>
        /// <param name="traitId">Trait this is attributable to</param>
        public static void DualityCardType(ref Character _character, ref CardData _castedCard, Enums.CardType[] cardTypes1, Enums.CardType[] cardTypes2, string traitId, int bonusActivations = 0)
        {
            if (!(MatchManager.Instance != null) || !(_castedCard != null))
                return;
            TraitData traitData = Globals.Instance.GetTraitData(traitId);
            if (MatchManager.Instance.activatedTraits != null && MatchManager.Instance.activatedTraits.ContainsKey(traitId) && MatchManager.Instance.activatedTraits[traitId] > (traitData.TimesPerTurn - 1 + bonusActivations))
                return;
            for (int index1 = 0; index1 < 2; ++index1)
            {
                Enums.CardType[] types1;
                Enums.CardType[] types2;
                if (index1 == 0)
                {
                    types1 = cardTypes1;
                    types2 = cardTypes2;
                }
                else
                {
                    types1 = cardTypes2;
                    types2 = cardTypes1;
                }
                CardData castedCard = _castedCard;
                // if (_castedCard.CardClass == cardClass1)
                bool hasProperCardTypeToTrigger = types1.Any(castedCard.HasCardType);
                // bool hasCardType2 = types2.Any(castedCard.HasCardType);
                if (hasProperCardTypeToTrigger)
                {
                    if (MatchManager.Instance.CountHeroHand() == 0 || !(_character.HeroData != null))
                        break;
                    List<CardData> cardDataList = new List<CardData>();
                    List<string> heroHand = MatchManager.Instance.GetHeroHand(_character.HeroIndex);
                    int num1 = 0;
                    for (int index2 = 0; index2 < heroHand.Count; ++index2)
                    {
                        CardData cardData = MatchManager.Instance.GetCardData(heroHand[index2]);
                        bool hasProperCardTypeToReduce = types2.Any(cardData.HasCardType);
                        if (cardData != null && hasProperCardTypeToReduce && _character.GetCardFinalCost(cardData) > num1)
                            num1 = _character.GetCardFinalCost(cardData);
                    }
                    if (num1 <= 0)
                        break;
                    for (int index3 = 0; index3 < heroHand.Count; ++index3)
                    {
                        CardData cardData = MatchManager.Instance.GetCardData(heroHand[index3]);
                        bool hasProperCardTypeToReduce = types2.Any(cardData.HasCardType);

                        if (cardData != null && hasProperCardTypeToReduce && _character.GetCardFinalCost(cardData) >= num1)
                            cardDataList.Add(cardData);
                    }
                    if (cardDataList.Count <= 0)
                        break;
                    CardData cardData1 = cardDataList.Count != 1 ? cardDataList[MatchManager.Instance.GetRandomIntRange(0, cardDataList.Count, "trait")] : cardDataList[0];
                    if (!(cardData1 != null))
                        break;
                    if (!MatchManager.Instance.activatedTraits.ContainsKey(traitId))
                        MatchManager.Instance.activatedTraits.Add(traitId, 1);
                    else
                        ++MatchManager.Instance.activatedTraits[traitId];
                    MatchManager.Instance.SetTraitInfoText();
                    int num2 = 1;
                    cardData1.EnergyReductionTemporal += num2;
                    MatchManager.Instance.GetCardFromTableByIndex(cardData1.InternalId).ShowEnergyModification(-num2);
                    MatchManager.Instance.UpdateHandCards();
                    _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_" + traitData.TraitName) + TextChargesLeft(MatchManager.Instance.activatedTraits[traitId], traitData.TimesPerTurn + bonusActivations), Enums.CombatScrollEffectType.Trait);

                    MatchManager.Instance.CreateLogCardModification(cardData1.InternalId, MatchManager.Instance.GetHero(_character.HeroIndex));
                    break;
                }
            }
        }

    }
}

