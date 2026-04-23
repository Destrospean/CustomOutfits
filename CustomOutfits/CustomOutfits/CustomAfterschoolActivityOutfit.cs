using System;
using System.Collections.Generic;
using Destrospean.CustomOutfits;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Careers;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.RabbitHoles;
using Sims3.Gameplay.Objects.ShelvesStorage;
using Sims3.Gameplay.Skills;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using Tuning = Sims3.Gameplay.Destrospean.CustomOutfits;

namespace Destrospean
{
    public class CustomAfterschoolActivityOutfit
    {
        [Tunable]
        protected static bool kInstantiator;

        [PersistableStatic(true)]
        static List<ulong> sBalletOutfitDisabledList;

        [PersistableStatic(true)]
        static List<ulong> sScoutsOutfitDisabledList;

        static EventListener sSimDescriptionDisposedListener, sSimSelectedListener;

        static CustomAfterschoolActivityOutfit()
        {
            sBalletOutfitDisabledList = new List<ulong>();
            sScoutsOutfitDisabledList = new List<ulong>();
            sSimDescriptionDisposedListener = null;
            sSimSelectedListener = null;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnObjectPlacedInLotEventHandler += OnObjectPlacedInLot;
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            World.sOnWorldQuitEventHandler += OnWorldQuit;
        }

        public class AttendAudition : SchoolRabbitHole.AttendAudition
        {
            public class DefinitionModified : InteractionDefinition<Sim, SchoolRabbitHole, AttendAudition>, IOverridesVisualType
            {
                Definition mDefinitionBase = new Definition();

                public InteractionVisualTypes GetVisualType
                {
                    get
                    {
                        return InteractionVisualTypes.Opportunity;
                    }
                }

                public override string GetInteractionName(Sim actor, SchoolRabbitHole target, InteractionObjectPair interaction)
                {
                    return mDefinitionBase.GetInteractionName(actor, target, interaction);
                }

                public override bool Test(Sim actor, SchoolRabbitHole target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return mDefinitionBase.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
                }
            }

            public override bool BeforeRouting(Sim actor)
            {
                SimDescription simDescription = actor.SimDescription;
                string outfitName = "";
                if (simDescription.Child)
                {
                    AfterschoolActivityType? afterschoolActivityType = null;
                    if (AfterschoolActivity.HasAfterschoolActivityOfType(actor, AfterschoolActivityType.Ballet))
                    {
                        afterschoolActivityType = AfterschoolActivityType.Ballet;
                        outfitName = GetAfterschoolActivityOutfitName(actor, AfterschoolActivityType.Ballet);
                    }
                    else if (AfterschoolActivity.HasAfterschoolActivityOfType(actor, AfterschoolActivityType.Scouts))
                    {
                        afterschoolActivityType = AfterschoolActivityType.Scouts;
                        outfitName = GetAfterschoolActivityOutfitName(actor, AfterschoolActivityType.Scouts);
                    }
                    if (afterschoolActivityType != null && GetAfterschoolActivityOutfitEnabled(simDescription, afterschoolActivityType))
                    {
                        if (simDescription.HasSpecialOutfit(outfitName))
                        {
                            simDescription.AddOutfit(simDescription.GetSpecialOutfit(outfitName), OutfitCategories.Career);
                            mOutfit = simDescription.GetOutfit(OutfitCategories.Career, simDescription.GetOutfitCount(OutfitCategories.Career) - 1);
                        }
                        else
                        {
                            mOutfit = OutfitUtils.CreateOutfitForSim(simDescription, ResourceKey.CreateOutfitKeyFromProductVersion(outfitName, ProductVersion.EP4), OutfitCategories.Career, OutfitCategories.Everyday, true);
                        }
                        if (mOutfit != null)
                        {
                            Actor.SwitchToOutfitWithSpin(mOutfit.Key);
                            mNeedToSwitchOutfitOnFinish = true;
                        }
                    }
                }
                else if (simDescription.Teen && actor.CurrentOutfitCategory != OutfitCategories.Formalwear)
                {
                    if (actor.Posture == actor.Standing)
                    {
                        actor.SwitchToOutfitWithSpin(Sim.ClothesChangeReason.GoingToRabbitHole, OutfitCategories.Formalwear);
                    }
                    else
                    {
                        actor.OutfitCategoryToUseForRoutingOffLot = OutfitCategories.Formalwear;
                    }
                }
                return true;
            }

            public override bool AfterExitingRabbitHole()
            {
                if (mNeedToSwitchOutfitOnFinish)
                {
                    Actor.SwitchToPreviousOutfitWithSpin();
                    Actor.SimDescription.RemoveOutfit(OutfitCategories.Career, Actor.SimDescription.GetOutfitCount(OutfitCategories.Career) - 1, true);
                    mNeedToSwitchOutfitOnFinish = false;
                }
                if (mNeedToRestoreSoundsOnFinish)
                {
                    Target.ClearAmbientSounds(Target);
                    Target.AddAmbientSound("rhole_school_oneshot");
                    mNeedToRestoreSoundsOnFinish = false;
                }
                return true;
            }

            public override void Cleanup()
            {
                if (mNeedToSwitchOutfitOnFinish)
                {
                    Actor.SwitchToPreviousOutfitWithoutSpin();
                    Actor.SimDescription.RemoveOutfit(OutfitCategories.Career, Actor.SimDescription.GetOutfitCount(OutfitCategories.Career) - 1, true);
                }
                if (mNeedToRestoreSoundsOnFinish)
                {
                    Target.ClearAmbientSounds(Target);
                    Target.AddAmbientSound("rhole_school_oneshot");
                }
                base.Cleanup();
            }
        }

        public class AttendRecital : SchoolRabbitHole.AttendRecital
        {
            public class DefinitionModified : BaseDefinition, IOverridesVisualType
            {
                Definition mDefinitionBase = new Definition();

                public override float TimeToWaitInside
                {
                    get
                    {
                        return kTimeToSpendInside;
                    }
                }

                public InteractionVisualTypes GetVisualType
                {
                    get
                    {
                        return InteractionVisualTypes.Opportunity;
                    }
                }

                public override string GetInteractionName(Sim actor, RabbitHole target, InteractionObjectPair interaction)
                {
                    return mDefinitionBase.GetInteractionName(actor, target, interaction);
                }

                public override bool Test(Sim actor, RabbitHole target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return mDefinitionBase.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
                }
            }

            public override bool BeforeRouting(Sim actor)
            {
                actor.ShowTNSIfSelectable(TNSNames.AfterschoolActivityRecitalTNS, null, actor, null, null, actor.IsFemale, false, actor);
                SimDescription simDescription = actor.SimDescription;
                string outfitName = "";
                if (simDescription.Child)
                {
                    AfterschoolActivityType? afterschoolActivityType = null;
                    if (AfterschoolActivity.HasAfterschoolActivityOfType(actor, AfterschoolActivityType.Ballet))
                    {
                        afterschoolActivityType = AfterschoolActivityType.Ballet;
                        outfitName = GetAfterschoolActivityOutfitName(actor, AfterschoolActivityType.Ballet);
                    }
                    else if (AfterschoolActivity.HasAfterschoolActivityOfType(actor, AfterschoolActivityType.Scouts))
                    {
                        afterschoolActivityType = AfterschoolActivityType.Scouts;
                        outfitName = GetAfterschoolActivityOutfitName(actor, AfterschoolActivityType.Scouts);
                    }
                    if (afterschoolActivityType != null && GetAfterschoolActivityOutfitEnabled(simDescription, afterschoolActivityType))
                    {
                        if (simDescription.HasSpecialOutfit(outfitName))
                        {
                            simDescription.AddOutfit(simDescription.GetSpecialOutfit(outfitName), OutfitCategories.Career);
                            mOutfit = simDescription.GetOutfit(OutfitCategories.Career, simDescription.GetOutfitCount(OutfitCategories.Career) - 1);
                        }
                        else
                        {
                            mOutfit = OutfitUtils.CreateOutfitForSim(simDescription, ResourceKey.CreateOutfitKeyFromProductVersion(outfitName, ProductVersion.EP4), OutfitCategories.Career, OutfitCategories.Everyday, true);
                        }
                        if (mOutfit != null)
                        {
                            actor.SwitchToOutfitWithSpin(mOutfit.Key);
                            mNeedToSwitchOutfitOnFinish = true;
                        }
                    }
                }
                else if (simDescription.Teen && actor.CurrentOutfitCategory != OutfitCategories.Formalwear)
                {
                    if (actor.Posture == actor.Standing)
                    {
                        actor.SwitchToOutfitWithSpin(Sim.ClothesChangeReason.GoingToRabbitHole, OutfitCategories.Formalwear);
                    }
                    else
                    {
                        actor.OutfitCategoryToUseForRoutingOffLot = OutfitCategories.Formalwear;
                    }
                }
                return true;
            }

            public override bool AfterExitingRabbitHole()
            {
                if (mNeedToSwitchOutfitOnFinish)
                {
                    Actor.SwitchToPreviousOutfitWithSpin();
                    Actor.SimDescription.RemoveOutfit(OutfitCategories.Career, Actor.SimDescription.GetOutfitCount(OutfitCategories.Career) - 1, true);
                    mNeedToSwitchOutfitOnFinish = false;
                }
                if (mNeedToRestoreSoundsOnFinish)
                {
                    Target.ClearAmbientSounds(Target);
                    Target.AddAmbientSound("rhole_school_oneshot");
                    mNeedToRestoreSoundsOnFinish = false;
                }
                return true;
            }

            public override void Cleanup()
            {
                if (mNeedToSwitchOutfitOnFinish)
                {
                    Actor.SwitchToPreviousOutfitWithoutSpin();
                    Actor.SimDescription.RemoveOutfit(OutfitCategories.Career, Actor.SimDescription.GetOutfitCount(OutfitCategories.Career) - 1, true);
                }
                if (mNeedToRestoreSoundsOnFinish)
                {
                    Target.ClearAmbientSounds(Target);
                    Target.AddAmbientSound("rhole_school_oneshot");
                }
                base.Cleanup();
            }
        }

        public class EditAfterschoolActivityOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public AfterschoolActivityType mAfterschoolActivityType;

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditAfterschoolActivityOutfit>
            {
                public AfterschoolActivityType mAfterschoolActivityType;

                public Definition()
                {
                }

                public Definition(AfterschoolActivityType afterschoolActivityType)
                {
                    mAfterschoolActivityType = afterschoolActivityType;
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, GameObject target, List<InteractionObjectPair> results)
                {
                    int index = 0;
                    foreach (AfterschoolActivityType afterschoolActivityType in Enum.GetValues(typeof(AfterschoolActivityType)))
                    {
                        results.Add(new InteractionObjectPair(new Definition(afterschoolActivityType), target));
                        if (index == 1)
                        {
                            break;
                        }
                        index++;
                    }
                }

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    EditAfterschoolActivityOutfit editAfterschoolActivityOutfit = new EditAfterschoolActivityOutfit();
                    editAfterschoolActivityOutfit.SetAfterschoolActivityType(mAfterschoolActivityType);
                    editAfterschoolActivityOutfit.Init(ref parameters);
                    return editAfterschoolActivityOutfit;
                }

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return Common.Localize(actor.IsFemale, GetLocalizationKey(mAfterschoolActivityType) + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, GetLocalizationKey(mAfterschoolActivityType) + "Path")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return (actor == target && Tuning.kShowSimMenu || target as Sim == null && Tuning.kShowObjectMenu) && actor.SimDescription.Child && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous && AfterschoolActivity.HasAfterschoolActivityOfType(actor, mAfterschoolActivityType);
                }
            }

            public static string GetLocalizationKey(AfterschoolActivityType afterschoolActivityType)
            {
                return "CustomAfterschoolActivityOutfit/" + new Dictionary<AfterschoolActivityType, string>
                {
                    {
                        AfterschoolActivityType.Ballet,
                        "EditBalletOutfit/"
                    },
                    {
                        AfterschoolActivityType.Scouts,
                        "EditScoutsOutfit/"
                    }
                }[afterschoolActivityType];
            }

            public override bool Run()
            {
                string outfitName = GetAfterschoolActivityOutfitName(Actor, mAfterschoolActivityType);
                return Common.EditSpecialOutfit(Actor, GetLocalizationKey(mAfterschoolActivityType), outfitName, outfitName, ProductVersion.EP4);
            }

            public void SetAfterschoolActivityType(AfterschoolActivityType afterschoolActivityType)
            {
                mAfterschoolActivityType = afterschoolActivityType;
            }
        }

        public class ResetAfterschoolActivityOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public AfterschoolActivityType mAfterschoolActivityType;

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ResetAfterschoolActivityOutfit>
            {
                public AfterschoolActivityType mAfterschoolActivityType;

                public Definition()
                {
                }

                public Definition(AfterschoolActivityType afterschoolActivityType)
                {
                    mAfterschoolActivityType = afterschoolActivityType;
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, GameObject target, List<InteractionObjectPair> results)
                {
                    int index = 0;
                    foreach (AfterschoolActivityType afterschoolActivityType in Enum.GetValues(typeof(AfterschoolActivityType)))
                    {
                        results.Add(new InteractionObjectPair(new Definition(afterschoolActivityType), target));
                        if (index == 1)
                        {
                            break;
                        }
                        index++;
                    }
                }

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    ResetAfterschoolActivityOutfit resetAfterschoolActivityOutfit = new ResetAfterschoolActivityOutfit();
                    resetAfterschoolActivityOutfit.SetAfterschoolActivityType(mAfterschoolActivityType);
                    resetAfterschoolActivityOutfit.Init(ref parameters);
                    return resetAfterschoolActivityOutfit;
                }

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return Common.Localize(actor.IsFemale, GetLocalizationKey(mAfterschoolActivityType) + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, GetLocalizationKey(mAfterschoolActivityType) + "Path")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return (actor == target && Tuning.kShowSimMenu || target as Sim == null && Tuning.kShowObjectMenu) && actor.SimDescription.Child && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous && AfterschoolActivity.HasAfterschoolActivityOfType(actor, mAfterschoolActivityType) && actor.SimDescription.HasSpecialOutfit(GetAfterschoolActivityOutfitName(actor, mAfterschoolActivityType));
                }
            }

            public static string GetLocalizationKey(AfterschoolActivityType afterschoolActivityType)
            {
                return "CustomAfterschoolActivityOutfit/" + new Dictionary<AfterschoolActivityType, string>
                {
                    {
                        AfterschoolActivityType.Ballet,
                        "ResetBalletOutfit/"
                    },
                    {
                        AfterschoolActivityType.Scouts,
                        "ResetScoutsOutfit/"
                    }
                }[afterschoolActivityType];
            }

            public override bool Run()
            {
                Actor.SimDescription.RemoveSpecialOutfit(GetAfterschoolActivityOutfitName(Actor, mAfterschoolActivityType));
                Common.Notify(Common.Localize(Actor.IsFemale, GetLocalizationKey(mAfterschoolActivityType) + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                return true;
            }

            public void SetAfterschoolActivityType(AfterschoolActivityType afterschoolActivityType)
            {
                mAfterschoolActivityType = afterschoolActivityType;
            }
        }

        public class DoShowOffMove : Sim.DoShowOffMove
        {
            public class DefinitionModified : InteractionDefinition<Sim, Sim, DoShowOffMove>
            {
                public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }
            }

            public override bool Run()
            {
                if (IsMaster && !Actor.HasExitReason())
                {
                    DoShowOffMove doShowOffMove = Singleton.CreateInstance(Actor, Target, GetPriority(), Autonomous, CancellableByPlayer) as DoShowOffMove;
                    doShowOffMove.LinkedInteractionInstance = this;
                    Target.InteractionQueue.AddNext(doShowOffMove);
                }
                if (!SafeToSync())
                {
                    return false;
                }
                if (!StartSync(IsMaster))
                {
                    FinishLinkedInteraction(IsMaster);
                    return false;
                }
                Actor.SynchronizationLevel = Sim.SyncLevel.Started;
                if (!Actor.WaitForSynchronizationLevelWithSim(Actor.SynchronizationTarget, Sim.SyncLevel.Started, 5))
                {
                    FinishLinkedInteraction(IsMaster);
                    return false;
                }
                Route route;
                if (SimJig == null)
                {
                    SimJig = GlobalFunctions.CreateObjectOutOfWorld("SocialJigTwoPerson") as SocialJigTwoPerson;
                    DoShowOffMove doShowOffMove = LinkedInteractionInstance as DoShowOffMove;
                    doShowOffMove.SimJig = SimJig;
                    route = Actor.CreateRoute();
                    route.PlanToPoint(Target.Position);
                    if (!SimJig.PlaceJigAlongRoute(Actor, Target, route))
                    {
                        SimJig.Destroy();
                        SimJig = null;
                        FinishLinkedInteraction(IsMaster);
                        return false;
                    }
                }
                route = IsMaster ? SimJig.RouteToJigA(Actor) : SimJig.RouteToJigB(Actor);
                Actor.LookAtManager.SetInteractionLookAt(Target, 200, LookAtJointFilter.HeadBones);
                if (!Actor.DoRoute(route))
                {
                    FinishLinkedInteraction(IsMaster);
                    return false;
                }
                Actor.SynchronizationLevel = Sim.SyncLevel.Routed;
                if (!Actor.WaitForSynchronizationLevelWithSim(Actor.SynchronizationTarget, Sim.SyncLevel.Routed, 15))
                {
                    FinishLinkedInteraction(IsMaster);
                    return false;
                }
                Actor.IdleManager.DisallowUniqueIdleAnimations = true;
                if (IsMaster)
                {
                    Skill skillForMoveType = GetSkillForMoveType(Actor, MoveType);
                    GetSkillLevelForActivity(Actor, skillForMoveType);
                    string entryState = "", exitState = "", outfitName = "";
                    AfterschoolActivityType? afterschoolActivityType = null;
                    switch (skillForMoveType.Guid)
                    {
                        case SkillNames.Ballet:
                            afterschoolActivityType = AfterschoolActivityType.Ballet;
                            entryState = "EnterBallet";
                            exitState = "ExitBallet";
                            outfitName = GetAfterschoolActivityOutfitName(Actor, afterschoolActivityType);
                            break;
                        case SkillNames.Scouting:
                            afterschoolActivityType = AfterschoolActivityType.Scouts;
                            entryState = "EnterScouts";
                            exitState = "ExitScouts";
                            outfitName = GetAfterschoolActivityOutfitName(Actor, afterschoolActivityType);
                            break;
                    }
                    SimDescription simDescription = Actor.SimDescription;
                    SimOutfit resultOutfit, simOutfit = null, uniform;
                    if (simDescription.HasSpecialOutfit(outfitName))
                    {
                        simOutfit = simDescription.GetSpecialOutfit(outfitName);
                    }
                    else if (OutfitUtils.TryGenerateSimOutfit(outfitName, ProductVersion.EP4, out uniform) && OutfitUtils.TryApplyUniformToOutfit(simDescription.GetOutfit(OutfitCategories.Everyday, 0), uniform, simDescription, outfitName, out resultOutfit))
                    {
                        simDescription.AddSpecialOutfit(resultOutfit, outfitName);
                        simOutfit = resultOutfit;
                    }
                    if (simOutfit != null && GetAfterschoolActivityOutfitEnabled(simDescription, afterschoolActivityType))
                    {
                        Actor.SwitchToOutfitWithSpin(simOutfit.Key);
                    }
                    EnterStateMachine("afterschoolactivities", entryState, "x");
                    BeginCommodityUpdates();
                    SetParameter("AfterschoolActivitySkillLevel", GetParameterForChosenMove(MoveType));
                    AnimateSim(exitState);
                    EndCommodityUpdates(true);
                }
                else
                {
                    float favorableReactionChance = GetFavorableReactionChance(Actor, Target);
                    ReactionTypes reactionType = RandomUtil.RandomChance(favorableReactionChance) ? (RandomUtil.CoinFlip() ? ReactionTypes.Excited : ReactionTypes.Cheer) : (RandomUtil.CoinFlip() ? ReactionTypes.Bored : ReactionTypes.Awkward);
                    Actor.PlayReaction(reactionType, new InteractionPriority(InteractionPriorityLevel.High), Target, ReactionSpeed.AfterInteraction);
                }
                FinishLinkedInteraction(IsMaster);
                return true;
            }
        }

        public class ToggleAfterschoolActivityOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public AfterschoolActivityType mAfterschoolActivityType;

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ToggleAfterschoolActivityOutfit>
            {
                public AfterschoolActivityType mAfterschoolActivityType;

                public Definition()
                {
                }

                public Definition(AfterschoolActivityType afterschoolActivityType)
                {
                    mAfterschoolActivityType = afterschoolActivityType;
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, GameObject target, List<InteractionObjectPair> results)
                {
                    int index = 0;
                    foreach (AfterschoolActivityType afterschoolActivityType in Enum.GetValues(typeof(AfterschoolActivityType)))
                    {
                        results.Add(new InteractionObjectPair(new Definition(afterschoolActivityType), target));
                        if (index == 1)
                        {
                            break;
                        }
                        index++;
                    }
                }

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    ToggleAfterschoolActivityOutfit toggleAfterschoolActivityOutfit = new ToggleAfterschoolActivityOutfit();
                    toggleAfterschoolActivityOutfit.SetAfterschoolActivityType(mAfterschoolActivityType);
                    toggleAfterschoolActivityOutfit.Init(ref parameters);
                    return toggleAfterschoolActivityOutfit;
                }

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    if (GetAfterschoolActivityOutfitEnabled(actor.SimDescription, mAfterschoolActivityType))
                    {
                        return Common.Localize(actor.IsFemale, GetLocalizationKey(mAfterschoolActivityType) + "DisableInteractionName");
                    }
                    return Common.Localize(actor.IsFemale, GetLocalizationKey(mAfterschoolActivityType) + "EnableInteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, GetLocalizationKey(mAfterschoolActivityType) + "Path")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return (actor == target && Tuning.kShowSimMenu || target as Sim == null && Tuning.kShowObjectMenu) && actor.SimDescription.Child && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous && AfterschoolActivity.HasAfterschoolActivityOfType(actor, mAfterschoolActivityType);
                }
            }

            public static string GetLocalizationKey(AfterschoolActivityType afterschoolActivityType)
            {
                return "CustomAfterschoolActivityOutfit/" + new Dictionary<AfterschoolActivityType, string>
                {
                    {
                        AfterschoolActivityType.Ballet,
                        "ToggleBalletOutfit/"
                    },
                    {
                        AfterschoolActivityType.Scouts,
                        "ToggleScoutsOutfit/"
                    }
                }[afterschoolActivityType];
            }

            public override bool Run()
            {
                if (GetAfterschoolActivityOutfitEnabled(Actor.SimDescription, mAfterschoolActivityType))
                {
                    DisableAfterschoolActivityOutfit(Actor.SimDescription, mAfterschoolActivityType);
                    Common.Notify(Common.Localize(Actor.IsFemale, GetLocalizationKey(mAfterschoolActivityType) + "DisabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                else
                {
                    EnableAfterschoolActivityOutfit(Actor.SimDescription, mAfterschoolActivityType);
                    Common.Notify(Common.Localize(Actor.IsFemale, GetLocalizationKey(mAfterschoolActivityType) + "EnabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                return true;
            }

            public void SetAfterschoolActivityType(AfterschoolActivityType afterschoolActivityType)
            {
                mAfterschoolActivityType = afterschoolActivityType;
            }
        }

        static void AddInteractions(GameObject gameObject)
        {
            if (gameObject != null && !gameObject.Interactions.Exists(interaction => interaction.InteractionDefinition.GetType() == EditAfterschoolActivityOutfit.Singleton.GetType()) && GameUtils.IsInstalled(ProductVersion.EP4))
            {
                gameObject.AddInteraction(EditAfterschoolActivityOutfit.Singleton);
                gameObject.AddInteraction(ResetAfterschoolActivityOutfit.Singleton);
                gameObject.AddInteraction(ToggleAfterschoolActivityOutfit.Singleton);
            }
        }

        static void DisableAfterschoolActivityOutfit(SimDescription simDescription, AfterschoolActivityType afterschoolActivityType)
        {
            switch (afterschoolActivityType)
            {
                case AfterschoolActivityType.Ballet:
                    DisableBalletOutfit(simDescription);
                    break;
                case AfterschoolActivityType.Scouts:
                    DisableScoutsOutfit(simDescription);
                    break;
            }
        }

        static void DisableBalletOutfit(SimDescription simDescription)
        {
            if (GetAfterschoolActivityOutfitEnabled(simDescription, AfterschoolActivityType.Ballet))
            {
                sBalletOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
        }

        static void DisableScoutsOutfit(SimDescription simDescription)
        {
            if (GetAfterschoolActivityOutfitEnabled(simDescription, AfterschoolActivityType.Scouts))
            {
                sScoutsOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
        }

        static void EnableAfterschoolActivityOutfit(SimDescription simDescription, AfterschoolActivityType afterschoolActivityType)
        {
            switch (afterschoolActivityType)
            {
                case AfterschoolActivityType.Ballet:
                    EnableBalletOutfit(simDescription);
                    break;
                case AfterschoolActivityType.Scouts:
                    EnableScoutsOutfit(simDescription);
                    break;
            }
        }

        static void EnableBalletOutfit(SimDescription simDescription)
        {
            if (!GetAfterschoolActivityOutfitEnabled(simDescription, AfterschoolActivityType.Ballet))
            {
                sBalletOutfitDisabledList.Remove(simDescription.SimDescriptionId);
            }
        }

        static void EnableScoutsOutfit(SimDescription simDescription)
        {
            if (!GetAfterschoolActivityOutfitEnabled(simDescription, AfterschoolActivityType.Scouts))
            {
                sScoutsOutfitDisabledList.Remove(simDescription.SimDescriptionId);
            }
        }

        static bool GetAfterschoolActivityOutfitEnabled(SimDescription simDescription, AfterschoolActivityType? afterschoolActivityType)
        {
            return !new Dictionary<AfterschoolActivityType?, bool>
            {
                {
                    AfterschoolActivityType.Ballet,
                    sBalletOutfitDisabledList.Contains(simDescription.SimDescriptionId)
                },
                {
                    AfterschoolActivityType.Scouts,
                    sScoutsOutfitDisabledList.Contains(simDescription.SimDescriptionId)
                }
            }[afterschoolActivityType];
        }

        public static string GetAfterschoolActivityOutfitName(Sim actor, AfterschoolActivityType? afterschoolActivityType)
        {
            return new Dictionary<AfterschoolActivityType?, string>
            {
                {
                    AfterschoolActivityType.Ballet,
                    actor.IsMale ? "cmBodyEP4Ballet" : "cfBodyEP4BalletTutu"
                },
                {
                    AfterschoolActivityType.Scouts,
                    "cuBodyEP4Scout"
                }
            }[afterschoolActivityType];
        }

        static void OnObjectPlacedInLot(object sender, EventArgs e)
        {
            World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs = e as World.OnObjectPlacedInLotEventArgs;
            if (onObjectPlacedInLotEventArgs != null)
            {
                AddInteractions(GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId) as Dresser);
            }
        }

        static void OnPreLoad()
        {
            SchoolRabbitHole.AttendAudition.Singleton = new AttendAudition.DefinitionModified();
            SchoolRabbitHole.AttendRecital.Singleton = new AttendRecital.DefinitionModified();
            Sim.DoShowOffMove.Singleton = new DoShowOffMove.DefinitionModified();
            Common.CopyTuning(typeof(SchoolRabbitHole), typeof(SchoolRabbitHole.AttendAudition.Definition), typeof(AttendAudition.DefinitionModified));
            Common.CopyTuning(typeof(SchoolRabbitHole), typeof(SchoolRabbitHole.AttendRecital.Definition), typeof(AttendRecital.DefinitionModified));
            Common.CopyTuning(typeof(Sim), typeof(Sim.DoShowOffMove.Definition), typeof(DoShowOffMove.DefinitionModified));
        }

        static ListenerAction OnSimDescriptionDisposed(Event e)
        {
            try
            {
                Sim sim = e.TargetObject as Sim;
                if (sim != null)
                {
                    int index = 0;
                    foreach (AfterschoolActivityType afterschoolActivityType in Enum.GetValues(typeof(AfterschoolActivityType)))
                    {
                        EnableAfterschoolActivityOutfit(sim.SimDescription, afterschoolActivityType);
                        if (index == 1)
                        {
                            break;
                        }
                        index++;
                    }
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
            Array.ForEach(Sims3.Gameplay.Queries.GetObjects<Dresser>(), AddInteractions);
            if (Household.ActiveHousehold != null)
            {
                Household.ActiveHousehold.Sims.ForEach(AddInteractions);
            }
            sSimDescriptionDisposedListener = EventTracker.AddListener(EventTypeId.kSimDescriptionDisposed, OnSimDescriptionDisposed);
            sSimSelectedListener = EventTracker.AddListener(EventTypeId.kEventSimSelected, OnSimSelected);
        }

        static void OnWorldQuit(object sender, EventArgs e)
        {
            EventTracker.RemoveListener(sSimDescriptionDisposedListener);
            EventTracker.RemoveListener(sSimSelectedListener);
            sSimDescriptionDisposedListener = null;
            sSimSelectedListener = null;
        }
    }
}