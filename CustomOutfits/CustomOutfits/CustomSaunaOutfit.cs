using System;
using System.Collections.Generic;
using System.Reflection;
using Destrospean.CustomOutfits;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.InteractionsShared;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Objects.Seating;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.Store.Objects;
using Sims3.UI;
using Tuning = Sims3.Gameplay.Destrospean.CustomOutfits;

namespace Destrospean
{
    public class CustomSaunaOutfit
    {
        public static readonly string kSaunaSpecialOutfitKey = "Sauna";

        [Tunable]
        protected static bool kInstantiator;

        [PersistableStatic(true)]
        static List<ulong> sSaunaOutfitDisabledList;

        static EventListener sSimDescriptionDisposedListener, sSimSelectedListener;

        static CustomSaunaOutfit()
        {
            Assembly woohooerAssembly, woohooerSaunaAssembly;
            if (TryGetWoohooerSaunaAssemblies(out woohooerAssembly, out woohooerSaunaAssembly))
            {
                Common.ReplaceMethod(woohooerSaunaAssembly.GetType("NRaas.WoohooerSpace.Helpers.SaunaClassicEx").GetMethod("StateMachineEnterAndSit", BindingFlags.Public | BindingFlags.Static), typeof(CustomSaunaOutfit).GetMethod("StateMachineEnterAndSit0", BindingFlags.Public | BindingFlags.Static));
            }
            sSaunaOutfitDisabledList = new List<ulong>();
            sSimDescriptionDisposedListener = null;
            sSimSelectedListener = null;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnObjectPlacedInLotEventHandler += OnObjectPlacedInLot;
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            World.sOnWorldQuitEventHandler += OnWorldQuit;
        }

        public class BatheInWater : Interaction<Sim, SaunaClassic>
        {
            public class DefinitionModified : InteractionDefinition<Sim, SaunaClassic, BatheInWater>
            {
                public enum BathType
                {
                    Water,
                    Mud
                }

                public BathType mBathType;

                public DefinitionModified()
                {
                }

                public DefinitionModified(BathType bathType)
                {
                    mBathType = bathType;
                }

                public override bool Test(Sim a, SaunaClassic target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (isAutonomous && a.HasTrait(TraitNames.Hydrophobic))
                    {
                        return false;
                    }
                    if (isAutonomous && a.OccultManager.HasOccultType(Sims3.UI.Hud.OccultTypes.Frankenstein))
                    {
                        return false;
                    }
                    if (a.SimDescription.IsVisuallyPregnant)
                    {
                        return false;
                    }
                    if (target.mSimInTub != null)
                    {
                        greyedOutTooltipCallback = () => SaunaClassic.LocalizeString(a.IsFemale, "InUse");
                        return false;
                    }
                    return true;
                }

                public override string GetInteractionName(Sim actor, SaunaClassic target, InteractionObjectPair iop)
                {
                    string name = "BatheInWater";
                    if (mBathType == BathType.Mud)
                    {
                        name = "BatheInMud";
                    }
                    return SaunaClassic.LocalizeString(actor.IsFemale, name, actor);
                }
            }

            public static InteractionDefinition MudSingleton = new DefinitionModified(DefinitionModified.BathType.Mud);

            public static InteractionDefinition WaterSingleton = new DefinitionModified(DefinitionModified.BathType.Water);

            public ObjectGuid mDuckyGuid;

            public override void ConfigureInteraction()
            {
                TimedStage timedStage = new TimedStage(GetInteractionName(), SaunaClassic.kBathLengthInMinutes, false, true, true);
                base.Stages = new List<Stage>(new Stage[1] { timedStage });
            }

            public override bool Run()
            {
                Slot[] slots = new Slot[2]
                    {
                        Slot.RoutingSlot_4,
                        Slot.RoutingSlot_6
                    };
                int slotIndex;
                if (!Actor.RouteToSlotList(Target, slots, out slotIndex))
                {
                    return false;
                }
                DefinitionModified definition = base.InteractionDefinition as DefinitionModified;
                if (definition == null)
                {
                    return false;
                }
                if (Target.mSimInTub != null)
                {
                    return false;
                }
                Target.mSimInTub = Actor;
                StandardEntry();
                if (Actor.HasTrait(TraitNames.Hydrophobic) || Actor.OccultManager.HasOccultType(Sims3.UI.Hud.OccultTypes.Frankenstein))
                {
                    Actor.PlayReaction(ReactionTypes.WhyMe, Target, Sims3.Gameplay.ThoughtBalloons.ThoughtBalloonAxis.kDislike, ReactionSpeed.ImmediateWithoutOverlay);
                }
                if (GetSaunaOutfitEnabled(Actor.SimDescription))
                {
                    if (Actor.SimDescription.HasSpecialOutfit(kSaunaSpecialOutfitKey))
                    {
                        Actor.SwitchToOutfitWithSpin(OutfitCategories.Special, Actor.SimDescription.GetSpecialOutfitIndexFromKey(ResourceUtils.HashString32(kSaunaSpecialOutfitKey)));
                    }
                    else
                    {
                        Actor.SwitchToOutfitWithSpin(Sim.ClothesChangeReason.GoingToSwim);
                    }
                }
                EnterStateMachine("Sauna_store", "SimEnter", "x", "saunaX");
                if (definition.mBathType == DefinitionModified.BathType.Mud)
                {
                    SetParameter("Mudbath", true);
                    Target.SetGeometryState("mud");
                }
                else
                {
                    SetParameter("Mudbath", false);
                    Target.SetGeometryState("water");
                }
                if (slotIndex == 1)
                {
                    SetParameter("IsMirrored", true);
                }
                else
                {
                    SetParameter("IsMirrored", false);
                }
                mDuckyGuid = Sims3.Gameplay.GlobalFunctions.CreateProp("RubberDucky", ProductVersion.BaseGame, Vector3.OutOfWorld, 0, Vector3.UnitZ);
                GameObject @object = GameObject.GetObject(mDuckyGuid);
                if (@object != null)
                {
                    @object.AddToUseList(Actor);
                    SetActor("ducky", @object);
                }
                BeginCommodityUpdates();
                Actor.RegisterGroupTalk();
                AnimateSim("TakeBath");
                StartStages();
                DoLoop(ExitReason.Default, LoopUpdate, mCurrentStateMachine);
                if (Actor.HasTrait(TraitNames.Hydrophobic))
                {
                    Actor.PlayReaction(ReactionTypes.Cry, Target, Sims3.Gameplay.ThoughtBalloons.ThoughtBalloonAxis.kDislike, ReactionSpeed.AfterInteraction);
                }
                Actor.UnregisterGroupTalk();
                Actor.BuffManager.RemoveElement(BuffNames.Singed);
                Actor.BuffManager.RemoveElement(BuffNames.SingedElectricity);
                Actor.BuffManager.RemoveElement(BuffNames.GotFleasHuman);
                if (definition.mBathType == DefinitionModified.BathType.Mud && Actor.HasExitReason(ExitReason.StageComplete))
                {
                    AgingState agingState = Actor.SimDescription.AgingState;
                    if (agingState != null)
                    {
                        agingState.ExtendAgingState(SaunaClassic.kMudBathAgeExtension);
                        Actor.ShowTNSIfSelectable(SaunaClassic.LocalizeString(Actor.IsFemale, "FeelingYounger", Actor), StyledNotification.NotificationStyle.kGameMessagePositive);
                        (Sims3.Gameplay.UI.Responder.Instance.HudModel as Sims3.Gameplay.UI.HudModel).OnSimAgeChanged(Actor.ObjectId);
                    }
                }
                if (definition.mBathType == DefinitionModified.BathType.Water && Actor.HasExitReason(ExitReason.StageComplete))
                {
                    Actor.BuffManager.AddElement(BuffNames.DuckTimeFun, Origin.FromBath);
                }
                bool flag = true;
                EndCommodityUpdates(flag);
                AnimateSim("SimExit");
                Actor.SwitchToOutfitWithSpin(Sim.ClothesChangeReason.GettingOutOfBath);
                if (Target.mSimInTub == Actor)
                {
                    Target.mSimInTub = null;
                }
                StandardExit();
                if (Actor.SimDescription.IsFrankenstein)
                {
                    OccultFrankenstein.PushFrankensteinShortOut(Actor);
                }
                return flag;
            }

            public void LoopUpdate(StateMachineClient smc, LoopData loopData)
            {
                EventTracker.SendEvent(EventTypeId.kEventTakeBath, Actor, Target);
                if (Actor.SimDescription.IsRobot)
                {
                    Actor.AddExitReason(ExitReason.CanceledByScript);
                    return;
                }
                if (Actor.HasGroupTalk && Actor.GroupTalkMembers.Count > 1 && Actor.DoGroupTalk(null, null, true))
                {
                    foreach (Sim item in Target.ActorsUsingMe)
                    {
                        if (item != Actor)
                        {
                            Sims3.Gameplay.Socializing.Relationship relationship = Actor.GetRelationship(item, true);
                            relationship.LTR.UpdateLiking(SaunaClassic.kGroupChatRelationshipBump);
                            Sims3.Gameplay.Socializing.SocialComponent.SetSocialFeedbackForActorAndTarget(Sims3.Gameplay.Socializing.CommodityTypes.Friendly, Actor, item, true, 0, Sims3.UI.Controller.LongTermRelationshipTypes.Undefined, Sims3.UI.Controller.LongTermRelationshipTypes.Undefined);
                        }
                        if (item.Motives.HasMotive(CommodityKind.Social))
                        {
                            item.Motives.ChangeValue(CommodityKind.Social, SaunaClassic.kGroupChatSocialBump);
                        }
                    }
                }
                DefinitionModified definition = base.InteractionDefinition as DefinitionModified;
                if (definition != null && loopData.mLifeTime > SaunaClassic.kBathDurationToRemoveFatigue)
                {
                    Actor.BuffManager.RemoveElement(BuffNames.Fatigued);
                    Actor.BuffManager.RemoveElement(BuffNames.Sore);
                    if (definition.mBathType == DefinitionModified.BathType.Water)
                    {
                        Actor.BuffManager.AddElementPaused((BuffNames)11132721365296021523uL, Origin.FromBath);
                    }
                    if (definition.mBathType == DefinitionModified.BathType.Mud)
                    {
                        Actor.BuffManager.AddElementPaused((BuffNames)11132721365296021524uL, Origin.FromBath);
                    }
                }
            }

            public override void Cleanup()
            {
                GameObject @object = GameObject.GetObject(mDuckyGuid);
                if (@object != null)
                {
                    @object.SetOpacity(0f, 0f);
                    @object.UnParent();
                    @object.Destroy();
                }
                Actor.BuffManager.UnpauseBuff(11132721365296021523uL);
                Actor.BuffManager.UnpauseBuff(11132721365296021524uL);
                base.Cleanup();
            }

            public override void AddExcludedDreams(ICollection<Sims3.Gameplay.DreamsAndPromises.DreamNames> excludedDreams)
            {
                base.AddExcludedDreams(excludedDreams);
                AddExcludedDream(Sims3.Gameplay.DreamsAndPromises.DreamNames.bathe);
            }
        }

        public class EditSaunaOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomSaunaOutfit/EditSaunaOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditSaunaOutfit>
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
                    return (actor == target && Tuning.kShowSimMenu || target as Sim == null && Tuning.kShowObjectMenu) && actor.SimDescription.TeenOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public override bool Run()
            {
                Actor.SimDescription.AddSpecialOutfit(new SimOutfit(Actor.SimDescription.GetOutfit(OutfitCategories.Swimwear, 0).Key), kSaunaSpecialOutfitKey);
                return Common.EditSpecialOutfit(Actor, sLocalizationKey, kSaunaSpecialOutfitKey);
            }
        }

        public class MakeSteam : Interaction<Sim, SaunaClassic>
        {
            public class DefinitionModified : InteractionDefinition<Sim, SaunaClassic, MakeSteam>
            {
                public override bool Test(Sim actor, SaunaClassic target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (actor.SimDescription.IsVisuallyPregnant)
                    {
                        return false;
                    }
                    return true;
                }

                public override string GetInteractionName(Sim actor, SaunaClassic target, InteractionObjectPair interaction)
                {
                    return SaunaClassic.LocalizeString(actor.IsFemale, "MakeSteam", actor);
                }
            }

            public const int mSteamyStoneTrigger = 900;

            public static InteractionDefinition Singleton = new DefinitionModified();

            public override bool Run()
            {
                if (!Actor.RouteToSlot(Target, Slot.RoutingSlot_14))
                {
                    return false;
                }
                if (!Actor.HasTrait(TraitNames.NeverNude) && GetSaunaOutfitEnabled(Actor.SimDescription))
                {
                    if (Actor.SimDescription.HasSpecialOutfit(kSaunaSpecialOutfitKey))
                    {
                        Actor.SwitchToOutfitWithSpin(OutfitCategories.Special, Actor.SimDescription.GetSpecialOutfitIndexFromKey(ResourceUtils.HashString32(kSaunaSpecialOutfitKey)));
                    }
                    else
                    {
                        Actor.SwitchToOutfitWithSpin(Sim.ClothesChangeReason.GoingToSwim);
                    }
                }
                StandardEntry();
                EnterStateMachine("Sauna_store", "SimEnter", "x", "saunaX");
                AddOneShotScriptEventHandler(900u, OnAnimationEvent);
                BeginCommodityUpdates();
                AnimateSim("PourWater");
                bool flag = true;
                EndCommodityUpdates(flag);
                AnimateSim("SimExit");
                List<Sim> simsInFootprint = Target.GetSimsInFootprint();
                foreach (Sim item in simsInFootprint)
                {
                    item.Motives.ChangeValue(CommodityKind.Fun, SaunaClassic.kFunBoostForSteam);
                }
                StandardExit(true, false);
                InteractionInstance interactionInstance = SaunaSit.Singleton.CreateInstance(Target, Actor, Actor.InheritedPriority(), false, false);
                if (interactionInstance.RunInteraction())
                {
                    BeginCommodityUpdates();
                    float minSitWaitTime = Sim.MinSitWaitTime,
                    maxSitWaitTime = Sim.MaxSitWaitTime;
                    Actor.ClearExitReasons();
                    Actor.RegisterGroupTalk();
                    float num = Sims3.Gameplay.Core.RandomUtil.RandomFloatGaussianDistribution(minSitWaitTime, maxSitWaitTime);
                    Sims3.Gameplay.Utilities.DateAndTime previousDateAndTime = Sims3.Gameplay.Utilities.SimClock.CurrentTime();
                    while (!Actor.WaitForExitReason(Sim.kWaitForExitReasonDefaultTime, ExitReason.Default))
                    {
                        if (Actor.ShouldDoGroupTalk() && Actor.DoGroupTalk(null, null, true))
                        {
                            foreach (Sim actorUsingMe in Target.ActorsUsingMe)
                            {
                                if (actorUsingMe != Actor)
                                {
                                    Sims3.Gameplay.Socializing.Relationship relationship = Actor.GetRelationship(actorUsingMe, true);
                                    relationship.LTR.UpdateLiking(SaunaClassic.kGroupChatRelationshipBump);
                                    Sims3.Gameplay.Socializing.SocialComponent.SetSocialFeedbackForActorAndTarget(Sims3.Gameplay.Socializing.CommodityTypes.Friendly, Actor, actorUsingMe, true, 0, Sims3.UI.Controller.LongTermRelationshipTypes.Undefined, Sims3.UI.Controller.LongTermRelationshipTypes.Undefined);
                                }
                                if (actorUsingMe.Motives.HasMotive(CommodityKind.Social))
                                {
                                    actorUsingMe.Motives.ChangeValue(CommodityKind.Social, SaunaClassic.kGroupChatSocialBump);
                                }
                            }
                        }
                        if (Sims3.Gameplay.Utilities.SimClock.ElapsedTime(Sims3.Gameplay.Utilities.TimeUnit.Minutes, previousDateAndTime) > num)
                        {
                            break;
                        }
                    }
                    Actor.UnregisterGroupTalk();
                    EndCommodityUpdates(true);
                    if (!base.Cancelled && Actor.InteractionQueue.GetNextInteraction() == null && Sims3.Gameplay.Core.RandomUtil.CoinFlip())
                    {
                        Actor.InteractionQueue.Add(Singleton.CreateInstance(Target, Actor, Actor.InheritedPriority(), base.Autonomous, true));
                    }
                }
                return flag;
            }

            public void OnAnimationEvent(StateMachineClient smc, IEvent evt)
            {
                if (evt.EventId == 900)
                {
                    Target.StartStonesSteamFX();
                }
            }

            public override void Cleanup()
            {
                base.Cleanup();
            }
        }

        public class ResetSaunaOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomSaunaOutfit/ResetSaunaOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ResetSaunaOutfit>
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
                    return (actor == target && Tuning.kShowSimMenu || target as Sim == null && Tuning.kShowObjectMenu) && actor.SimDescription.TeenOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous && actor.SimDescription.HasSpecialOutfit(kSaunaSpecialOutfitKey);
                }
            }

            public override bool Run()
            {
                Actor.SimDescription.RemoveSpecialOutfit(kSaunaSpecialOutfitKey);
                Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                return true;
            }
        }

        public class SaunaSit : Sit
        {
            public class DefinitionModified : Sit.Definition
            {
                public DefinitionModified()
                {
                }

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    InteractionInstance interactionInstance = new SaunaSit();
                    interactionInstance.Init(ref parameters);
                    return interactionInstance;
                }

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
                {
                    return base.GetInteractionName(actor, target, new InteractionObjectPair(Sit.Singleton, target));
                }
            }

            public new static InteractionDefinition Singleton = new DefinitionModified();

            public bool mIsMaster;

            public bool mCompleted;

            public override void Cleanup()
            {
                mCompleted = true;
                base.Cleanup();
            }

            public override bool Run()
            {
                try
                {
                    Sims3.Gameplay.Interfaces.ISittable sittable = SittingHelpers.CastToSittable(Target);
                    if (sittable == null)
                    {
                        Actor.AddExitReason(ExitReason.FailedToStart);
                        return false;
                    }
                    Slot containmentSlotClosestToHit = base.GetContainmentSlotClosestToHit();
                    if (Actor.Posture.Container == Target)
                    {
                        SittingPosture sittingPosture = Actor.Posture as SittingPosture;
                        if (sittingPosture != null)
                        {
                            SitData target = sittingPosture.Part.Target;
                            if (containmentSlotClosestToHit == target.ContainmentSlot)
                            {
                                return true;
                            }
                            if (!Stand.Singleton.CreateInstance(Target, Actor, GetPriority(), base.Autonomous, base.CancellableByPlayer).RunInteraction())
                            {
                                return false;
                            }
                        }
                    }
                    SimQueue simLine = Target.SimLine;
                    if (simLine != null && !simLine.WaitForTurn(this, SimQueue.WaitBehavior.NeverWait | SimQueue.WaitBehavior.DontPlayRouteFail, ExitReason.Default, 0f))
                    {
                        Sim firstSim = simLine.FirstSim;
                        if (firstSim != null && firstSim.InteractionQueue.TransitionInteraction is Stand)
                        {
                            Actor.RemoveExitReason(ExitReason.ObjectInUse);
                            simLine.WaitForTurn(this, SimQueue.WaitBehavior.OnlyWaitAtHeadOfLine | SimQueue.WaitBehavior.DontPlayRouteFail, ExitReason.Default, Sit.kTimeToWait);
                        }
                    }
                    SitData partToSitDownIn;
                    Slot routingSlot;
                    object sitContext;
                    if (!sittable.RouteToForSitting(Actor, containmentSlotClosestToHit, true, out partToSitDownIn, out routingSlot, out sitContext))
                    {
                        return false;
                    }
                    sittable = SittingHelpers.CastToSittable(partToSitDownIn.Container);
                    if (!SittingHelpers.ReserveSittable(this, Actor, sittable, partToSitDownIn))
                    {
                        return false;
                    }
                    StateMachineClient stateMachineClient = sittable.StateMachineAcquireAndInit(Actor);
                    if (stateMachineClient == null)
                    {
                        Actor.AddExitReason(ExitReason.NullValueFound);
                        SittingHelpers.UnreserveSittable(this, sittable, partToSitDownIn);
                        return false;
                    }
                    ISittingPostureCreator sittingPostureCreator = partToSitDownIn.Container.Parent as ISittingPostureCreator;
                    SittingPosture sittingPosture2 = ((sittingPostureCreator == null) ? new SittingPosture(partToSitDownIn.Container, Actor, stateMachineClient, partToSitDownIn) : sittingPostureCreator.CreatePosture(partToSitDownIn.Container, Actor, stateMachineClient, partToSitDownIn));
                    if (stateMachineClient.HasActorDefinition("surface"))
                    {
                        stateMachineClient.SetActor("surface", partToSitDownIn.Container);
                    }
                    BeginCommodityUpdates();
                    Actor.LookAtManager.DisableLookAts();
                    bool flag = Actor.CarryStateMachine != null && Actor.GetObjectInRightHand() is IUseCarrySitTransitions;
                    if (flag)
                    {
                        Actor.CarryStateMachine.RequestState(false, "x", "CarrySitting");
                    }
                    if (!StateMachineEnterAndSit0(sittable as SaunaClassic, false, stateMachineClient, sittingPosture2, routingSlot, sitContext))
                    {
                        if (flag)
                        {
                            Actor.CarryStateMachine.RequestState(false, "x", "Carry");
                        }
                        Actor.LookAtManager.EnableLookAts();
                        Actor.AddExitReason(ExitReason.NullValueFound);
                        SittingHelpers.UnreserveSittable(this, sittable, partToSitDownIn);
                        EndCommodityUpdates(false);
                        return false;
                    }
                    Actor.LookAtManager.EnableLookAts();
                    Actor.Posture = sittingPosture2;
                    if (sittable.ComfyScore > 0)
                    {
                        Actor.BuffManager.AddElement(BuffNames.Comfy, sittable.ComfyScore, Origin.FromComfyObject);
                    }
                    EndCommodityUpdates(true);
                    StandardExit(false, false);
                    if (Actor.HasExitReason(ExitReason.UserCanceled))
                    {
                        Actor.AddExitReason(ExitReason.CancelledByPosture);
                    }
                    if (mIsMaster)
                    {
                        SaunaSit saunaSit = LinkedInteractionInstance as SaunaSit;
                        if (saunaSit != null)
                        {
                            Sim actor = saunaSit.Actor;
                            while (!base.Cancelled && actor.InteractionQueue.HasInteraction(saunaSit) && !saunaSit.mCompleted)
                            {
                                Simulator.Sleep(0);
                            }
                        }
                    }
                    return !Actor.HasExitReason();
                }
                catch (ResetException)
                {
                    throw;
                }
            }
        }

        public class ToggleSaunaOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomSaunaOutfit/ToggleSaunaOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ToggleSaunaOutfit>
            {
                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    if (GetSaunaOutfitEnabled(actor.SimDescription))
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
                    return (actor == target && Tuning.kShowSimMenu || target as Sim == null && Tuning.kShowObjectMenu) && actor.SimDescription.TeenOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public override bool Run()
            {
                if (GetSaunaOutfitEnabled(Actor.SimDescription))
                {
                    DisableSaunaOutfit(Actor.SimDescription);
                    Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "DisabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                else
                {
                    EnableSaunaOutfit(Actor.SimDescription);
                    Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "EnabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                return true;
            }
        }

        static void AddInteractions(GameObject gameObject)
        {
            if (gameObject != null && !gameObject.Interactions.Exists(interaction => interaction.InteractionDefinition.GetType() == EditSaunaOutfit.Singleton.GetType()))
            {
                gameObject.RemoveInteractionByType(Sit.Singleton);
                Assembly woohooerAssembly, woohooerSaunaAssembly;
                if (!TryGetWoohooerSaunaAssemblies(out woohooerAssembly, out woohooerSaunaAssembly))
                {
                    gameObject.AddInteraction(SaunaSit.Singleton);
                }
                gameObject.AddInteraction(EditSaunaOutfit.Singleton);
                gameObject.AddInteraction(ResetSaunaOutfit.Singleton);
                gameObject.AddInteraction(ToggleSaunaOutfit.Singleton);
            }
        }

        static void DisableSaunaOutfit(SimDescription simDescription)
        {
            if (GetSaunaOutfitEnabled(simDescription))
            {
                sSaunaOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
        }

        static void EnableSaunaOutfit(SimDescription simDescription)
        {
            if (!GetSaunaOutfitEnabled(simDescription))
            {
                sSaunaOutfitDisabledList.Remove(simDescription.SimDescriptionId);
            }
        }

        static bool GetSaunaOutfitEnabled(SimDescription simDescription)
        {
            return !sSaunaOutfitDisabledList.Contains(simDescription.SimDescriptionId);
        }

        static void OnObjectPlacedInLot(object sender, EventArgs e)
        {
            World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs = e as World.OnObjectPlacedInLotEventArgs;
            if (onObjectPlacedInLotEventArgs != null)
            {
                AddInteractions(GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId) as SaunaClassic);
            }
        }

        static void OnPreLoad()
        {
            SaunaClassic.BatheInWater.MudSingleton = new BatheInWater.DefinitionModified(BatheInWater.DefinitionModified.BathType.Mud);
            SaunaClassic.BatheInWater.WaterSingleton = new BatheInWater.DefinitionModified(BatheInWater.DefinitionModified.BathType.Water);
            SaunaClassic.MakeSteam.Singleton = new MakeSteam.DefinitionModified();
            Common.CopyTuning(typeof(SaunaClassic), typeof(SaunaClassic.BatheInWater.Definition), typeof(BatheInWater.DefinitionModified));
            Common.CopyTuning(typeof(SaunaClassic), typeof(SaunaClassic.MakeSteam.Definition), typeof(MakeSteam.DefinitionModified));
        }

        static ListenerAction OnSimDescriptionDisposed(Event e)
        {
            try
            {
                Sim sim = e.TargetObject as Sim;
                if (sim != null)
                {
                    EnableSaunaOutfit(sim.SimDescription);
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
            UpdateListeners();
            new List<SaunaClassic>(Sims3.Gameplay.Queries.GetObjects<SaunaClassic>()).ForEach(AddInteractions);
            if (Household.ActiveHousehold != null)
            {
                Household.ActiveHousehold.Sims.ForEach(AddInteractions);
            }
        }

        static void OnWorldQuit(object sender, EventArgs e)
        {
            EventTracker.RemoveListener(sSimDescriptionDisposedListener);
            EventTracker.RemoveListener(sSimSelectedListener);
            sSimDescriptionDisposedListener = null;
            sSimSelectedListener = null;
        }

        public static bool StateMachineEnterAndSit(MultiSeatObject ths, StateMachineClient smc, SittingPosture sitPosture, Slot routingSlot, object sitContext)
        {
            if (!StateMachineEnterAndSit1(ths.Sittable, smc, sitPosture, routingSlot, sitContext))
            {
                return false;
            }
            SittableComponent.SitContext sitContext2 = sitContext as SittableComponent.SitContext;
            if (sitContext2 != null && sitContext2.PreferredSeat != null && sitContext2.PreferredSeat.ContainedSim == null)
            {
                Scoot scoot = (Scoot)Scoot.Singleton.CreateInstance(sitPosture.Container, sitPosture.Sim, sitPosture.Sim.InteractionQueue.GetHeadInteraction().GetPriority(), false, true);
                scoot.TargetSeat = sitContext2.PreferredSeat as Seat;
                if (scoot.TargetSeat != null)
                {
                    sitPosture.Sim.InteractionQueue.AddNext(scoot);
                }
            }
            if (ths.SculptureComponent != null && ths.SculptureComponent.Material == SculptureComponent.SculptureMaterial.Ice)
            {
                sitPosture.Sim.BuffManager.AddElementPaused(BuffNames.Chilly, Origin.FromSittingOnIce);
            }
            return true;
        }

        public static bool StateMachineEnterAndSit0(SaunaClassic ths, bool forWoohoo, StateMachineClient smc, SittingPosture sitPosture, Slot routingSlot, object sitContext)
        {
            Assembly woohooerAssembly, woohooerSaunaAssembly;
            if (!TryGetWoohooerSaunaAssemblies(out woohooerAssembly, out woohooerSaunaAssembly) || sitPosture.Sim.CarryingChildPosture != null || sitPosture.Sim.CarryingPetPosture != null)
            {
                return false;
            }
            if (!sitPosture.Sim.HasTrait(TraitNames.NeverNude))
            {
                bool flag = false;
                object settings = woohooerAssembly.GetType("NRaas.Woohooer").GetProperty("Settings", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
                if ((bool)settings.GetType().GetField("mNakedOutfitSaunaGeneral").GetValue(settings) || (forWoohoo && (bool)settings.GetType().GetField("mNakedOutfitSaunaWoohoo").GetValue(settings)))
                {
                    if (sitPosture.Sim.SimDescription.Teen)
                    {
                        if ((bool)settings.GetType().GetField("mAllowTeenWoohoo").GetValue(settings))
                        {
                            flag = true;
                        }
                    }
                    else if (sitPosture.Sim.SimDescription.YoungAdultOrAbove)
                    {
                        flag = true;
                    }
                }
                if (flag)
                {
                    sitPosture.Sim.SwitchToOutfitWithSpin(OutfitCategories.Naked, 0);
                    settings.GetType().GetMethod("AddChange").Invoke(settings, new object[]
                        {
                            sitPosture.Sim
                        });
                }
                else if (GetSaunaOutfitEnabled(sitPosture.Sim.SimDescription))
                {
                    if (sitPosture.Sim.SimDescription.HasSpecialOutfit(kSaunaSpecialOutfitKey))
                    {
                        sitPosture.Sim.SwitchToOutfitWithSpin(OutfitCategories.Special, sitPosture.Sim.SimDescription.GetSpecialOutfitIndexFromKey(ResourceUtils.HashString32(kSaunaSpecialOutfitKey)));
                    }
                    else
                    {
                        sitPosture.Sim.SwitchToOutfitWithSpin(Sim.ClothesChangeReason.GoingToSwim);
                    }
                }
            }
            if (!StateMachineEnterAndSit(ths, smc, sitPosture, routingSlot, sitContext))
            {
                return false;
            }
            sitPosture.Interactions.Remove(new InteractionObjectPair(StartSeatedCuddleA.Singleton, sitPosture.Sim));
            sitPosture.Sim.RemoveInteractionByType(StartSeatedCuddleA.Singleton);
            List<InteractionDefinition> mSocialInteractionDefinitions = ((Posture)sitPosture).mSocialInteractionDefinitions;
            if (mSocialInteractionDefinitions != null)
            {
                mSocialInteractionDefinitions.Remove(StartSeatedCuddleA.Singleton);
            }
            sitPosture.AddInteraction(SaunaClassic.CuddleSeatedWooHooSauna.Singleton, sitPosture.Sim);
            sitPosture.AddInteraction(SaunaClassic.CuddleSeatedWooHooSauna.TryForBoySingleton, sitPosture.Sim);
            sitPosture.AddInteraction(SaunaClassic.CuddleSeatedWooHooSauna.TryForGirlSingleton, sitPosture.Sim);
            sitPosture.AddInteraction(SaunaClassic.StartSaunaSeatedCuddleA.Singleton, sitPosture.Sim);
            sitPosture.AddSocialInteraction(SaunaClassic.StartSaunaSeatedCuddleA.Singleton);
            sitPosture.Sim.BuffManager.AddElementPaused((BuffNames)11132721365296021528uL, Origin.None);
            return true;
        }

        public static bool StateMachineEnterAndSit1(SittableComponent ths, StateMachineClient smc, SittingPosture sitPosture, Slot routingSlot, object sitContext)
        {
            if (smc == null || sitPosture == null)
            {
                return false;
            }
            SitData target = sitPosture.Part.Target;
            bool flag = ths.Owner.BoobyTrapComponent != null && ths.Owner.BoobyTrapComponent.CanTriggerTrap(sitPosture.Sim.SimDescription);
            smc.SetParameter("isBoobyTrapped", flag);
            smc.SetParameter("sitTemplateSuffix", target.IKSuffix);
            smc.EnterState("x", ths.GetEnterStateName(routingSlot));
            smc.RequestState("x", ths.GetSitStateName());
            if (flag)
            {
                (ths.Owner as Sims3.Gameplay.Interfaces.IBoobyTrap).TriggerTrap(sitPosture.Sim);
                smc.SetParameter("isBoobyTrapped", false);
            }
            ths.TurnOnFootDiscouragmentArea(target);
            return true;
        }

        public static bool TryGetWoohooerSaunaAssemblies(out Assembly woohooerAssembly, out Assembly woohooerSaunaAssembly)
        {
            woohooerAssembly = null;
            woohooerSaunaAssembly = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                switch (assembly.GetName().Name)
                {
                    case "NRaasWoohooer":
                        woohooerAssembly = assembly;
                        break;
                    case "NRaasWoohooerSauna":
                        woohooerSaunaAssembly = assembly;
                        break;
                }
                if (woohooerAssembly != null && woohooerSaunaAssembly != null)
                {
                    return true;
                }
            }
            return false;
        }

        static void UpdateListeners()
        {
            if (sSimDescriptionDisposedListener != null)
            {
                EventTracker.RemoveListener(sSimDescriptionDisposedListener);
                sSimDescriptionDisposedListener = null;
            }
            if (sSimSelectedListener != null)
            {
                EventTracker.RemoveListener(sSimSelectedListener);
                sSimSelectedListener = null;
            }
            sSimDescriptionDisposedListener = EventTracker.AddListener(EventTypeId.kSimDescriptionDisposed, OnSimDescriptionDisposed);
            sSimSelectedListener = EventTracker.AddListener(EventTypeId.kEventSimSelected, OnSimSelected);
        }
    }
}