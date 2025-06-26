using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Objects.Insect;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.SimIFace.Enums;
using Sims3.UI;
using System;
using System.Collections.Generic;
using static Destrospean.Common;
using static Sims3.Gameplay.Destrospean.CustomOutfits;
using static Sims3.Gameplay.Objects.Insect.BeekeepingBox;

namespace Destrospean
{
    public class CustomBeekeeperOutfit
    {
        [Tunable]
        protected static bool kInstantiator;

        [PersistableStatic]
        static List<ulong> sBeekeeperOutfitDisabledList;

        [PersistableStatic]
        static EventListener sSimDestroyedListener;

        [PersistableStatic]
        static EventListener sSimSelectedListener;

        static CustomBeekeeperOutfit()
        {
            kInstantiator = false;
            sBeekeeperOutfitDisabledList = new List<ulong>();
            sSimDestroyedListener = null;
            sSimSelectedListener = null;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnObjectPlacedInLotEventHandler += OnObjectPlacedInLot;
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            World.sOnWorldQuitEventHandler += OnWorldQuit;
        }

        public class Clean : BeekeepingBox.Clean
        {
            public class DefinitionModified : InteractionDefinition<Sim, BeekeepingBox, Clean>
            {
                public override string GetInteractionName(Sim actor, BeekeepingBox target, InteractionObjectPair interaction)
                {
                    return LocalizeString("CleanBox");
                }

                public override bool Test(Sim actor, BeekeepingBox target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return !target.Charred && target.mCurrentAttackTarget == null;
                }
            }

            public override bool Run()
            {
                bool baseInteractionFunctionalityDone = false;
                AlarmHandle alarmHandle = AlarmHandle.kInvalidHandle;
                baseInteractionFunctionalityDone = DoBaseInteractionFunctionality(mCurrentStateMachine, Actor, this, out var outfitChanged, out var swimwearUsed, Target);
                if (baseInteractionFunctionalityDone)
                {
                    AnimateSim("FeedCleanLoop");
                    alarmHandle = Target.AddAlarm(kCleanAnimationDurationInMinutes, TimeUnit.Minutes, ExitAfterDuration, "Beekeeping exit clean animation", AlarmType.DeleteOnReset);
                    DoLoop(ExitReason.Default);
                    AnimateSim("Success");
                }
                if (!Actor.HasExitReason(ExitReason.RouteFailed))
                {
                    if (Actor.HasExitReason(ExitReason.CanceledByScript))
                    {
                        Target.mCleanlinessLevel = 100f;
                    }
                    else if (alarmHandle != AlarmHandle.kInvalidHandle)
                    {
                        Target.RemoveAlarm(alarmHandle);
                    }
                    EndCommodityUpdates(baseInteractionFunctionalityDone);
                    StandardExit();
                }
                Target.RestorePreviousOutfit(Actor, swimwearUsed, outfitChanged, baseInteractionFunctionalityDone);
                return baseInteractionFunctionalityDone;
            }
        }

        public class EditBeekeeperOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomBeekeeperOutfit/EditBeekeeperOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditBeekeeperOutfit>
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
                string outfitName = GetBeekeeperOutfitName(Actor);
                return EditSpecialOutfit(Actor, sLocalizationKey, outfitName, outfitName, ProductVersion.EP7);
            }
        }

        public class Feed : BeekeepingBox.Feed
        {
            public class DefinitionModified : InteractionDefinition<Sim, BeekeepingBox, Feed>
            {
                public override string GetInteractionName(Sim actor, BeekeepingBox target, InteractionObjectPair interaction)
                {
                    return LocalizeString("FeedBees");
                }

                public override bool Test(Sim actor, BeekeepingBox target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return !target.Charred && target.mCurrentAttackTarget == null;
                }
            }

            public override bool Run()
            {
                bool baseInteractionFunctionalityDone = false;
                AlarmHandle alarmHandle = AlarmHandle.kInvalidHandle;
                baseInteractionFunctionalityDone = DoBaseInteractionFunctionality(mCurrentStateMachine, Actor, this, out var outfitChanged, out var swimwearUsed, Target);
                if (baseInteractionFunctionalityDone)
                {
                    AnimateSim("FeedCleanLoop");
                    alarmHandle = Target.AddAlarm(kFeedAnimationDurationInMinutes, TimeUnit.Minutes, ExitAfterDuration, "Beekeeping exit feed animation", AlarmType.DeleteOnReset);
                    DoLoop(ExitReason.Default);
                    AnimateSim("Success");
                    EventTracker.SendEvent(EventTypeId.kFedBees, Actor);
                }
                if (!Actor.HasExitReason(ExitReason.RouteFailed))
                {
                    if (Actor.HasExitReason(ExitReason.CanceledByScript))
                    {
                        Target.mHungerLevel = 100f;
                    }
                    else if (alarmHandle != AlarmHandle.kInvalidHandle)
                    {
                        Target.RemoveAlarm(alarmHandle);
                    }
                    EndCommodityUpdates(baseInteractionFunctionalityDone);
                    StandardExit();
                }
                Target.RestorePreviousOutfit(Actor, swimwearUsed, outfitChanged, baseInteractionFunctionalityDone);
                return baseInteractionFunctionalityDone;
            }
        }

        public class Harvest : BeekeepingBox.Harvest
        {
            public class DefinitionModified : InteractionDefinition<Sim, BeekeepingBox, Harvest>
            {
                public override string GetInteractionName(Sim actor, BeekeepingBox target, InteractionObjectPair interaction)
                {
                    return LocalizeString("HarvestHoney");
                }

                public string NoHoneyStored()
                {
                    return LocalizeString("NoHoneyStored");
                }

                public override bool Test(Sim actor, BeekeepingBox target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (target.Charred || target.mCurrentAttackTarget != null)
                    {
                        return false;
                    }
                    if (target.mStoredHoneyInfo != null && target.mStoredHoneyInfo.Count > 0)
                    {
                        return true;
                    }
                    greyedOutTooltipCallback = NoHoneyStored;
                    return false;
                }
            }

            public override bool Run()
            {
                bool baseInteractionFunctionalityDone = false;
                baseInteractionFunctionalityDone = DoBaseInteractionFunctionality(mCurrentStateMachine, Actor, this, out var outfitChanged, out var swimwearUsed, Target);
                if (baseInteractionFunctionalityDone)
                {
                    AnimateSim("Harvest");
                    bool honeyStorageLimitMet = Target.mStoredHoneyInfo.Count == kHoneyStorageLimit;
                    while (Target.mStoredHoneyInfo.Count > 0)
                    {
                        HoneyCreationInfo honeyCreationInfo = Target.mStoredHoneyInfo[0];
                        Target.mStoredHoneyInfo.RemoveAt(0);
                        Ingredient honey = Ingredient.Create(IngredientData.NameToDataMap["Honey"]);
                        honey.SetQuality(honeyCreationInfo.Quality);
                        Actor.Inventory.TryToAdd(honey);
                        EventTracker.SendEvent(new GuidEvent<Quality>(EventTypeId.kHarvestedHoney, Actor, honeyCreationInfo.Quality));
                        if (honeyCreationInfo.HasBeesWax)
                        {
                            mCurrentStateMachine.SetParameter("harvestsBeeswax", YesOrNo.yes);
                            Ingredient beesWax = Ingredient.Create(IngredientData.NameToDataMap["BeesWax"]);
                            beesWax.SetQuality(honeyCreationInfo.Quality);
                            Actor.Inventory.TryToAdd(beesWax);
                        }
                    }
                    AnimateSim("Success");
                    if (honeyStorageLimitMet)
                    {
                        Target.mHoneyProductionAlarm = Target.AddAlarmRepeating(kHoneyProductionTimeInSimHours, TimeUnit.Hours, Target.ProduceHoney, "Produce Honey", AlarmType.AlwaysPersisted);
                    }
                }
                EndCommodityUpdates(baseInteractionFunctionalityDone);
                StandardExit();
                Target.RestorePreviousOutfit(Actor, swimwearUsed, outfitChanged, baseInteractionFunctionalityDone);
                return baseInteractionFunctionalityDone;
            }
        }

        public class ResetBeekeeperOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomBeekeeperOutfit/ResetBeekeeperOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ResetBeekeeperOutfit>
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
                    return !(!actor.SimDescription.HasSpecialOutfit(GetBeekeeperOutfitName(actor)) || (target is Sim && actor != target) || actor.SimDescription.ChildOrBelow || !actor.SimDescription.IsHuman || actor.SimDescription.IsRobot || isAutonomous);
                }
            }

            public override bool Run()
            {
                Actor.SimDescription.RemoveSpecialOutfit(GetBeekeeperOutfitName(Actor));
                Notify(Localize(Actor.IsFemale, sLocalizationKey + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                return true;
            }
        }

        public class SmokeOut : BeekeepingBox.SmokeOut
        {
            public class DefinitionModified : InteractionDefinition<Sim, BeekeepingBox, SmokeOut>
            {
                public override bool Test(Sim actor, BeekeepingBox target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return !target.Charred && target.mCurrentAttackTarget == null;
                }

                public override string GetInteractionName(Sim actor, BeekeepingBox target, InteractionObjectPair interaction)
                {
                    return LocalizeString("SmokeOut");
                }
            }

            public override bool Run()
            {
                bool baseInteractionFunctionalityDone;
                _ = AlarmHandle.kInvalidHandle;
                baseInteractionFunctionalityDone = DoBaseInteractionFunctionality(mCurrentStateMachine, Actor, this, out var outfitChanged, out var swimwearUsed, Target);
                if (baseInteractionFunctionalityDone)
                {
                    AnimateSim("Smoke");
                    Target.SmokeOutHelper();
                    AnimateSim("Success");
                }
                if (!Actor.HasExitReason(ExitReason.RouteFailed))
                {
                    EndCommodityUpdates(baseInteractionFunctionalityDone);
                    StandardExit();
                }
                Target.RestorePreviousOutfit(Actor, swimwearUsed, outfitChanged, baseInteractionFunctionalityDone);
                return baseInteractionFunctionalityDone;
            }
        }

        public class ToggleBeekeeperOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomBeekeeperOutfit/ToggleBeekeeperOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ToggleBeekeeperOutfit>
            {
                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    if (GetBeekeeperOutfitEnabled(actor.SimDescription))
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
                if (GetBeekeeperOutfitEnabled(Actor.SimDescription))
                {
                    DisableBeekeeperOutfit(Actor.SimDescription);
                    Notify(Localize(Actor.IsFemale, sLocalizationKey + "DisabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                else
                {
                    EnableBeekeeperOutfit(Actor.SimDescription);
                    Notify(Localize(Actor.IsFemale, sLocalizationKey + "EnabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                return true;
            }
        }

        static void AddInteractions(GameObject gameObject)
        {
            if (gameObject == null || !GameUtils.IsInstalled(ProductVersion.EP7))
            {
                return;
            }
            foreach (InteractionObjectPair interaction in gameObject.Interactions)
            {
                if (interaction.InteractionDefinition.GetType() == EditBeekeeperOutfit.Singleton.GetType())
                {
                    return;
                }
            }
            gameObject.AddInteraction(EditBeekeeperOutfit.Singleton);
            gameObject.AddInteraction(ResetBeekeeperOutfit.Singleton);
            gameObject.AddInteraction(ToggleBeekeeperOutfit.Singleton);
        }

        public static bool ChangeSimToBeekeeperOutfit(Sim actor, out bool swimsuitUsed, BeekeepingBox target)
        {
            if (actor.BuffManager.HasTransformBuff() || actor.GetCurrentOutfitCategoryFromOutfitInGameObject() == OutfitCategories.Singed || !GetBeekeeperOutfitEnabled(actor.SimDescription))
            {
                swimsuitUsed = false;
                return false;
            }
            bool result = false;
            actor.RefreshCurrentOutfit(false);
            if (actor.TraitManager.HasAnyElement(TraitNames.Daredevil, TraitNames.Insane))
            {
                target.mLastOutfit = actor.CurrentOutfit;
                actor.SwitchToOutfitWithSpin(Sim.ClothesChangeReason.GoingToSwim, OutfitCategories.Swimwear, false, true, true);
                swimsuitUsed = true;
                result = true;
            }
            else
            {
                SimDescription simDescription = actor.SimDescription;
                string specialOutfitKey = GetBeekeeperOutfitName(actor);
                bool hasBeekeeperOutfit = simDescription.HasSpecialOutfit(specialOutfitKey);
                SimOutfit uniform = null;
                if (hasBeekeeperOutfit || OutfitUtils.TryGenerateSimOutfit(specialOutfitKey, ProductVersion.EP7, out uniform))
                {
                    SimOutfit resultOutfit = null;
                    if (hasBeekeeperOutfit || OutfitUtils.TryApplyUniformToOutfit(simDescription.GetOutfit(OutfitCategories.Everyday, 0), uniform, simDescription, "BeekeepingBox.ApplyBeekeepingOutfit", out resultOutfit))
                    {
                        if (resultOutfit == null)
                        {
                            resultOutfit = simDescription.GetSpecialOutfit(specialOutfitKey);
                        }
                        else
                        {
                            simDescription.AddSpecialOutfit(resultOutfit, specialOutfitKey);
                        }
                        target.mLastOutfit = actor.CurrentOutfit;
                        actor.SwitchToOutfitWithSpin(resultOutfit.Key);
                        result = true;
                    }
                }
                swimsuitUsed = false;
            }
            return result;
        }

        static void DisableBeekeeperOutfit(SimDescription simDescription)
        {
            if (sBeekeeperOutfitDisabledList == null)
            {
                sBeekeeperOutfitDisabledList = new List<ulong>();
            }
            if (GetBeekeeperOutfitEnabled(simDescription))
            {
                sBeekeeperOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
            UpdateListeners();
        }

        public static bool DoBaseInteractionFunctionality(StateMachineClient stateMachineClient, Sim actor, InteractionInstance instance, out bool outfitChanged, out bool swimwearUsed, BeekeepingBox target)
        {
            if (!actor.RouteToSlotAndCheckInUse(target, Slot.RoutingSlot_0))
            {
                actor.AddExitReason(ExitReason.RouteFailed);
                outfitChanged = false;
                swimwearUsed = false;
                return false;
            }
            outfitChanged = ChangeSimToBeekeeperOutfit(actor, out swimwearUsed, target);
            instance.StandardEntry();
            instance.BeginCommodityUpdates();
            instance.EnterStateMachine("beekeepingbox", "Enter", "x", "box");
            bool result = true;
            bool attackOccurred = false;
            bool stingOccurred = false;
            if (instance.InteractionDefinition != SmokeOut.Singleton && !actor.SimDescription.IsMummy && !actor.SimDescription.IsRobot)
            {
                target.GetAttackAndStingOccurrences(actor, out attackOccurred, out stingOccurred);
            }
            instance.mCurrentStateMachine.SetParameter("stingOccurs", stingOccurred ? YesOrNo.yes : YesOrNo.no);
            instance.AnimateSim("Action");
            if (stingOccurred)
            {
                BuffInstance element = actor.BuffManager.GetElement(BuffNames.BeeSting);
                if (element == null || element.EffectValue >= BuffManager.BuffDictionary[7533959535590961648uL].EffectValue)
                {
                    actor.BuffManager.AddElement(BuffNames.BeeSting, Origin.FromBeingStung);
                }
            }
            else if (attackOccurred)
            {
                actor.RequestWalkStyle(Sim.WalkStyle.AutoSelect);
                target.mVfxBeeAttack = VisualEffect.Create("ep7BeesAggressiveSim_main");
                target.mVfxBeeAttack.ParentTo(actor, Sim.FXJoints.Spine0);
                target.mVfxBeeAttack.Start();
                instance.AnimateSim("Attack");
                instance.mCurrentStateMachine.RemoveActor(actor);
                actor.RequestWalkStyle(Sim.WalkStyle.OnFire);
                actor.RouteAway(0.5f, 5f, true, new InteractionPriority(InteractionPriorityLevel.Fire), false, true, true, RouteDistancePreference.PreferFurthestFromRouteOrigin);
                target.mCurrentAttackTarget = actor;
                target.mAttackEndingAlarm = actor.AddAlarmRepeating(3f, TimeUnit.Seconds, target.CheckForBeeAttackEnding, "BeekeepingBox.CheckForBeeAttackEnding", AlarmType.AlwaysPersisted);
                actor.BuffManager.AddElement(BuffNames.BeeAttack, Origin.FromBeingAttackedByBees);
                result = false;
            }
            return result;
        }

        static void EnableBeekeeperOutfit(SimDescription simDescription)
        {
            if (!GetBeekeeperOutfitEnabled(simDescription))
            {
                sBeekeeperOutfitDisabledList.Remove(simDescription.SimDescriptionId);
                UpdateListeners();
            }
        }

        static bool GetBeekeeperOutfitEnabled(SimDescription simDescription)
        {
            return !sBeekeeperOutfitDisabledList.Contains(simDescription.SimDescriptionId);
        }

        public static string GetBeekeeperOutfitName(Sim actor)
        {
            return string.Format("{0}{1}Beekeeping", OutfitUtils.GetAgePrefix(actor.SimDescription.Age, true), actor.IsMale ? "m" : "f");
        }

        static void Init()
        {
            UpdateListeners();
        }

        static void OnObjectPlacedInLot(object sender, EventArgs e)
        {
            if (kShowObjectMenu && e is World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs && GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId) is BeekeepingBox beekeepingBox)
            {
                AddInteractions(beekeepingBox);
            }
        }

        static void OnPreLoad()
        {
            BeekeepingBox.Clean.Singleton = new Clean.DefinitionModified();
            BeekeepingBox.Feed.Singleton = new Feed.DefinitionModified();
            BeekeepingBox.Harvest.Singleton = new Harvest.DefinitionModified();
            BeekeepingBox.SmokeOut.Singleton = new SmokeOut.DefinitionModified();
            CopyTuning(typeof(BeekeepingBox), typeof(BeekeepingBox.Clean.Definition), typeof(Clean.DefinitionModified));
            CopyTuning(typeof(BeekeepingBox), typeof(BeekeepingBox.Feed.Definition), typeof(Feed.DefinitionModified));
            CopyTuning(typeof(BeekeepingBox), typeof(BeekeepingBox.Harvest.Definition), typeof(Harvest.DefinitionModified));
            CopyTuning(typeof(BeekeepingBox), typeof(BeekeepingBox.SmokeOut.Definition), typeof(SmokeOut.DefinitionModified));
        }

        static ListenerAction OnSimDestroyed(Event e)
        {
            try
            {
                if (e.Actor is Sim sim)
                {
                    EnableBeekeeperOutfit(sim.SimDescription);
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
                foreach (BeekeepingBox beekeepingBox in Sims3.Gameplay.Queries.GetObjects<BeekeepingBox>())
                {
                    AddInteractions(beekeepingBox);
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