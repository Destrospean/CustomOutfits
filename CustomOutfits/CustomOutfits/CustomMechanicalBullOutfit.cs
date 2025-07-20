using Destrospean.CustomOutfits;
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
using Tuning = Sims3.Gameplay.Destrospean.CustomOutfits;

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
        static EventListener sSimDestroyedListener;

        [PersistableStatic]
        static EventListener sSimSelectedListener;

        static CustomMechanicalBullOutfit()
        {
            kInstantiator = false;
            sMechanicalBullOutfitDisabledList = new List<ulong>();
            sMechanicalBullSwimwearDisabledList = new List<ulong>();
            sSimDestroyedListener = null;
            sSimSelectedListener = null;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnObjectPlacedInLotEventHandler += OnObjectPlacedInLot;
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
                    return ((target is Sim && actor == target && Tuning.kShowSimMenu) || (!(target is Sim) && Tuning.kShowObjectMenu)) && actor.SkillManager.HasElement(SkillNames.Athletic) && actor.SkillManager.GetElement(SkillNames.Athletic).SkillLevel >= MechanicalBull.kSkillForCowboyOutfit && actor.SimDescription.YoungAdultOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public override bool Run()
            {
                return Common.EditSpecialOutfit(Actor, sLocalizationKey, kMechanicalBullSpecialOutfitKey, GetMechanicalBullOutfitName(Actor), ProductVersion.EP6);
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
                    return ((target is Sim && actor == target && Tuning.kShowSimMenu) || (!(target is Sim) && Tuning.kShowObjectMenu)) && actor.SkillManager.HasElement(SkillNames.Athletic) && actor.SkillManager.GetElement(SkillNames.Athletic).SkillLevel >= MechanicalBull.kSkillForCowboyOutfit && actor.SimDescription.YoungAdultOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous && actor.SimDescription.HasSpecialOutfit(kMechanicalBullSpecialOutfitKey);
                }
            }

            public override bool Run()
            {
                Actor.SimDescription.RemoveSpecialOutfit(kMechanicalBullSpecialOutfitKey);
                Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
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
                        interactionSubstring += " " + EAText.GetMoneyString(MechanicalBull.kCostOfRide);
                    }
                    return interactionSubstring;
                }

                public override bool Test(Sim actor, MechanicalBull target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (target.mCurrentRider != null)
                    {
                        return false;
                    }
                    if (ShouldChargeForRide(actor, target) && actor.FamilyFunds < MechanicalBull.kCostOfRide)
                    {
                        greyedOutTooltipCallback = CreateTooltipCallback(LocalizationHelper.InsufficientFunds);
                        return false;
                    }
                    if (actor.BuffManager.HasElement(BuffNames.Fatigued))
                    {
                        greyedOutTooltipCallback = () => MechanicalBull.LocalizeString("FailRideBullFatigue", actor);
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
                    if (Actor.FamilyFunds < MechanicalBull.kCostOfRide)
                    {
                        Actor.AddExitReason(ExitReason.FailedToStart);
                        return false;
                    }
                    Actor.ModifyFunds(-MechanicalBull.kCostOfRide);
                }
                Target.mCurrentRider = Actor;
                StandardEntry();
                if (GetMechanicalBullOutfitEnabled(Actor.SimDescription) && skillLevel >= MechanicalBull.kSkillForCowboyOutfit)
                {
                    changeClothesState = ChangeClothesState.Cowboy;
                }
                if (GetMechanicalBullSwimwearEnabled(Actor.SimDescription) && (Actor.TraitManager.HasElement(TraitNames.Dramatic) || Actor.TraitManager.HasElement(TraitNames.Inappropriate)))
                {
                    float roll = RandomUtil.GetFloat(100f);
                    if (roll < MechanicalBull.kSwimwearPercent)
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
                            SimOutfit resultOutfit, uniform;
                            if (Actor.SimDescription.HasSpecialOutfit(kMechanicalBullSpecialOutfitKey))
                            {
                                Actor.SwitchToOutfitWithSpin(Actor.SimDescription.GetSpecialOutfit(kMechanicalBullSpecialOutfitKey).Key);
                            }
                            else if (OutfitUtils.TryGenerateSimOutfit(GetMechanicalBullOutfitName(Actor), ProductVersion.EP6, out uniform))
                            {
                                if (OutfitUtils.TryApplyUniformToOutfit(Actor.SimDescription.GetOutfit(OutfitCategories.Everyday, 0), uniform, Actor.SimDescription, "MechanicalBull.ApplyCowboyOutfit", out resultOutfit))
                                {
                                    Actor.SimDescription.AddSpecialOutfit(resultOutfit, kMechanicalBullSpecialOutfitKey);
                                    Actor.SwitchToOutfitWithSpin(resultOutfit.Key);
                                }
                            }
                            break;
                        }
                }
                if (RandomUtil.RandomChance(MechanicalBull.kProbabilityOfFail[(uint)mDifficulty] - skillLevel * MechanicalBull.kPercentPerLevel))
                {
                    mRideEnding = RideEndingState.Fail;
                    if (RandomUtil.RandomChance(MechanicalBull.kBaseEpicFailChance - MechanicalBull.kPercentPerLevelEpicFail * skillLevel))
                    {
                        mRideEnding = RideEndingState.EpicFail;
                    }
                    mLengthOfRide = RandomUtil.GetFloat(MechanicalBull.kRideLength[(uint)mDifficulty]) * (1f + skillLevel / 10f);
                    mLengthOfRide = Math.Min(mLengthOfRide, MechanicalBull.kRideLength[(uint)mDifficulty]);
                }
                else
                {
                    mLengthOfRide = MechanicalBull.kRideLength[(uint)mDifficulty];
                }
                mBroadcasterParams.PulseRadius += MechanicalBull.kPulseRadiusIncreasePerSkillLevel * skillLevel;
                mBroadcasterParams.MaxSimsToProcessPerTick += MechanicalBull.kSimsToProcessPerDifficultyLevel * (int)mDifficulty;
                if (changeClothesState == ChangeClothesState.Swimwear || Actor.CurrentOutfitCategory == OutfitCategories.Swimwear)
                {
                    Actor.BuffManager.AddElement(BuffNames.AttentionFrenzy, Origin.FromRidingBull);
                    mBroadcasterParams.MaxSimsToProcessPerTick += MechanicalBull.kSimsIncreaseForFrenzy;
                }
                mBroadcaster = new ReactionBroadcaster(Target, mBroadcasterParams, MechanicalBull.WatchBull.Singleton);
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
                    return ((target is Sim && actor == target && Tuning.kShowSimMenu) || (!(target is Sim) && Tuning.kShowObjectMenu)) && actor.SkillManager.HasElement(SkillNames.Athletic) && actor.SkillManager.GetElement(SkillNames.Athletic).SkillLevel >= MechanicalBull.kSkillForCowboyOutfit && actor.SimDescription.YoungAdultOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public override bool Run()
            {
                if (GetMechanicalBullOutfitEnabled(Actor.SimDescription))
                {
                    DisableMechanicalBullOutfit(Actor.SimDescription);
                    Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "DisabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                else
                {
                    EnableMechanicalBullOutfit(Actor.SimDescription);
                    Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "EnabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
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
                    return ((target is Sim && actor == target && Tuning.kShowSimMenu) || (!(target is Sim) && Tuning.kShowObjectMenu)) && actor.SkillManager.HasElement(SkillNames.Athletic) && actor.SkillManager.GetElement(SkillNames.Athletic).SkillLevel >= MechanicalBull.kSkillForCowboyOutfit && actor.SimDescription.YoungAdultOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous && (actor.TraitManager.HasElement(TraitNames.Dramatic) || actor.TraitManager.HasElement(TraitNames.Inappropriate));
                }
            }

            public override bool Run()
            {
                if (GetMechanicalBullSwimwearEnabled(Actor.SimDescription))
                {
                    DisableMechanicalBullSwimwear(Actor.SimDescription);
                    Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "DisabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                else
                {
                    EnableMechanicalBullSwimwear(Actor.SimDescription);
                    Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "EnabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                return true;
            }
        }

        static void AddInteractions(GameObject gameObject)
        {
            if (gameObject != null && !gameObject.Interactions.Exists(interaction => interaction.InteractionDefinition.GetType() == EditMechanicalBullOutfit.Singleton.GetType()) && GameUtils.IsInstalled(ProductVersion.EP6))
            {
                gameObject.AddInteraction(EditMechanicalBullOutfit.Singleton);
                gameObject.AddInteraction(ResetMechanicalBullOutfit.Singleton);
                gameObject.AddInteraction(ToggleMechanicalBullOutfit.Singleton);
                gameObject.AddInteraction(ToggleMechanicalBullSwimwear.Singleton);
            }
        }

        static void DisableMechanicalBullOutfit(SimDescription simDescription)
        {
            if (GetMechanicalBullOutfitEnabled(simDescription))
            {
                sMechanicalBullOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
            UpdateListeners();
        }

        static void DisableMechanicalBullSwimwear(SimDescription simDescription)
        {
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

        static void OnObjectPlacedInLot(object sender, EventArgs e)
        {
            if (e is World.OnObjectPlacedInLotEventArgs)
            {
                GameObject gameObject = GameObject.GetObject(((World.OnObjectPlacedInLotEventArgs)e).ObjectId);
                if (gameObject is MechanicalBull)
                {
                    AddInteractions(gameObject);
                }
            }
        }

        static void OnPreLoad()
        {
            MechanicalBull.RideBull.Singleton = new RideBull.DefinitionModified();
            Common.CopyTuning(typeof(MechanicalBull), typeof(MechanicalBull.RideBull.Definition), typeof(RideBull.DefinitionModified));
        }

        static ListenerAction OnSimDestroyed(Event e)
        {
            try
            {
                if (e.Actor is Sim)
                {
                    EnableMechanicalBullOutfit(e.Actor.SimDescription);
                    EnableMechanicalBullSwimwear(e.Actor.SimDescription);
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
            new List<MechanicalBull>(Sims3.Gameplay.Queries.GetObjects<MechanicalBull>()).ForEach(AddInteractions);
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