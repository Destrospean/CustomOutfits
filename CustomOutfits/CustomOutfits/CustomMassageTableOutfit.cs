using System;
using System.Collections.Generic;
using Destrospean.CustomOutfits;
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
using Tuning = Sims3.Gameplay.Destrospean.CustomOutfits;

namespace Destrospean
{
    public class CustomMassageTableOutfit
    {
        public static readonly string kMassageTableSpecialOutfitKey = "MassageTable";

        [Tunable]
        protected static bool kInstantiator;

        [PersistableStatic(true)]
        static List<ulong> sMassageTableOutfitDisabledList;

        static EventListener sSimDestroyedListener, sSimSelectedListener;

        static CustomMassageTableOutfit()
        {
            kInstantiator = false;
            sMassageTableOutfitDisabledList = new List<ulong>();
            sSimDestroyedListener = null;
            sSimSelectedListener = null;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnObjectPlacedInLotEventHandler += OnObjectPlacedInLot;
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
                    return Common.Localize(actor.IsFemale, sLocalizationKey + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, sLocalizationKey + "Path")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return ((target is Sim && actor == target && Tuning.kShowSimMenu) || (!(target is Sim) && Tuning.kShowObjectMenu)) && actor.SimDescription.TeenOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public override bool Run()
            {
                return Common.EditSpecialOutfit(Actor, sLocalizationKey, kMassageTableSpecialOutfitKey, GetMassageTableOutfitName(Actor), ProductVersion.EP3, Actor.SimDescription.GetOutfit(OutfitCategories.Swimwear, 0));
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
                    return Common.Localize(actor.IsFemale, sLocalizationKey + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, sLocalizationKey + "Path")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return ((target is Sim && actor == target && Tuning.kShowSimMenu) || (!(target is Sim) && Tuning.kShowObjectMenu)) && actor.SimDescription.TeenOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous && actor.SimDescription.HasSpecialOutfit(kMassageTableSpecialOutfitKey);
                }
            }

            public override bool Run()
            {
                Actor.SimDescription.RemoveSpecialOutfit(kMassageTableSpecialOutfitKey);
                Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
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
                        return Common.Localize(actor.IsFemale, sLocalizationKey + "DisableInteractionName");
                    }
                    return Common.Localize(actor.IsFemale, sLocalizationKey + "EnableInteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, sLocalizationKey + "Path")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return ((target is Sim && actor == target && Tuning.kShowSimMenu) || (!(target is Sim) && Tuning.kShowObjectMenu)) && actor.SimDescription.TeenOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public override bool Run()
            {
                if (GetMassageTableOutfitEnabled(Actor.SimDescription))
                {
                    DisableMassageTableOutfit(Actor.SimDescription);
                    Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "DisabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                else
                {
                    EnableMassageTableOutfit(Actor.SimDescription);
                    Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "EnabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                return true;
            }
        }

        static void AddInteractions(GameObject gameObject)
        {
            if (gameObject != null && !gameObject.Interactions.Exists(interaction => interaction.InteractionDefinition.GetType() == EditMassageTableOutfit.Singleton.GetType()))
            {
                gameObject.AddInteraction(EditMassageTableOutfit.Singleton);
                gameObject.AddInteraction(ResetMassageTableOutfit.Singleton);
                gameObject.AddInteraction(ToggleMassageTableOutfit.Singleton);
            }
        }

        public static void ChangeSimToTowelOutfit(Sim actor)
        {
            if (!(actor.CurrentOutfitCategory == OutfitCategories.Singed || actor.Service is GrimReaper))
            {
                SimOutfit resultOutfit, simOutfit = null;
                SimDescription simDescription = actor.SimDescription;
                if (simDescription.HasSpecialOutfit(kMassageTableSpecialOutfitKey))
                {
                    simDescription.AddOutfit(simDescription.GetSpecialOutfit(kMassageTableSpecialOutfitKey), OutfitCategories.SkinnyDippingTowel);
                    simOutfit = simDescription.GetOutfit(OutfitCategories.SkinnyDippingTowel, simDescription.GetOutfitCount(OutfitCategories.SkinnyDippingTowel) - 1);
                }
                else if (OutfitUtils.TryApplyUniformToOutfit(simDescription.GetOutfit(OutfitCategories.Swimwear, 0), new SimOutfit(ResourceKey.CreateOutfitKey(GetMassageTableOutfitName(actor), ResourceUtils.ProductVersionToGroupId(ProductVersion.EP3))), simDescription, "ChangeSimToTowelOutfit", out resultOutfit))
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
            if (GetMassageTableOutfitEnabled(simDescription))
            {
                sMassageTableOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
        }

        static void EnableMassageTableOutfit(SimDescription simDescription)
        {
            if (!GetMassageTableOutfitEnabled(simDescription))
            {
                sMassageTableOutfitDisabledList.Remove(simDescription.SimDescriptionId);
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

        static void OnObjectPlacedInLot(object sender, EventArgs e)
        {
            World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs = e as World.OnObjectPlacedInLotEventArgs;
            if (onObjectPlacedInLotEventArgs != null)
            {
                AddInteractions(GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId) as MassageTable);
            }
        }

        static void OnPreLoad()
        {
            MassageTable.GetMassage.Singleton = new GetMassage.DefinitionModified();
            Common.CopyTuning(typeof(MassageTable), typeof(MassageTable.GetMassage.Definition), typeof(GetMassage.DefinitionModified));
        }

        static ListenerAction OnSimDestroyed(Event e)
        {
            try
            {
                Sim sim = e.TargetObject as Sim;
                if (sim != null)
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
                AddInteractions(Sim.ActiveActor);
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
            new List<MassageTable>(Sims3.Gameplay.Queries.GetObjects<MassageTable>()).ForEach(AddInteractions);
            if (Household.ActiveHousehold != null)
            {
                Household.ActiveHousehold.Sims.ForEach(AddInteractions);
            }
        }

        static void OnWorldQuit(object sender, EventArgs e)
        {
            EventTracker.RemoveListener(sSimDestroyedListener);
            EventTracker.RemoveListener(sSimSelectedListener);
            sSimDestroyedListener = null;
            sSimSelectedListener = null;
        }

        static void UpdateListeners()
        {
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
            sSimDestroyedListener = EventTracker.AddListener(EventTypeId.kSimDescriptionDisposed, OnSimDestroyed);
            sSimSelectedListener = EventTracker.AddListener(EventTypeId.kEventSimSelected, OnSimSelected);
        }
    }
}