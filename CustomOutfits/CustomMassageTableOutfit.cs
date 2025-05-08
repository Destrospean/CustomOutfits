using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Services;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.Store.Objects;
using Sims3.UI;
using System;
using System.Collections.Generic;
using static Destrospean.Common;
using static Sims3.Gameplay.Destrospean.CustomOutfits;

namespace Destrospean
{
    public class CustomMassageTableOutfit
    {
        public static readonly string kMassageTableSpecialOutfitKey = "MassageTable";

        [Tunable]
        protected static bool kInstantiator;

        [PersistableStatic]
        static List<ulong> sMassageTableOutfitDisabledList;

        [PersistableStatic]
        static EventListener sObjectBoughtListener;

        [PersistableStatic]
        static EventListener sSimDestroyedListener;

        [PersistableStatic]
        static EventListener sSimSelectedListener;

        static CustomMassageTableOutfit()
        {
            kInstantiator = false;
            sMassageTableOutfitDisabledList = new List<ulong>();
            sObjectBoughtListener = null;
            sSimDestroyedListener = null;
            sSimSelectedListener = null;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            World.sOnWorldQuitEventHandler += OnWorldQuit;
        }

        public class EditMassageTableOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomMassageTableOutfit/EditMassageTableOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditMassageTableOutfit>
            {
                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return Localize(actor.IsFemale, sLocalizationKey + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Localize(isFemale, sLocalizationKey + "Path")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return !((target is Sim && actor != target) || actor.SimDescription.ChildOrBelow || !actor.SimDescription.IsHuman || actor.SimDescription.IsRobot || isAutonomous);
                }
            }

            public override bool Run()
            {
                return EditSpecialOutfit(Actor, sLocalizationKey, kMassageTableSpecialOutfitKey, GetMassageTableOutfitName(Actor), ProductVersion.EP3, Actor.SimDescription.GetOutfit(OutfitCategories.Swimwear, 0));
            }
        }

        public class GetMassage : MassageTable.GetMassage
        {
            public class DefinitionModified : InteractionDefinition<Sim, MassageTable, GetMassage>
            {
                public override bool Test(Sim actor, MassageTable target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return actor.IsHuman;
                }

                public override string GetInteractionName(Sim actor, MassageTable target, InteractionObjectPair interaction)
                {
                    return MassageTable.LocalizeString("GetMassage");
                }
            }

            public override string GetInteractionName()
            {
                string text = MassageTable.LocalizeString("GetMassage");
                if (mMassInfo != null)
                {
                    text += "\n" + MassageTable.LocalizeString(mMassInfo.mName);
                }
                return text;
            }

            public override bool Run()
            {
                if (!Actor.RouteToSlot(Target, Slot.RoutingSlot_1))
                {
                    return false;
                }
                Actor.ClearExitReasons();
                if (!StartSync(false))
                {
                    return false;
                }
                bool needToSwitchOutfitOnFinish = GetMassageTableOutfitEnabled(Actor.SimDescription);
                if (needToSwitchOutfitOnFinish)
                {
                    ChangeSimToTowelOutfit(Actor);
                }
                Actor.LoopIdle();
                Actor.SynchronizationLevel = Sim.SyncLevel.Routed;
                Actor.ClearExitReasons();
                if (!Actor.WaitForSynchronizationLevelWithSim(mMasseuse, Sim.SyncLevel.Routed, 60f))
                {
                    return false;
                }
                StandardEntry();
                BeginCommodityUpdates();
                WaitForMasterInteractionToFinish();
                EndCommodityUpdates(true);
                StandardExit();
                WaitForSyncComplete();
                if (needToSwitchOutfitOnFinish)
                {
                    ChangeSimToEverydayOutfit(Actor);
                }
                return true;
            }
        }

        public class ResetMassageTableOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomMassageTableOutfit/ResetMassageTableOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ResetMassageTableOutfit>
            {
                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return Localize(actor.IsFemale, sLocalizationKey + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Localize(isFemale, sLocalizationKey + "Path")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return !(!actor.SimDescription.HasSpecialOutfit(kMassageTableSpecialOutfitKey) || (target is Sim && actor != target) || actor.SimDescription.ChildOrBelow || !actor.SimDescription.IsHuman || actor.SimDescription.IsRobot || isAutonomous);
                }
            }

            public override bool Run()
            {
                Actor.SimDescription.RemoveSpecialOutfit(kMassageTableSpecialOutfitKey);
                Notify(Localize(Actor.IsFemale, sLocalizationKey + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                return true;
            }
        }

        public class ToggleMassageTableOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomMassageTableOutfit/ToggleMassageTableOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ToggleMassageTableOutfit>
            {
                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    if (GetMassageTableOutfitEnabled(actor.SimDescription))
                    {
                        return Localize(actor.IsFemale, sLocalizationKey + "DisableInteractionName");
                    }
                    return Localize(actor.IsFemale, sLocalizationKey + "EnableInteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Localize(isFemale, sLocalizationKey + "Path")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return !((target is Sim && actor != target) || actor.SimDescription.ChildOrBelow || !actor.SimDescription.IsHuman || actor.SimDescription.IsRobot || isAutonomous);
                }
            }

            public override bool Run()
            {
                if (GetMassageTableOutfitEnabled(Actor.SimDescription))
                {
                    DisableMassageTableOutfit(Actor.SimDescription);
                    Notify(Localize(Actor.IsFemale, sLocalizationKey + "DisabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                else
                {
                    EnableMassageTableOutfit(Actor.SimDescription);
                    Notify(Localize(Actor.IsFemale, sLocalizationKey + "EnabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                return true;
            }
        }

        static void AddInteractions(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }
            foreach (InteractionObjectPair interaction in gameObject.Interactions)
            {
                if (interaction.InteractionDefinition.GetType() == EditMassageTableOutfit.Singleton.GetType())
                {
                    return;
                }
            }
            gameObject.AddInteraction(EditMassageTableOutfit.Singleton);
            gameObject.AddInteraction(ResetMassageTableOutfit.Singleton);
            gameObject.AddInteraction(ToggleMassageTableOutfit.Singleton);
        }

        public static void ChangeSimToTowelOutfit(Sim actor)
        {
            if (!(actor.CurrentOutfitCategory == OutfitCategories.Singed || actor.Service is GrimReaper))
            {
                SimOutfit simOutfit = null;
                SimDescription simDescription = actor.SimDescription;
                if (simDescription.HasSpecialOutfit(kMassageTableSpecialOutfitKey))
                {
                    simDescription.AddOutfit(simDescription.GetSpecialOutfit(kMassageTableSpecialOutfitKey), OutfitCategories.SkinnyDippingTowel);
                    simOutfit = simDescription.GetOutfit(OutfitCategories.SkinnyDippingTowel, simDescription.GetOutfitCount(OutfitCategories.SkinnyDippingTowel) - 1);
                }
                else if (OutfitUtils.TryApplyUniformToOutfit(simDescription.GetOutfit(OutfitCategories.Swimwear, 0), new SimOutfit(ResourceKey.CreateOutfitKey(GetMassageTableOutfitName(actor), ResourceUtils.ProductVersionToGroupId(ProductVersion.EP3))), simDescription, "ChangeSimToTowelOutfit", out var resultOutfit))
                {
                    simDescription.AddOutfit(resultOutfit, OutfitCategories.SkinnyDippingTowel);
                    simOutfit = simDescription.GetOutfit(OutfitCategories.SkinnyDippingTowel, simDescription.GetOutfitCount(OutfitCategories.SkinnyDippingTowel) - 1);
                }
                if (simOutfit == null)
                {
                    actor.SwitchToOutfitWithSpin(Sim.ClothesChangeReason.Force, OutfitCategories.Sleepwear);
                }
                else
                {
                    actor.SwitchToOutfitWithSpin(simOutfit.Key);
                }
            }
        }

        public static void ChangeSimToEverydayOutfit(Sim actor)
        {
            if (!(actor.CurrentOutfitCategory == OutfitCategories.Singed || actor.Service is GrimReaper))
            {
                if (actor.CurrentOutfitCategory == OutfitCategories.SkinnyDippingTowel)
                {
                    actor.SimDescription.RemoveOutfit(OutfitCategories.SkinnyDippingTowel, actor.CurrentOutfitIndex, true);
                }
                actor.SwitchToOutfitWithSpin(Sim.ClothesChangeReason.Force, OutfitCategories.Everyday);
            }
        }

        static void DisableMassageTableOutfit(SimDescription simDescription)
        {
            if (sMassageTableOutfitDisabledList == null)
            {
                sMassageTableOutfitDisabledList = new List<ulong>();
            }
            if (GetMassageTableOutfitEnabled(simDescription))
            {
                sMassageTableOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
            UpdateListeners();
        }

        static void EnableMassageTableOutfit(SimDescription simDescription)
        {
            if (!GetMassageTableOutfitEnabled(simDescription))
            {
                sMassageTableOutfitDisabledList.Remove(simDescription.SimDescriptionId);
                UpdateListeners();
            }
        }

        static bool GetMassageTableOutfitEnabled(SimDescription simDescription)
        {
            return !sMassageTableOutfitDisabledList.Contains(simDescription.SimDescriptionId);
        }

        public static string GetMassageTableOutfitName(Sim actor)
        {
            return (actor.IsMale ? "m" : "f") + OutfitUtils.GetAgePrefix(actor.SimDescription.Age, true) + "_towel";
        }

        static void Init()
        {
            UpdateListeners();
        }

        static ListenerAction OnObjectBought(Event e)
        {
            try
            {
                if (kShowObjectMenu && e.TargetObject is MassageTable massageTable)
                {
                    AddInteractions(massageTable);
                }
            }
            catch (Exception ex)
            {
                ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, ex);
            }
            return ListenerAction.Keep;
        }

        static void OnPreLoad()
        {
            MassageTable.GetMassage.Singleton = new GetMassage.DefinitionModified();
            CopyTuning(typeof(MassageTable), typeof(MassageTable.GetMassage.Definition), typeof(GetMassage.DefinitionModified));
        }

        static ListenerAction OnSimDestroyed(Event e)
        {
            try
            {
                if (e.Actor is Sim sim)
                {
                    EnableMassageTableOutfit(sim.SimDescription);
                }
            }
            catch (Exception ex)
            {
                ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, ex);
            }
            return ListenerAction.Keep;
        }

        static ListenerAction OnSimSelected(Event e)
        {
            try
            {
                if (kShowSimMenu)
                {
                    AddInteractions(Sim.ActiveActor);
                }
            }
            catch (Exception ex)
            {
                ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, ex);
            }
            return ListenerAction.Keep;
        }

        static void OnWorldLoadFinished(object sender, EventArgs e)
        {
            Init();
            if (kShowObjectMenu)
            {
                foreach (MassageTable massageTable in Sims3.Gameplay.Queries.GetObjects<MassageTable>())
                {
                    AddInteractions(massageTable);
                }
            }
            if (kShowSimMenu && Household.ActiveHousehold != null)
            {
                foreach (Sim sim in Household.ActiveHousehold.Sims)
                {
                    AddInteractions(sim);
                }
            }
        }

        static void OnWorldQuit(object sender, EventArgs e)
        {
            EventTracker.RemoveListener(sObjectBoughtListener);
            EventTracker.RemoveListener(sSimDestroyedListener);
            EventTracker.RemoveListener(sSimSelectedListener);
            sObjectBoughtListener = null;
            sSimDestroyedListener = null;
            sSimSelectedListener = null;
        }

        static void UpdateListeners()
        {
            if (sObjectBoughtListener != null)
            {
                EventTracker.RemoveListener(sObjectBoughtListener);
                sObjectBoughtListener = null;
            }
            if (sSimDestroyedListener != null)
            {
                EventTracker.RemoveListener(sSimDestroyedListener);
                sSimDestroyedListener = null;
            }
            if (sSimSelectedListener != null)
            {
                EventTracker.RemoveListener(sSimSelectedListener);
                sSimSelectedListener = null;
            }
            sObjectBoughtListener = EventTracker.AddListener(EventTypeId.kBoughtObject, OnObjectBought);
            sSimDestroyedListener = EventTracker.AddListener(EventTypeId.kSimDescriptionDisposed, OnSimDestroyed);
            sSimSelectedListener = EventTracker.AddListener(EventTypeId.kEventSimSelected, OnSimSelected);
        }
    }
}