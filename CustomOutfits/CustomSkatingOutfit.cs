using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Objects.ShelvesStorage;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using System;
using System.Collections.Generic;
using static Destrospean.Common;
using static Sims3.Gameplay.Destrospean.CustomOutfits;

namespace Destrospean
{
    public class CustomSkatingOutfit
    {
        [Tunable]
        protected static bool kInstantiator;

        [PersistableStatic]
        static EventListener sObjectBoughtListener;

        [PersistableStatic]
        static EventListener sSimSelectedListener;

        public enum SkatingTypes
        {
            Ice,
            Roller
        }

        static CustomSkatingOutfit()
        {
            kInstantiator = false;
            sObjectBoughtListener = null;
            sSimSelectedListener = null;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            World.sOnWorldQuitEventHandler += OnWorldQuit;
        }

        public class EditSkatingOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

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
                    return Localize(actor.IsFemale, GetLocalizationKey(mSkatingType) + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Localize(isFemale, GetLocalizationKey(mSkatingType) + "Path0"),
                        Localize(isFemale, GetLocalizationKey(mSkatingType) + "Path1")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return !((target is Sim && actor != target) || actor.SimDescription.ToddlerOrBelow || !actor.SimDescription.IsHuman || actor.SimDescription.IsRobot || isAutonomous);
                }
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

            public override bool Run()
            {
                string outfitName = GetSkatingOutfitName(Actor, mSkatingType);
                if (!Actor.SimDescription.HasSpecialOutfit(outfitName))
                {
                    Actor.SimDescription.AddSpecialOutfit(CreateSkatingOutfit(Actor, mSkatingType), outfitName);
                }
                return EditSpecialOutfit(Actor, GetLocalizationKey(mSkatingType), outfitName);
            }

            public void SetSkatingType(SkatingTypes skatingType)
            {
                mSkatingType = skatingType;
            }
        }

        public class ResetSkatingOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

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
                    return Localize(actor.IsFemale, GetLocalizationKey(mSkatingType) + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Localize(isFemale, GetLocalizationKey(mSkatingType) + "Path0"),
                        Localize(isFemale, GetLocalizationKey(mSkatingType) + "Path1")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return !(!actor.SimDescription.HasSpecialOutfit(GetSkatingOutfitName(actor, mSkatingType)) || (target is Sim && actor != target) || actor.SimDescription.ToddlerOrBelow || !actor.SimDescription.IsHuman || actor.SimDescription.IsRobot || isAutonomous);
                }
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

            public override bool Run()
            {
                Actor.SimDescription.RemoveSpecialOutfit(GetSkatingOutfitName(Actor, mSkatingType));
                Notify(Localize(Actor.IsFemale, GetLocalizationKey(mSkatingType) + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
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
                    if (interactionToJoin.Target is ISkatableObject skatableObject)
                    {
                        ISkatableObject skatableObjectForAskToJoin = skatableObject.GetSkatableObjectForAskToJoin(target);
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
            if (gameObject == null || !GameUtils.IsInstalled(ProductVersion.EP8))
            {
                return;
            }
            foreach (InteractionObjectPair interaction in gameObject.Interactions)
            {
                if (interaction.InteractionDefinition.GetType() == EditSkatingOutfit.Singleton.GetType())
                {
                    return;
                }
            }
            gameObject.AddInteraction(EditSkatingOutfit.Singleton);
            gameObject.AddInteraction(ResetSkatingOutfit.Singleton);
        }

        public static bool CreateSkatingOutfit(Sim actor, bool isIceRink, ref int skateOutfitIndex)
        {
            SimOutfit simOutfit = null;
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
            if (simOutfit != null || OutfitUtils.TryGenerateSimOutfit(outfitName, ProductVersion.EP8, out simOutfit))
            {
                SimOutfit currentOutfit = actor.CurrentOutfit;
                if (OutfitUtils.TryApplyUniformToOutfit(GetOutfitWithoutShoes(actor, currentOutfit), simOutfit, simDescription, "CreateSkatingOutfit", out var resultOutfit))
                {
                    skateOutfitIndex = simDescription.AddSpecialOutfit(resultOutfit, "SkatingOutfit");
                    return true;
                }
            }
            return false;
        }

        public static SimOutfit CreateSkatingOutfit(Sim actor, SkatingTypes skatingType)
        {
            return OutfitUtils.TryGenerateSimOutfit(GetSkatingOutfitName(actor, skatingType), ProductVersion.EP8, out var simOutfit) && OutfitUtils.TryApplyUniformToOutfit(actor.CurrentOutfit, simOutfit, actor.SimDescription, "CreateSkatingOutfit", out var resultOutfit) ? resultOutfit : null;
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

        static void Init()
        {
            UpdateListeners();
        }

        static ListenerAction OnObjectBought(Event e)
        {
            try
            {
                if (kShowObjectMenu && e.TargetObject is Dresser dresser)
                {
                    AddInteractions(dresser);
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
            SkatingRink.Skate.Singleton = new Skate.DefinitionModified();
            CopyTuning(typeof(SkatingRink), typeof(SkatingRink.Skate.Definition), typeof(Skate.DefinitionModified));
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
                foreach (Dresser dresser in Sims3.Gameplay.Queries.GetObjects<Dresser>())
                {
                    AddInteractions(dresser);
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
            EventTracker.RemoveListener(sSimSelectedListener);
            sObjectBoughtListener = null;
            sSimSelectedListener = null;
        }

        static void UpdateListeners()
        {
            if (sObjectBoughtListener != null)
            {
                EventTracker.RemoveListener(sObjectBoughtListener);
                sObjectBoughtListener = null;
            }
            if (sSimSelectedListener != null)
            {
                EventTracker.RemoveListener(sSimSelectedListener);
                sSimSelectedListener = null;
            }
            sObjectBoughtListener = EventTracker.AddListener(EventTypeId.kBoughtObject, OnObjectBought);
            sSimSelectedListener = EventTracker.AddListener(EventTypeId.kEventSimSelected, OnSimSelected);
        }
    }
}