using System;
using System.Collections.Generic;
using Destrospean.CustomOutfits;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.HobbiesSkills;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Tutorial;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using Tuning = Sims3.Gameplay.Destrospean.CustomOutfits;

namespace Destrospean
{
    public class CustomChemistryLabOutfit
    {
        public static readonly string kChemistryLabSpecialOutfitKey = "ChemistryLab";

        [Tunable]
        protected static bool kInstantiator;

        [PersistableStatic(true)]
        static List<ulong> sChemistryLabOutfitDisabledList;

        static EventListener sSimDestroyedListener, sSimSelectedListener;

        static CustomChemistryLabOutfit()
        {
            kInstantiator = false;
            sChemistryLabOutfitDisabledList = new List<ulong>();
            sSimDestroyedListener = null;
            sSimSelectedListener = null;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnObjectPlacedInLotEventHandler += OnObjectPlacedInLot;
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            World.sOnWorldQuitEventHandler += OnWorldQuit;
        }

        public class DiscoverPotion : ChemistryLab.DiscoverPotion
        {
            public class DefinitionModified : InteractionDefinition<Sim, ChemistryLab, DiscoverPotion>
            {
                Definition mDefinitionBase = new Definition();

                public override string GetInteractionName(Sim actor, ChemistryLab target, InteractionObjectPair interaction)
                {
                    return mDefinitionBase.GetInteractionName(actor, target, interaction);
                }

                public override bool Test(Sim actor, ChemistryLab target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return mDefinitionBase.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
                }
            }

            public override void Cleanup()
            {
                if (mNeedToSwitchOutfitOnFinish && mOutfit.Key == Actor.SimDescription.GetOutfit(OutfitCategories.Career, Actor.SimDescription.GetOutfitCount(OutfitCategories.Career) - 1).Key)
                {
                    if (Actor.CurrentOutfitCategory == OutfitCategories.Career)
                    {
                        Actor.SwitchToPreviousOutfitWithSpin();
                    }
                    else
                    {
                        Actor.SwitchToOutfitWithoutSpin(Sim.ClothesChangeReason.Force, OutfitCategories.Everyday);
                    }
                    Actor.SimDescription.RemoveOutfit(OutfitCategories.Career, Actor.SimDescription.GetOutfitCount(OutfitCategories.Career) - 1, true);
                    mOutfit = null;
                    mNeedToSwitchOutfitOnFinish = false;
                }
                base.Cleanup();
            }

            public override bool Run()
            {
                if (!Target.RouteToChemistryLab(Actor))
                {
                    return false;
                }
                Actor.RefreshCurrentOutfit(false);
                SimOutfit outfit;
                mNeedToSwitchOutfitOnFinish = ChangeSimToChemistryLabOutfit(Actor, out outfit);
                mOutfit = outfit;
                StandardEntry();
                mLogicSkill = Actor.SkillManager.AddElement(SkillNames.Logic) as LogicSkill;
                mResult = PotionResult.Nothing;
                EnterStateMachine("ChemistryLab", "EnterLab", "x", "chemistryLab");
                if (Actor.SimDescription.Child)
                {
                    mStool = GlobalFunctions.CreateObjectOutOfWorld("ChildStool") as GameObject;
                    SetActor("childStool", mStool);
                }
                Target.SetGeometryState("InUse");
                AddSynchronousOneShotScriptEventHandler(101u, StartEvent);
                AddSynchronousOneShotScriptEventHandler(102u, FailEvent);
                AddSynchronousOneShotScriptEventHandler(103u, FinishEvent);
                BeginCommodityUpdates();
                AnimateSim("MakePotionAtLab");
                bool loopDone = DoLoop(ExitReason.Default, DiscoverPotionLoopCallback, mCurrentStateMachine, ChemistryLab.kMinutesPerDiceRoll);
                EndCommodityUpdates(loopDone);
                switch (mResult)
                {
                    case PotionResult.Discovery:
                        mLogicSkill.RegisterPotionDiscovered();
                        mLogicSkill.RegisterPotionMade(Target.mCurrentPotionType);
                        EventTracker.SendEvent(EventTypeId.kDiscoveredAPotion, Actor, Target.mCreatedPotion);
                        EventTracker.SendEvent(EventTypeId.kMadeAPotion, Actor, Target.mCreatedPotion);
                        Tutorialette.TriggerLesson(Lessons.ChemistryLab, Actor);
                        AnimateSim("ExitSucceedDiscovery");
                        break;
                    case PotionResult.Fire:
                        mNeedToSwitchOutfitOnFinish = false;
                        AnimateSim("ExitFail");
                        Actor.ShowTNSIfSelectable(TNSNames.DiscoverPotionFail, Target, Actor, null, Actor.IsFemale, Actor.IsFemale, Actor.SimDescription);
                        PetStartleBehavior.CheckForStartle(Target, StartleType.PotionFailExplosion);
                        break;
                    case PotionResult.Nothing:
                        AnimateSim("ExitCancel");
                        break;
                }
                if (mNeedToSwitchOutfitOnFinish && mOutfit.Key == Actor.SimDescription.GetOutfit(OutfitCategories.Career, Actor.SimDescription.GetOutfitCount(OutfitCategories.Career) - 1).Key)
                {
                    if (Actor.CurrentOutfitCategory == OutfitCategories.Career)
                    {
                        Actor.SwitchToPreviousOutfitWithSpin();
                    }
                    else
                    {
                        Actor.SwitchToOutfitWithoutSpin(Sim.ClothesChangeReason.Force, OutfitCategories.Everyday);
                    }
                    Actor.SimDescription.RemoveOutfit(OutfitCategories.Career, Actor.SimDescription.GetOutfitCount(OutfitCategories.Career) - 1, true);
                    mOutfit = null;
                    mNeedToSwitchOutfitOnFinish = false;
                }
                StandardExit();
                return loopDone;
            }
        }

        public class EditChemistryLabOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomChemistryLabOutfit/EditChemistryLabOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditChemistryLabOutfit>
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
                    return ((target is Sim && actor == target && Tuning.kShowSimMenu) || (!(target is Sim) && Tuning.kShowObjectMenu)) && actor.SimDescription.ChildOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public override bool Run()
            {
                return Common.EditSpecialOutfit(Actor, sLocalizationKey, kChemistryLabSpecialOutfitKey, GetChemistryLabOutfitName(Actor), ProductVersion.EP4);
            }
        }

        public class MakePotion : ChemistryLab.MakePotion
        {
            public class DefinitionModified : InteractionDefinition<Sim, ChemistryLab, MakePotion>
            {
                public bool IsContinuation;

                public LogicSkill.PotionType CurrPotionType;

                public DefinitionModified()
                {
                }

                public DefinitionModified(LogicSkill.PotionType potionType, bool isContinuation)
                {
                    IsContinuation = isContinuation;
                    CurrPotionType = potionType;
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, ChemistryLab target, List<InteractionObjectPair> results)
                {
                    LogicSkill skill = actor.SkillManager.GetSkill<LogicSkill>(SkillNames.Logic);
                    if (skill == null)
                    {
                        return;
                    }
                    if (target.mPotionProgress > 0f && !target.IsActorUsingMe(actor))
                    {
                        results.Add(new InteractionObjectPair(new DefinitionModified(target.mCurrentPotionType, true), interaction.Target));
                        return;
                    }
                    foreach (LogicSkill.PotionType discoveredPotionType in skill.DiscoveredPotionTypes)
                    {
                        results.Add(new InteractionObjectPair(new DefinitionModified(discoveredPotionType, false), interaction.Target));
                    }
                }

                public override string GetInteractionName(Sim actor, ChemistryLab target, InteractionObjectPair interaction)
                {
                    if (IsContinuation)
                    {
                        return ChemistryLab.LocalizeString(actor.IsFemale, "Continue", Localization.LocalizeString(Potion.GetPotionLocKey(CurrPotionType)), target.GetRoundedPercentComplete());
                    }
                    return ChemistryLab.LocalizeString(actor.IsFemale, "MakeInteractionName", Localization.LocalizeString(Potion.GetPotionLocKey(CurrPotionType)), Potion.GetPotionTypeCost(actor, CurrPotionType));
                }

                public override string[] GetPath(bool isFemale)
                {
                    if (IsContinuation)
                    {
                        return new string[0];
                    }
                    return new string[]
                    {
                        ChemistryLab.LocalizeString(isFemale, "MakePotionPath")
                    };
                }

                public override bool Test(Sim actor, ChemistryLab target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    Skill logicSkill = actor.SkillManager.GetElement(SkillNames.Logic);
                    if ((target.InUse && !target.IsActorUsingMe(actor)) || !(logicSkill is LogicSkill) || ((LogicSkill)logicSkill).DiscoveredPotionTypes.Count == 0)
                    {
                        return false;
                    }
                    if (IsContinuation)
                    {
                        if (!((LogicSkill)logicSkill).DiscoveredPotionTypes.Contains(target.mCurrentPotionType))
                        {
                            greyedOutTooltipCallback = CreateTooltipCallback(ChemistryLab.LocalizeString(actor.IsFemale, "DoNotKnowHowToMake", actor.SimDescription));
                            return false;
                        }
                    }
                    else
                    {
                        if (actor.FamilyFunds < Potion.GetPotionTypeCost(actor, CurrPotionType))
                        {
                            greyedOutTooltipCallback = CreateTooltipCallback(ChemistryLab.LocalizeString(actor.IsFemale, "NotEnoughMoney"));
                            return false;
                        }
                        if (actor.CurrentOutfitCategory == OutfitCategories.Singed)
                        {
                            greyedOutTooltipCallback = CreateTooltipCallback(ChemistryLab.LocalizeString(actor.IsFemale, "DisallowedSingedTooltip"));
                            return false;
                        }
                        if (actor.CurrentOutfitCategory == OutfitCategories.SkinnyDippingTowel)
                        {
                            greyedOutTooltipCallback = CreateTooltipCallback(ChemistryLab.LocalizeString(actor.IsFemale, "DisallowedInTowelTooltip"));
                            return false;
                        }
                    }
                    return true;
                }
            }

            public override void Init(ref InteractionInstanceParameters parameters)
            {
                DefinitionModified definition = parameters.InteractionDefinition as DefinitionModified;
                mLogicSkill = parameters.Actor.SkillManager.AddElement(SkillNames.Logic) as LogicSkill;
                if (parameters.Autonomous)
                {
                    ChemistryLab chemistryLab = parameters.Target as ChemistryLab;
                    if (chemistryLab.mPotionProgress > 0f && !chemistryLab.IsActorUsingMe(parameters.Actor as Sim))
                    {
                        definition.IsContinuation = true;
                    }
                    else
                    {
                        definition.CurrPotionType = RandomUtil.GetRandomObjectFromList(mLogicSkill.DiscoveredPotionTypes);
                    }
                }
                base.Init(ref parameters);
            }

            public override void Cleanup()
            {
                if (mNeedToSwitchOutfitOnFinish && mOutfit.Key == Actor.SimDescription.GetOutfit(OutfitCategories.Career, Actor.SimDescription.GetOutfitCount(OutfitCategories.Career) - 1).Key)
                {
                    if (Actor.CurrentOutfitCategory == OutfitCategories.Career)
                    {
                        Actor.SwitchToPreviousOutfitWithSpin();
                    }
                    else
                    {
                        Actor.SwitchToOutfitWithoutSpin(Sim.ClothesChangeReason.Force, OutfitCategories.Everyday);
                    }
                    Actor.SimDescription.RemoveOutfit(OutfitCategories.Career, Actor.SimDescription.GetOutfitCount(OutfitCategories.Career) - 1, true);
                    mOutfit = null;
                    mNeedToSwitchOutfitOnFinish = false;
                }
                base.Cleanup();
            }

            public override bool Run()
            {
                DefinitionModified definition = InteractionDefinition as DefinitionModified;
                LogicSkill.PotionType currPotionType = definition.CurrPotionType;
                int potionTypeCost = Potion.GetPotionTypeCost(Actor, currPotionType);
                if (Actor.FamilyFunds < potionTypeCost || !Target.RouteToChemistryLab(Actor))
                {
                    return false;
                }
                if (!definition.IsContinuation)
                {
                    Actor.ModifyFunds(-potionTypeCost);
                }
                Target.mCurrentPotionType = currPotionType;
                Actor.RefreshCurrentOutfit(false);
                SimOutfit outfit;
                mNeedToSwitchOutfitOnFinish = ChangeSimToChemistryLabOutfit(Actor, out outfit);
                mOutfit = outfit;
                Target.RemovePlaceholderPotion();
                mTotalTime = GetTimeToCompletion();
                StandardEntry();
                StartStages();
                EnterStateMachine("ChemistryLab", "EnterLab", "x", "chemistryLab");
                if (Actor.SimDescription.Child)
                {
                    mStool = GlobalFunctions.CreateObjectOutOfWorld("ChildStool") as GameObject;
                    SetActor("childStool", mStool);
                }
                if (!Target.CreatePotion(currPotionType, Actor))
                {
                    return false;
                }
                SetActor("potionFlask", Target.mCreatedPotion);
                Target.SetGeometryState("InUse");
                AddSynchronousOneShotScriptEventHandler(101u, StartEvent);
                AddSynchronousOneShotScriptEventHandler(103u, FinishEvent);
                BeginCommodityUpdates();
                AnimateSim("MakePotionAtLab");
                bool loopDone = DoLoop(ExitReason.Default, MakePotionLoopCallback, mCurrentStateMachine);
                EndCommodityUpdates(loopDone);
                if (Actor.HasExitReason(ExitReason.Finished))
                {
                    EventTracker.SendEvent(EventTypeId.kMadeAPotion, Actor, Target.mCreatedPotion);
                    mLogicSkill.RegisterPotionMade(currPotionType);
                    AnimateSim("ExitSucceedMakePotion");
                }
                else
                {
                    AnimateSim("ExitCancel");
                    if (Target.mPotionProgress > 0f)
                    {
                        Target.AddPlaceholderPotion();
                    }
                }
                if (mNeedToSwitchOutfitOnFinish && mOutfit.Key == Actor.SimDescription.GetOutfit(OutfitCategories.Career, Actor.SimDescription.GetOutfitCount(OutfitCategories.Career) - 1).Key)
                {
                    if (Actor.CurrentOutfitCategory == OutfitCategories.Career)
                    {
                        Actor.SwitchToPreviousOutfitWithSpin();
                    }
                    else
                    {
                        Actor.SwitchToOutfitWithoutSpin(Sim.ClothesChangeReason.Force, OutfitCategories.Everyday);
                    }
                    Actor.SimDescription.RemoveOutfit(OutfitCategories.Career, Actor.SimDescription.GetOutfitCount(OutfitCategories.Career) - 1, true);
                    mOutfit = null;
                    mNeedToSwitchOutfitOnFinish = false;
                }
                StandardExit();
                return loopDone;
            }
        }

        public class ResetChemistryLabOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomChemistryLabOutfit/ResetChemistryLabOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ResetChemistryLabOutfit>
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
                    return ((target is Sim && actor == target && Tuning.kShowSimMenu) || (!(target is Sim) && Tuning.kShowObjectMenu)) && actor.SimDescription.ChildOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous && actor.SimDescription.HasSpecialOutfit(kChemistryLabSpecialOutfitKey);
                }
            }

            public override bool Run()
            {
                Actor.SimDescription.RemoveSpecialOutfit(kChemistryLabSpecialOutfitKey);
                Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                return true;
            }
        }

        public class ToggleChemistryLabOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomChemistryLabOutfit/ToggleChemistryLabOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ToggleChemistryLabOutfit>
            {
                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    if (GetChemistryLabOutfitEnabled(actor.SimDescription))
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
                    return ((target is Sim && actor == target && Tuning.kShowSimMenu) || (!(target is Sim) && Tuning.kShowObjectMenu)) && actor.SimDescription.ChildOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public override bool Run()
            {
                if (GetChemistryLabOutfitEnabled(Actor.SimDescription))
                {
                    DisableChemistryLabOutfit(Actor.SimDescription);
                    Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "DisabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                else
                {
                    EnableChemistryLabOutfit(Actor.SimDescription);
                    Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "EnabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                return true;
            }
        }

        static void AddInteractions(GameObject gameObject)
        {
            if (gameObject != null && !gameObject.Interactions.Exists(interaction => interaction.InteractionDefinition.GetType() == EditChemistryLabOutfit.Singleton.GetType()) && GameUtils.IsInstalled(ProductVersion.EP4))
            {
                gameObject.AddInteraction(EditChemistryLabOutfit.Singleton);
                gameObject.AddInteraction(ResetChemistryLabOutfit.Singleton);
                gameObject.AddInteraction(ToggleChemistryLabOutfit.Singleton);
            }
        }

        public static bool ChangeSimToChemistryLabOutfit(Sim actor, out SimOutfit outfit)
        {
            SimDescription simDescription = actor.SimDescription;
            if (simDescription.IsPregnant || !GetChemistryLabOutfitEnabled(simDescription))
            {
                outfit = null;
                return false;
            }
            SimOutfit simOutfit;
            if (simDescription.HasSpecialOutfit(kChemistryLabSpecialOutfitKey))
            {
                simDescription.AddOutfit(simDescription.GetSpecialOutfit(kChemistryLabSpecialOutfitKey), OutfitCategories.Career);
                simOutfit = simDescription.GetOutfit(OutfitCategories.Career, simDescription.GetOutfitCount(OutfitCategories.Career) - 1);
            }
            else
            {
                simOutfit = OutfitUtils.CreateOutfitForSim(simDescription, ResourceKey.CreateOutfitKeyFromProductVersion(GetChemistryLabOutfitName(actor), ProductVersion.EP4), OutfitCategories.Career, OutfitCategories.Everyday, true);
            }
            if (simOutfit != null)
            {
                actor.SwitchToOutfitWithSpin(simOutfit.Key);
                outfit = simOutfit;
                return true;
            }
            outfit = null;
            return false;
        }

        static void DisableChemistryLabOutfit(SimDescription simDescription)
        {
            if (GetChemistryLabOutfitEnabled(simDescription))
            {
                sChemistryLabOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
        }

        static void EnableChemistryLabOutfit(SimDescription simDescription)
        {
            if (!GetChemistryLabOutfitEnabled(simDescription))
            {
                sChemistryLabOutfitDisabledList.Remove(simDescription.SimDescriptionId);
            }
        }

        static bool GetChemistryLabOutfitEnabled(SimDescription simDescription)
        {
            return !sChemistryLabOutfitDisabledList.Contains(simDescription.SimDescriptionId);
        }

        public static string GetChemistryLabOutfitName(Sim actor)
        {
            return OutfitUtils.GetAgePrefix(actor.SimDescription.Age, true) + (actor.IsMale ? "m" : "f") + "chemistry";
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
                if (gameObject is ChemistryLab)
                {
                    AddInteractions(gameObject);
                }
            }
        }

        static void OnPreLoad()
        {
            ChemistryLab.DiscoverPotion.Singleton = new DiscoverPotion.DefinitionModified();
            ChemistryLab.MakePotion.Singleton = new MakePotion.DefinitionModified();
            Common.CopyTuning(typeof(ChemistryLab), typeof(ChemistryLab.DiscoverPotion.Definition), typeof(DiscoverPotion.DefinitionModified));
            Common.CopyTuning(typeof(ChemistryLab), typeof(ChemistryLab.MakePotion.Definition), typeof(MakePotion.DefinitionModified));
        }

        static ListenerAction OnSimDestroyed(Event e)
        {
            try
            {
                if (e.TargetObject is Sim)
                {
                    EnableChemistryLabOutfit(((Sim)e.TargetObject).SimDescription);
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
            new List<ChemistryLab>(Sims3.Gameplay.Queries.GetObjects<ChemistryLab>()).ForEach(AddInteractions);
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