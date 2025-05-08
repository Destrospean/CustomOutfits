using Sims3.Gameplay;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using Sims3.UI.CAS;
using System;

namespace Destrospean
{
    public class Common
    {
        internal static readonly string kLocalizationPath = "Destrospean/";

        public static void CopyTuning(Type baseType, Type oldType, Type newType)
        {
            if (AutonomyTuning.GetTuning(newType.FullName, baseType.FullName) == null)
            {
                InteractionTuning tuning = AutonomyTuning.GetTuning(oldType, oldType.FullName, baseType);
                if (tuning != null)
                {
                    AutonomyTuning.AddTuning(newType.FullName, baseType.FullName, tuning);
                }
            }
            InteractionObjectPair.sTuningCache.Remove(new Pair<Type, Type>(newType, baseType));
        }

        public static ResourceKey CreateAndAddSpecialOutfit(Sim actor, string specialOutfitKey, ResourceKey uniformKey)
        {
            if (uniformKey == ResourceKey.kInvalidResourceKey)
            {
                return uniformKey;
            }
            SimDescription simDescription = actor.SimDescription;
            if (simDescription.HasSpecialOutfit(specialOutfitKey))
            {
                return simDescription.GetSpecialOutfit(specialOutfitKey).Key;
            }
            ResourceKey key = OutfitUtils.ApplyUniformToOutfit(simDescription.GetOutfit(OutfitCategories.Everyday, 0), new SimOutfit(uniformKey), simDescription, "CreateAndAddTempOutfit");
            simDescription.AddSpecialOutfit(new SimOutfit(key), specialOutfitKey);
            return key;
        }

        public static bool EditSpecialOutfit(Sim actor, string localizationKey, string specialOutfitKey)
        {
            SimDescription simDescription = actor.SimDescription;
            if (!simDescription.HasSpecialOutfit(specialOutfitKey))
            {
                simDescription.AddSpecialOutfit(simDescription.GetOutfit(OutfitCategories.Everyday, 0), specialOutfitKey);
            }
            OutfitCategories previousOutfitCategory = actor.CurrentOutfitCategory;
            int previousOutfitIndex = actor.CurrentOutfitIndex;
            simDescription.AddOutfit(simDescription.GetSpecialOutfit(specialOutfitKey), OutfitCategories.Everyday, 0);
            simDescription.RemoveSpecialOutfit(specialOutfitKey);
            actor.SwitchToOutfitWithoutSpin(OutfitCategories.Everyday, 0);
            CASLogic cASLogic = CASLogic.GetSingleton();
            cASLogic.ShowUI = (ShowUIDelegate)Delegate.Combine(cASLogic.ShowUI, new ShowUIDelegate(OnShowUI));
            cASLogic.UseTempSimDesc = true;
            cASLogic.LoadSim(simDescription, actor.CurrentOutfitCategory, actor.CurrentOutfitIndex);
            CASChangeReporter.Instance.ClearChanges();
            GameStates.TransitionToCASStylistMode();
            // Notify(Localize(actor.IsFemale, localizationKey + "Warning", actor.Name), simDescription, StyledNotification.NotificationStyle.kSystemMessage);
            while (GameStates.NextInWorldStateId != 0)
            {
                Simulator.Sleep(0u);
            }
            CASChangeReporter.Instance.SendChangedEvents(actor);
            cASLogic.ShowUI = (ShowUIDelegate)Delegate.Remove(cASLogic.ShowUI, new ShowUIDelegate(OnShowUI));
            simDescription.AddSpecialOutfit(simDescription.GetOutfit(OutfitCategories.Everyday, 0), specialOutfitKey);
            simDescription.RemoveOutfit(OutfitCategories.Everyday, 0, true);
            actor.SwitchToOutfitWithoutSpin(previousOutfitCategory, previousOutfitIndex);
            if (!CASChangeReporter.Instance.CasCancelled)
            {
                Notify(Localize(actor.IsFemale, localizationKey + "Feedback", actor.Name), simDescription, StyledNotification.NotificationStyle.kSystemMessage);
            }
            return true;
        }

        public static bool EditSpecialOutfit(Sim actor, string localizationKey, string specialOutfitKey, string outfitName, uint group)
        {
            return EditSpecialOutfit(actor, localizationKey, specialOutfitKey, outfitName, group, actor.SimDescription.GetOutfit(OutfitCategories.Everyday, 0));
        }

        public static bool EditSpecialOutfit(Sim actor, string localizationKey, string specialOutfitKey, string outfitName, uint group, SimOutfit outfitToApplyTo)
        {
            SimDescription simDescription = actor.SimDescription;
            if (!simDescription.HasSpecialOutfit(specialOutfitKey))
            {
                if (OutfitUtils.TryApplyUniformToOutfit(outfitToApplyTo, new SimOutfit(ResourceKey.CreateOutfitKey(outfitName, group)), simDescription, "EditSpecialOutfit", out var resultOutfit))
                {
                    simDescription.AddSpecialOutfit(resultOutfit, specialOutfitKey);
                }
            }
            return EditSpecialOutfit(actor, localizationKey, specialOutfitKey);
        }

        public static bool EditSpecialOutfit(Sim actor, string localizationKey, string specialOutfitKey, string outfitName, ProductVersion productVersion)
        {
            return EditSpecialOutfit(actor, localizationKey, specialOutfitKey, outfitName, ResourceUtils.ProductVersionToGroupId(productVersion));
        }

        public static bool EditSpecialOutfit(Sim actor, string localizationKey, string specialOutfitKey, string outfitName, ProductVersion productVersion, SimOutfit outfitToApplyTo)
        {
            return EditSpecialOutfit(actor, localizationKey, specialOutfitKey, outfitName, ResourceUtils.ProductVersionToGroupId(productVersion), outfitToApplyTo);
        }

        public static string Localize(string entryKey)
        {
            return Localization.LocalizeString(kLocalizationPath + entryKey);
        }

        public static string Localize(string entryKey, params object[] parameters)
        {
            return Localization.LocalizeString(kLocalizationPath + entryKey, parameters);
        }

        public static string Localize(bool isFemale, string entryKey, params object[] parameters)
        {
            return Localization.LocalizeString(isFemale, kLocalizationPath + entryKey, parameters);
        }

        public static void Notify(string message, SimDescription simDescription, StyledNotification.NotificationStyle style)
        {
            Notify(message, simDescription, style, true);
        }

        public static void Notify(string message, SimDescription fakeSimDescription, StyledNotification.NotificationStyle style, bool checkForFake)
        {
            SimDescription simDescription = fakeSimDescription;
            if (simDescription == null)
            {
                StyledNotification.Show(new StyledNotification.Format(message, style));
                return;
            }
            if (checkForFake)
            {
                simDescription = SimDescription.Find(fakeSimDescription.SimDescriptionId);
                if (simDescription == null)
                {
                    StyledNotification.Show(new StyledNotification.Format(message, style));
                    return;
                }
            }
            if (simDescription.CreatedSim != null)
            {
                StyledNotification.Show(new StyledNotification.Format(message, ObjectGuid.InvalidObjectGuid, simDescription.CreatedSim.ObjectId, style));
            }
            else
            {
                StyledNotification.Show(new StyledNotification.Format(message, style));
            }
        }

        public static void OnShowUI(bool toShow)
        {
            if (!toShow)
            {
                return;
            }
            CASDresserSheet cASDresserSheet = CASDresserSheet.gSingleton;
            if (cASDresserSheet == null || cASDresserSheet.mButtons == null)
            {
                return;
            }
            for (int i = 1; i < cASDresserSheet.mButtons.Length; i++)
            {
                if (cASDresserSheet.mButtons[i] != null)
                {
                    cASDresserSheet.mButtons[i].Visible = false;
                }
                if (cASDresserSheet.mButtonText[i] != null)
                {
                    cASDresserSheet.mButtonText[i].Visible = false;
                }
            }
            CASDresserClothing cASDresserClothing = CASDresserClothing.gSingleton;
            if (cASDresserClothing == null || cASDresserClothing.mOutfitButtons == null || cASDresserClothing.mDeleteOutfitButtons == null)
            {
                return;
            }
            for (int i = 1; i < cASDresserClothing.mOutfitButtons.Length; i++)
            {
                cASDresserClothing.mOutfitButtons[i].Visible = false;
                cASDresserClothing.mDeleteOutfitButtons[i].Visible = false;
            }
            cASDresserClothing.mAddOutfitButton.Visible = false;
        }
    }
}