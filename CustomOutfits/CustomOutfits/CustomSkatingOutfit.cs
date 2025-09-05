using System;
using System.Collections.Generic;
using Destrospean.CustomOutfits;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Pools;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using Tuning = Sims3.Gameplay.Destrospean.CustomOutfits;

namespace Destrospean
{
    public class CustomSkatingOutfit
    {
        [Tunable]
        protected static bool kInstantiator;

        static EventListener sSimSelectedListener;

        public enum SkatingTypes
        {
            Ice,
            Roller
        }

        static CustomSkatingOutfit()
        {
            kInstantiator = false;
            sSimSelectedListener = null;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnObjectPlacedInLotEventHandler += OnObjectPlacedInLot;
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            World.sOnWorldQuitEventHandler += OnWorldQuit;
        }

        public class EditSkatingOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public GameObjectHit mHit = GameObjectHit.NoHit;

            public SkatingTypes mSkatingType;

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditSkatingOutfit>
            {
                public SkatingTypes mSkatingType;

                public Definition()
                {
                }

                public Definition(SkatingTypes skatingType)
                {
                    mSkatingType = skatingType;
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, GameObject target, List<InteractionObjectPair> results)
                {
                    foreach (SkatingTypes skatingType in Enum.GetValues(typeof(SkatingTypes)))
                    {
                        results.Add(new InteractionObjectPair(new Definition(skatingType), target));
                    }
                }

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    EditSkatingOutfit editSkatingOutfit = new EditSkatingOutfit();
                    editSkatingOutfit.SetSkatingType(mSkatingType);
                    editSkatingOutfit.Init(ref parameters);
                    return editSkatingOutfit;
                }

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return Common.Localize(actor.IsFemale, GetLocalizationKey(mSkatingType) + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, GetLocalizationKey(mSkatingType) + "Path0"),
                        Common.Localize(isFemale, GetLocalizationKey(mSkatingType) + "Path1")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }

                public override InteractionTestResult Test(ref InteractionInstanceParameters parameters, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return InteractionDefinitionUtilities.FromBool((Tuning.kShowObjectMenu && SkatableTerrain.GetPondSkatingAreaAtPoint(parameters.Hit.mPoint) != null && mSkatingType == SkatingTypes.Ice && PondManager.ArePondsFrozen() || parameters.Target is ISkatableObject && mSkatingType == (((ISkatableObject)parameters.Target).IsIceRink ? SkatingTypes.Ice : SkatingTypes.Roller) || Tuning.kShowSimMenu && parameters.Actor == parameters.Target) && parameters.Actor.SimDescription.ChildOrAbove && parameters.Actor.SimDescription.IsHuman && !parameters.Actor.SimDescription.IsRobot && !parameters.Autonomous);
                }
            }

            public override InteractionInstanceParameters GetInteractionParameters()
            {
                return new InteractionInstanceParameters(InteractionObjectPair, InstanceActor, GetPriority(), false, false, mHit);
            }

            public static string GetLocalizationKey(SkatingTypes skatingType)
            {
                return "CustomSkatingOutfit/" + new Dictionary<SkatingTypes, string>
                {
                    {
                        SkatingTypes.Ice,
                        "EditIceSkatingOutfit/"
                    },
                    {
                        SkatingTypes.Roller,
                        "EditRollerSkatingOutfit/"
                    }
                }[skatingType];
            }

            public override void Init(ref InteractionInstanceParameters parameters)
            {
                base.Init(ref parameters);
                if (mHit == GameObjectHit.NoHit)
                {
                    mHit = parameters.Hit;
                }
            }

            public override bool Run()
            {
                string outfitName = GetSkatingOutfitName(Actor, mSkatingType);
                if (!Actor.SimDescription.HasSpecialOutfit(outfitName))
                {
                    Actor.SimDescription.AddSpecialOutfit(CreateSkatingOutfit(Actor, mSkatingType), outfitName);
                }
                return Common.EditSpecialOutfit(Actor, GetLocalizationKey(mSkatingType), outfitName);
            }

            public void SetSkatingType(SkatingTypes skatingType)
            {
                mSkatingType = skatingType;
            }
        }

        public class ResetSkatingOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public GameObjectHit mHit = GameObjectHit.NoHit;

            public SkatingTypes mSkatingType;

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ResetSkatingOutfit>
            {
                public SkatingTypes mSkatingType;

                public Definition()
                {
                }

                public Definition(SkatingTypes skatingType)
                {
                    mSkatingType = skatingType;
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, GameObject target, List<InteractionObjectPair> results)
                {
                    foreach (SkatingTypes skatingType in Enum.GetValues(typeof(SkatingTypes)))
                    {
                        results.Add(new InteractionObjectPair(new Definition(skatingType), target));
                    }
                }

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    ResetSkatingOutfit resetSkatingOutfit = new ResetSkatingOutfit();
                    resetSkatingOutfit.SetSkatingType(mSkatingType);
                    resetSkatingOutfit.Init(ref parameters);
                    return resetSkatingOutfit;
                }

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return Common.Localize(actor.IsFemale, GetLocalizationKey(mSkatingType) + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, GetLocalizationKey(mSkatingType) + "Path0"),
                        Common.Localize(isFemale, GetLocalizationKey(mSkatingType) + "Path1")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }

                public override InteractionTestResult Test(ref InteractionInstanceParameters parameters, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return InteractionDefinitionUtilities.FromBool((Tuning.kShowObjectMenu && SkatableTerrain.GetPondSkatingAreaAtPoint(parameters.Hit.mPoint) != null && mSkatingType == SkatingTypes.Ice && PondManager.ArePondsFrozen() || parameters.Target is ISkatableObject && mSkatingType == (((ISkatableObject)parameters.Target).IsIceRink ? SkatingTypes.Ice : SkatingTypes.Roller) || Tuning.kShowSimMenu && parameters.Actor == parameters.Target) && parameters.Actor.SimDescription.ChildOrAbove && parameters.Actor.SimDescription.IsHuman && !parameters.Actor.SimDescription.IsRobot && !parameters.Autonomous && parameters.Actor.SimDescription.HasSpecialOutfit(GetSkatingOutfitName((Sim)parameters.Actor, mSkatingType)));
                }
            }

            public override InteractionInstanceParameters GetInteractionParameters()
            {
                return new InteractionInstanceParameters(InteractionObjectPair, InstanceActor, GetPriority(), false, false, mHit);
            }

            public static string GetLocalizationKey(SkatingTypes skatingType)
            {
                return "CustomSkatingOutfit/" + new Dictionary<SkatingTypes, string>
                {
                    {
                        SkatingTypes.Ice,
                        "ResetIceSkatingOutfit/"
                    },
                    {
                        SkatingTypes.Roller,
                        "ResetRollerSkatingOutfit/"
                    }
                }[skatingType];
            }

            public override void Init(ref InteractionInstanceParameters parameters)
            {
                base.Init(ref parameters);
                if (mHit == GameObjectHit.NoHit)
                {
                    mHit = parameters.Hit;
                }
            }

            public override bool Run()
            {
                Actor.SimDescription.RemoveSpecialOutfit(GetSkatingOutfitName(Actor, mSkatingType));
                Common.Notify(Common.Localize(Actor.IsFemale, GetLocalizationKey(mSkatingType) + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                return true;
            }

            public void SetSkatingType(SkatingTypes skatingType)
            {
                mSkatingType = skatingType;
            }
        }

        public class Skate : SkatingRink.Skate
        {
            public class DefinitionModified : InteractionDefinition<Sim, ISkatableObject, Skate>, Sim.IAskToJoinCustomIOP, Sim.IAskToJoinCustomTest
            {
                public override bool Test(Sim actor, ISkatableObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return SharedCanSkateOnObjectTest(actor, target, true, ref greyedOutTooltipCallback);
                }

                public override string GetInteractionName(Sim actor, ISkatableObject target, InteractionObjectPair interaction)
                {
                    return SkatingRink.LocalizeString(actor.IsFemale, "Skate");
                }

                public InteractionObjectPair GetInteractionForJoin(IActor actor, Sim target, InteractionObjectPair interactionToJoin, bool isAskToJoin)
                {
                    if (interactionToJoin.Target is ISkatableObject)
                    {
                        ISkatableObject skatableObjectForAskToJoin = ((ISkatableObject)target).GetSkatableObjectForAskToJoin(target);
                        return new InteractionObjectPair(Singleton, skatableObjectForAskToJoin);
                    }
                    return null;
                }

                public bool AskToJoinTest(Sim actor, Sim target, InteractionObjectPair interaction, bool isAskToJoin)
                {
                    return SkatingRink.IsSimValidToSkateWithActor(actor, target);
                }
            }

            public new bool ApproachRink()
            {
                mSkateState = SkateState.Approaching;
                if (!mIsOccultSkater && mShouldChangeOutfit && Target.AnimateGetIntoAndOutOfSkates)
                {
                    int skateOutfitIndex = mSkateOutfitIndex;
                    if (CreateSkatingOutfit(Actor, Target.IsIceRink, ref skateOutfitIndex))
                    {
                        mPreviousOutfitCategory = Actor.CurrentOutfitCategory;
                        mPreviousOutfitIndex = Actor.CurrentOutfitIndex;
                        mSkateOutfitIndex = skateOutfitIndex;
                    }
                }
                int entranceUsed;
                bool result = Target.RouteUpToSkatingLocation(Actor, this, out entranceUsed);
                mEntranceUsed = entranceUsed;
                return result;
            }

            public override bool Run()
            {
                mIsOccultSkater = CalculateIfActorIsOccultSkater();
                mShouldChangeOutfit = CalculateIfActorShouldChangeOutfit();
                if (!ApproachRink())
                {
                    return false;
                }
                if (!PutOnSkatesAndEnterRink())
                {
                    return false;
                }
                if (!WaitForSpot())
                {
                    return false;
                }
                if (!DoSkate())
                {
                    return false;
                }
                ExitRink();
                return true;
            }

            public override void Cleanup()
            {
                if (mSkateState != 0)
                {
                    if (Target != null)
                    {
                        Target.FinishedWaitingAtEntrance(mEntranceUsed, Actor);
                        Target.UnregisterSkater(Actor);
                        Target.FinishedWaitingAtExit(mExitUsed, Actor, this);
                    }
                    if (mAddedExtraInteractions)
                    {
                        RemoveExtraInteractions();
                    }
                    Actor.OnExitReasonsAdded -= RouteExitReasonsAddedCallback;
                    Actor.RoutingComponent.RemoveTravellingEventCallback(SkatingRouteCallback);
                    Actor.LookAtManager.EnableLookAts();
                    if (mPutOnSkateOutfit)
                    {
                        TakeOffSkatesEvent(null, null);
                    }
                    if (mSkateOutfitIndex != -1)
                    {
                        Actor.SimDescription.RemoveSpecialOutfit("SkatingOutfit");
                        mSkateOutfitIndex = -1;
                    }
                    DestroySingleSpinJig();
                    DestroyCouplesSpinJig();
                }
                base.Cleanup();
            }
        }

        static void AddInteractions(GameObject gameObject)
        {
            if (gameObject != null && !gameObject.Interactions.Exists(interaction => interaction.InteractionDefinition.GetType() == EditSkatingOutfit.Singleton.GetType()) && GameUtils.IsInstalled(ProductVersion.EP8))
            {
                gameObject.AddInteraction(EditSkatingOutfit.Singleton);
                gameObject.AddInteraction(ResetSkatingOutfit.Singleton);
            }
        }

        public static bool CreateSkatingOutfit(Sim actor, bool isIceRink, ref int skateOutfitIndex)
        {
            SimOutfit resultOutfit, simOutfit = null;
            SimDescription simDescription = actor.SimDescription;
            uint specialOutfitKey = ResourceUtils.HashString32("SkatingOutfit");
            int specialOutfitIndexFromKey = simDescription.GetSpecialOutfitIndexFromKey(specialOutfitKey);
            if (specialOutfitIndexFromKey != -1)
            {
                skateOutfitIndex = specialOutfitIndexFromKey;
                return true;
            }
            string outfitName = SkatingRink.Skate.CreateSkateOutfitName(actor, isIceRink);
            if (simDescription.HasSpecialOutfit(outfitName))
            {
                simOutfit = GetSkatingOutfitShoesOnly(actor, isIceRink ? SkatingTypes.Ice : SkatingTypes.Roller);
            }
            if ((simOutfit != null || OutfitUtils.TryGenerateSimOutfit(outfitName, ProductVersion.EP8, out simOutfit)) && OutfitUtils.TryApplyUniformToOutfit(GetOutfitWithoutShoes(actor, actor.CurrentOutfit), simOutfit, simDescription, "CreateSkatingOutfit", out resultOutfit))
            {
                skateOutfitIndex = simDescription.AddSpecialOutfit(resultOutfit, specialOutfitKey);
                return true;
            }
            return false;
        }

        public static SimOutfit CreateSkatingOutfit(Sim actor, SkatingTypes skatingType)
        {
            SimOutfit resultOutfit, simOutfit;
            return OutfitUtils.TryGenerateSimOutfit(GetSkatingOutfitName(actor, skatingType), ProductVersion.EP8, out simOutfit) && OutfitUtils.TryApplyUniformToOutfit(actor.CurrentOutfit, simOutfit, actor.SimDescription, "CreateSkatingOutfit", out resultOutfit) ? resultOutfit : null;
        }

        public static SimOutfit GetOutfitWithoutShoes(Sim actor, SimOutfit outfit)
        {
            SimBuilder simBuilder = new SimBuilder
            {
                UseCompression = true
            };
            OutfitUtils.SetOutfit(simBuilder, outfit, actor.SimDescription);
            foreach (CASPart part in outfit.Parts)
            {
                if (part.BodyType == BodyTypes.Shoes)
                {
                    simBuilder.RemovePart(part);
                }
            }
            return new SimOutfit(simBuilder.CacheOutfit("SkatingRestOfOutfit" + actor.SimDescription.SimDescriptionId));
        }

        public static SimOutfit GetSkatingOutfitShoesOnly(Sim actor, SkatingTypes skatingType)
        {
            SimBuilder simBuilder = new SimBuilder
            {
                UseCompression = true
            };
            SimDescription simDescription = actor.SimDescription;
            SimOutfit simOutfit = simDescription.GetSpecialOutfit(GetSkatingOutfitName(actor, skatingType));
            OutfitUtils.SetOutfit(simBuilder, simOutfit, simDescription);
            foreach (CASPart part in simOutfit.Parts)
            {
                if (part.BodyType != BodyTypes.Shoes)
                {
                    simBuilder.RemovePart(part);
                }
            }
            return new SimOutfit(simBuilder.CacheOutfit("SkatingShoes" + simDescription.SimDescriptionId));
        }

        public static string GetSkatingOutfitName(Sim actor, SkatingTypes skatingType)
        {
            return SkatingRink.Skate.CreateSkateOutfitName(actor, skatingType == SkatingTypes.Ice);
        }

        static void OnObjectPlacedInLot(object sender, EventArgs e)
        {
            World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs = e as World.OnObjectPlacedInLotEventArgs;
            if (onObjectPlacedInLotEventArgs != null)
            {
                AddInteractions(GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId) as ISkatableObject as GameObject);
            }
        }

        static void OnPreLoad()
        {
            SkatingRink.Skate.Singleton = new Skate.DefinitionModified();
            Common.CopyTuning(typeof(SkatingRink), typeof(SkatingRink.Skate.Definition), typeof(Skate.DefinitionModified));
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
            new List<SkatingRink>(Sims3.Gameplay.Queries.GetObjects<SkatingRink>()).ForEach(AddInteractions);
            new List<Terrain>(Sims3.Gameplay.Queries.GetObjects<Terrain>()).ForEach(AddInteractions);
            if (Household.ActiveHousehold != null)
            {
                Household.ActiveHousehold.Sims.ForEach(AddInteractions);
            }
        }

        static void OnWorldQuit(object sender, EventArgs e)
        {
            EventTracker.RemoveListener(sSimSelectedListener);
            sSimSelectedListener = null;
        }

        static void UpdateListeners()
        {
            if (sSimSelectedListener != null)
            {
                EventTracker.RemoveListener(sSimSelectedListener);
                sSimSelectedListener = null;
            }
            sSimSelectedListener = EventTracker.AddListener(EventTypeId.kEventSimSelected, OnSimSelected);
        }
    }
}