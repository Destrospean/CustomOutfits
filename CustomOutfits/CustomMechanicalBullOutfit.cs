using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Entertainment;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.SimIFace.Enums;
using Sims3.UI;
using System;
using System.Collections.Generic;
using static Destrospean.Common;
using static Sims3.Gameplay.Destrospean.CustomOutfits;
using static Sims3.Gameplay.Objects.Entertainment.MechanicalBull;

namespace Destrospean
{
    public class CustomMechanicalBullOutfit
    {
        [Tunable]
        protected static bool kInstantiator;

        public static readonly string kMechanicalBullSpecialOutfitKey = "MechanicalBull";

        [PersistableStatic]
        static List<ulong> sMechanicalBullOutfitDisabledList;

        [PersistableStatic]
        static List<ulong> sMechanicalBullSwimwearDisabledList;

        [PersistableStatic]
        static EventListener sObjectBoughtListener;

        [PersistableStatic]
        static EventListener sSimDestroyedListener;

        [PersistableStatic]
        static EventListener sSimSelectedListener;

        static CustomMechanicalBullOutfit()
        {
            kInstantiator = false;
            sMechanicalBullOutfitDisabledList = new List<ulong>();
            sMechanicalBullSwimwearDisabledList = new List<ulong>();
            sObjectBoughtListener = null;
            sSimDestroyedListener = null;
            sSimSelectedListener = null;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            World.sOnWorldQuitEventHandler += OnWorldQuit;
        }

        public class EditMechanicalBullOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomMechanicalBullOutfit/EditMechanicalBullOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditMechanicalBullOutfit>
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
                    return !((target is Sim && actor != target) || !actor.SkillManager.HasElement(SkillNames.Athletic) || actor.SkillManager.GetElement(SkillNames.Athletic).SkillLevel < kSkillForCowboyOutfit || actor.SimDescription.TeenOrBelow || !actor.SimDescription.IsHuman || actor.SimDescription.IsRobot || isAutonomous);
                }
            }

            public override bool Run()
            {
                return EditSpecialOutfit(Actor, sLocalizationKey, kMechanicalBullSpecialOutfitKey, GetMechanicalBullOutfitName(Actor), ProductVersion.EP6);
            }
        }

        public class ResetMechanicalBullOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomMechanicalBullOutfit/ResetMechanicalBullOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ResetMechanicalBullOutfit>
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
                    return !(!actor.SimDescription.HasSpecialOutfit(kMechanicalBullSpecialOutfitKey) || (target is Sim && actor != target) || !actor.SkillManager.HasElement(SkillNames.Athletic) || actor.SkillManager.GetElement(SkillNames.Athletic).SkillLevel < kSkillForCowboyOutfit || actor.SimDescription.TeenOrBelow || !actor.SimDescription.IsHuman || actor.SimDescription.IsRobot || isAutonomous);
                }
            }

            public override bool Run()
            {
                Actor.SimDescription.RemoveSpecialOutfit(kMechanicalBullSpecialOutfitKey);
                Notify(Localize(Actor.IsFemale, sLocalizationKey + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                return true;
            }
        }

        public class RideBull : MechanicalBull.RideBull
        {
            public class DefinitionModified : InteractionDefinition<Sim, MechanicalBull, RideBull>
            {
                public MechanicalBullDifficulty mDifficulty;

                public DefinitionModified()
                {
                }

                public DefinitionModified(MechanicalBullDifficulty difficulty)
                {
                    mDifficulty = difficulty;
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, MechanicalBull target, List<InteractionObjectPair> results)
                {
                    foreach (MechanicalBullDifficulty difficulty in Enum.GetValues(typeof(MechanicalBullDifficulty)))
                    {
                        results.Add(new InteractionObjectPair(new DefinitionModified(difficulty), target));
                    }
                }

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    RideBull rideBull = new RideBull();
                    rideBull.SetDifficulty(mDifficulty);
                    rideBull.Init(ref parameters);
                    return rideBull;
                }

                public override string GetInteractionName(Sim actor, MechanicalBull target, InteractionObjectPair interaction)
                {
                    string interactionSubstring = "";
                    switch (mDifficulty)
                    {
                        case MechanicalBullDifficulty.EasyRider:
                            interactionSubstring = Localization.LocalizeString("GamePlay/Objects/Entertainment/MechanicalBull/RideBull:EasyRider");
                            break;
                        case MechanicalBullDifficulty.BuckingBronco:
                            interactionSubstring = Localization.LocalizeString("GamePlay/Objects/Entertainment/MechanicalBull/RideBull:BuckingBronco");
                            break;
                        case MechanicalBullDifficulty.CrazyCowboy:
                            interactionSubstring = Localization.LocalizeString(string.Format("GamePlay/Objects/Entertainment/MechanicalBull/RideBull:{0}", actor.IsMale ? "CrazyCowboy" : "CrazyCowgirl"));
                            break;
                    }
                    if (ShouldChargeForRide(actor, target))
                    {
                        interactionSubstring += " " + EAText.GetMoneyString(kCostOfRide);
                    }
                    return interactionSubstring;
                }

                public override bool Test(Sim actor, MechanicalBull target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (target.mCurrentRider != null)
                    {
                        return false;
                    }
                    if (ShouldChargeForRide(actor, target) && actor.FamilyFunds < kCostOfRide)
                    {
                        greyedOutTooltipCallback = CreateTooltipCallback(LocalizationHelper.InsufficientFunds);
                        return false;
                    }
                    if (actor.BuffManager.HasElement(BuffNames.Fatigued))
                    {
                        greyedOutTooltipCallback = () => LocalizeString("FailRideBullFatigue", actor);
                        return false;
                    }
                    if (actor.TraitManager.HasElement(TraitNames.Daredevil))
                    {
                        return mDifficulty == MechanicalBullDifficulty.CrazyCowboy;
                    }
                    return true;
                }
            }

            public override bool Run()
            {
                Skill skill = Actor.SkillManager.AddElement(SkillNames.Athletic);
                int skillLevel = skill.SkillLevel;
                bool shouldChargeForRide = ShouldChargeForRide(Actor, Target);
                ChangeClothesState changeClothesState = ChangeClothesState.None;
                mClumsy = Actor.TraitManager.HasElement(TraitNames.Clumsy);
                mUnlucky = Actor.TraitManager.HasElement(TraitNames.Unlucky);
                if (!Actor.RouteToSlotAndCheckInUse(Target, Slot.RoutingSlot_0))
                {
                    Actor.AddExitReason(ExitReason.FailedToStart);
                    return false;
                }
                if (shouldChargeForRide)
                {
                    if (Actor.FamilyFunds < kCostOfRide)
                    {
                        Actor.AddExitReason(ExitReason.FailedToStart);
                        return false;
                    }
                    Actor.ModifyFunds(-kCostOfRide);
                }
                Target.mCurrentRider = Actor;
                StandardEntry();
                if (GetMechanicalBullOutfitEnabled(Actor.SimDescription) && skillLevel >= kSkillForCowboyOutfit)
                {
                    changeClothesState = ChangeClothesState.Cowboy;
                }
                if (GetMechanicalBullSwimwearEnabled(Actor.SimDescription) && (Actor.TraitManager.HasElement(TraitNames.Dramatic) || Actor.TraitManager.HasElement(TraitNames.Inappropriate)))
                {
                    float roll = RandomUtil.GetFloat(100f);
                    if (roll < kSwimwearPercent)
                    {
                        changeClothesState = ChangeClothesState.Swimwear;
                    }
                }
                Actor.RefreshCurrentOutfit(false);
                switch (changeClothesState)
                {
                    case ChangeClothesState.Swimwear:
                        Actor.SwitchToOutfitWithSpin(Sim.ClothesChangeReason.GoingToSwim);
                        break;
                    case ChangeClothesState.Cowboy:
                        {
                            if (Actor.SimDescription.HasSpecialOutfit(kMechanicalBullSpecialOutfitKey))
                            {
                                Actor.SwitchToOutfitWithSpin(Actor.SimDescription.GetSpecialOutfit(kMechanicalBullSpecialOutfitKey).Key);
                            }
                            else if (OutfitUtils.TryGenerateSimOutfit(GetMechanicalBullOutfitName(Actor), ProductVersion.EP6, out var uniform))
                            {
                                if (OutfitUtils.TryApplyUniformToOutfit(Actor.SimDescription.GetOutfit(OutfitCategories.Everyday, 0), uniform, Actor.SimDescription, "MechanicalBull.ApplyCowboyOutfit", out var resultOutfit))
                                {
                                    Actor.SimDescription.AddSpecialOutfit(resultOutfit, kMechanicalBullSpecialOutfitKey);
                                    Actor.SwitchToOutfitWithSpin(resultOutfit.Key);
                                }
                            }
                            break;
                        }
                }
                if (RandomUtil.RandomChance(kProbabilityOfFail[(uint)mDifficulty] - skillLevel * kPercentPerLevel))
                {
                    mRideEnding = RideEndingState.Fail;
                    if (RandomUtil.RandomChance(kBaseEpicFailChance - kPercentPerLevelEpicFail * skillLevel))
                    {
                        mRideEnding = RideEndingState.EpicFail;
                    }
                    mLengthOfRide = RandomUtil.GetFloat(kRideLength[(uint)mDifficulty]) * (1f + skillLevel / 10f);
                    mLengthOfRide = Math.Min(mLengthOfRide, kRideLength[(uint)mDifficulty]);
                }
                else
                {
                    mLengthOfRide = kRideLength[(uint)mDifficulty];
                }
                mBroadcasterParams.PulseRadius += kPulseRadiusIncreasePerSkillLevel * skillLevel;
                mBroadcasterParams.MaxSimsToProcessPerTick += kSimsToProcessPerDifficultyLevel * (int)mDifficulty;
                if (changeClothesState == ChangeClothesState.Swimwear || Actor.CurrentOutfitCategory == OutfitCategories.Swimwear)
                {
                    Actor.BuffManager.AddElement(BuffNames.AttentionFrenzy, Origin.FromRidingBull);
                    mBroadcasterParams.MaxSimsToProcessPerTick += kSimsIncreaseForFrenzy;
                }
                mBroadcaster = new ReactionBroadcaster(Target, mBroadcasterParams, WatchBull.Singleton);
                mStartTime = SimClock.ElapsedTime(TimeUnit.Minutes);
                AcquireStateMachine("MechanicalBull");
                SetParameter("AthleticSkill", skill.GetSkillLevelParameterForJazzGraph());
                SetParameter("speed", mDifficulty);
                SetParameter("clumsy", mClumsy);
                SetParameter("unlucky", mUnlucky);
                SetActor("x", Actor);
                SetActor("Bull", Target);
                EnterState("x", "Enter");
                AddOneShotScriptEventHandler(1001u, StartAudioLoop);
                AddOneShotScriptEventHandler(1002u, StopAudioLoop);
                AnimateSim("ridebull");
                BeginCommodityUpdates();
                bool loopDone = DoLoop(ExitReason.Default, Loop, null);
                ExitBull(loopDone);
                Simulator.Sleep(1u);
                EndCommodityUpdates(loopDone);
                if (changeClothesState != 0)
                {
                    Actor.SwitchToPreviousOutfitWithSpin();
                }
                switch (mDifficulty)
                {
                    case MechanicalBullDifficulty.EasyRider:
                        EventTracker.SendEvent(EventTypeId.kRideMechanicalBullOnEasyRider, Actor);
                        break;
                    case MechanicalBullDifficulty.BuckingBronco:
                        EventTracker.SendEvent(EventTypeId.kRideMechanicalBullOnBuckingBronco, Actor);
                        break;
                    case MechanicalBullDifficulty.CrazyCowboy:
                        EventTracker.SendEvent(EventTypeId.kRideMechanicalBullOnCrazyCowboy, Actor);
                        break;
                }
                switch (mRideEnding)
                {
                    case RideEndingState.Success:
                        Actor.BuffManager.RemoveElement(BuffNames.PwnedByBull);
                        switch (mDifficulty)
                        {
                            case MechanicalBullDifficulty.EasyRider:
                                Actor.BuffManager.AddElement(BuffNames.ConqueredTheBull, Origin.FromConquerBullEasy);
                                break;
                            case MechanicalBullDifficulty.BuckingBronco:
                                Actor.BuffManager.AddElement(BuffNames.ConqueredTheBull, Origin.FromConquerBullMedium);
                                break;
                            case MechanicalBullDifficulty.CrazyCowboy:
                                Actor.BuffManager.AddElement(BuffNames.ConqueredTheBull, Origin.FromConquerBullHard);
                                break;
                        }
                        break;
                    case RideEndingState.EpicFail:
                        if (!Actor.TraitManager.HasElement(TraitNames.Daredevil))
                        {
                            Actor.BuffManager.RemoveElement(BuffNames.ConqueredTheBull);
                            Actor.BuffManager.AddElement(BuffNames.PwnedByBull, Origin.FromRidingBull);
                        }
                        break;
                }
                StandardExit();
                Actor.BuffManager.RemoveElement(BuffNames.AttentionFrenzy);
                return loopDone;
            }
        }

        public class ToggleMechanicalBullOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomMechanicalBullOutfit/ToggleMechanicalBullOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ToggleMechanicalBullOutfit>
            {
                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    if (GetMechanicalBullOutfitEnabled(actor.SimDescription))
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
                    return !((target is Sim && actor != target) || !actor.SkillManager.HasElement(SkillNames.Athletic) || actor.SkillManager.GetElement(SkillNames.Athletic).SkillLevel < kSkillForCowboyOutfit || actor.SimDescription.TeenOrBelow || !actor.SimDescription.IsHuman || actor.SimDescription.IsRobot || isAutonomous);
                }
            }

            public override bool Run()
            {
                if (GetMechanicalBullOutfitEnabled(Actor.SimDescription))
                {
                    DisableMechanicalBullOutfit(Actor.SimDescription);
                    Notify(Localize(Actor.IsFemale, sLocalizationKey + "DisabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                else
                {
                    EnableMechanicalBullOutfit(Actor.SimDescription);
                    Notify(Localize(Actor.IsFemale, sLocalizationKey + "EnabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                return true;
            }
        }

        public class ToggleMechanicalBullSwimwear : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomMechanicalBullOutfit/ToggleMechanicalBullSwimwear/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ToggleMechanicalBullSwimwear>
            {
                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    if (GetMechanicalBullSwimwearEnabled(actor.SimDescription))
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
                    return !(!(actor.TraitManager.HasElement(TraitNames.Dramatic) || actor.TraitManager.HasElement(TraitNames.Inappropriate)) || (target is Sim && actor != target) || actor.SimDescription.TeenOrBelow || !actor.SimDescription.IsHuman || actor.SimDescription.IsRobot || isAutonomous);
                }
            }

            public override bool Run()
            {
                if (GetMechanicalBullSwimwearEnabled(Actor.SimDescription))
                {
                    DisableMechanicalBullSwimwear(Actor.SimDescription);
                    Notify(Localize(Actor.IsFemale, sLocalizationKey + "DisabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                else
                {
                    EnableMechanicalBullSwimwear(Actor.SimDescription);
                    Notify(Localize(Actor.IsFemale, sLocalizationKey + "EnabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                return true;
            }
        }

        static void AddInteractions(GameObject gameObject)
        {
            if (gameObject == null || !GameUtils.IsInstalled(ProductVersion.EP6))
            {
                return;
            }
            foreach (InteractionObjectPair interaction in gameObject.Interactions)
            {
                if (interaction.InteractionDefinition.GetType() == EditMechanicalBullOutfit.Singleton.GetType())
                {
                    return;
                }
            }
            gameObject.AddInteraction(EditMechanicalBullOutfit.Singleton);
            gameObject.AddInteraction(ResetMechanicalBullOutfit.Singleton);
            gameObject.AddInteraction(ToggleMechanicalBullOutfit.Singleton);
            gameObject.AddInteraction(ToggleMechanicalBullSwimwear.Singleton);
        }

        static void DisableMechanicalBullOutfit(SimDescription simDescription)
        {
            if (sMechanicalBullOutfitDisabledList == null)
            {
                sMechanicalBullOutfitDisabledList = new List<ulong>();
            }
            if (GetMechanicalBullOutfitEnabled(simDescription))
            {
                sMechanicalBullOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
            UpdateListeners();
        }

        static void DisableMechanicalBullSwimwear(SimDescription simDescription)
        {
            if (sMechanicalBullSwimwearDisabledList == null)
            {
                sMechanicalBullSwimwearDisabledList = new List<ulong>();
            }
            if (GetMechanicalBullSwimwearEnabled(simDescription))
            {
                sMechanicalBullSwimwearDisabledList.Add(simDescription.SimDescriptionId);
            }
            UpdateListeners();
        }

        static void EnableMechanicalBullOutfit(SimDescription simDescription)
        {
            if (!GetMechanicalBullOutfitEnabled(simDescription))
            {
                sMechanicalBullOutfitDisabledList.Remove(simDescription.SimDescriptionId);
                UpdateListeners();
            }
        }

        static void EnableMechanicalBullSwimwear(SimDescription simDescription)
        {
            if (!GetMechanicalBullSwimwearEnabled(simDescription))
            {
                sMechanicalBullSwimwearDisabledList.Remove(simDescription.SimDescriptionId);
                UpdateListeners();
            }
        }

        static bool GetMechanicalBullOutfitEnabled(SimDescription simDescription)
        {
            return !sMechanicalBullOutfitDisabledList.Contains(simDescription.SimDescriptionId);
        }

        public static string GetMechanicalBullOutfitName(Sim actor)
        {
            return "A" + (actor.IsMale ? "m" : "f") + "Cowboy";
        }

        static bool GetMechanicalBullSwimwearEnabled(SimDescription simDescription)
        {
            return !sMechanicalBullSwimwearDisabledList.Contains(simDescription.SimDescriptionId);
        }

        static void Init()
        {
            UpdateListeners();
        }

        static ListenerAction OnObjectBought(Event e)
        {
            try
            {
                if (kShowObjectMenu && e.TargetObject is MechanicalBull mechanicalBull)
                {
                    AddInteractions(mechanicalBull);
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
            MechanicalBull.RideBull.Singleton = new RideBull.DefinitionModified();
            CopyTuning(typeof(MechanicalBull), typeof(MechanicalBull.RideBull.Definition), typeof(RideBull.DefinitionModified));
        }

        static ListenerAction OnSimDestroyed(Event e)
        {
            try
            {
                if (e.Actor is Sim sim)
                {
                    EnableMechanicalBullOutfit(sim.SimDescription);
                    EnableMechanicalBullSwimwear(sim.SimDescription);
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
                foreach (MechanicalBull mechanicalBull in Sims3.Gameplay.Queries.GetObjects<MechanicalBull>())
                {
                    AddInteractions(mechanicalBull);
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