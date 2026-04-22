using System;
using System.Collections.Generic;
using Destrospean.CustomOutfits;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Plumbing;
using Sims3.Gameplay.Pools;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using Tuning = Sims3.Gameplay.Destrospean.CustomOutfits;

namespace Destrospean
{
    public class CustomTowelOutfit
    {
        [Tunable]
        protected static bool kInstantiator;

        public static readonly string kTowelSpecialOutfitKey = "SkinnyDipTowel";

        static EventListener sSimSelectedListener;

        static CustomTowelOutfit()
        {
            Common.ReplaceMethod(typeof(SkinnyDipClothingPile).GetMethod("ChangeSimToTowelOutfit", (System.Reflection.BindingFlags)0x28), typeof(CustomTowelOutfit).GetMethod("ChangeSimToTowelOutfit"));
            sSimSelectedListener = null;
            World.sOnObjectPlacedInLotEventHandler += OnObjectPlacedInLot;
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            World.sOnWorldQuitEventHandler += OnWorldQuit;
        }

        public class EditTowelOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public GameObjectHit mHit = GameObjectHit.NoHit;

            public const string sLocalizationKey = "CustomTowelOutfit/EditTowelOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditTowelOutfit>
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
                    return true;
                }

                public override InteractionTestResult Test(ref InteractionInstanceParameters parameters, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return InteractionDefinitionUtilities.FromBool(((Tuning.kShowObjectMenu && ((Pool.GetPoolNearestPoint(parameters.Hit.mPoint) != null && parameters.Hit.mType != GameObjectHitType.WaterFountain) || parameters.Target is HotTubBase)) || (Tuning.kShowSimMenu && parameters.Target is Sim && parameters.Actor == parameters.Target)) && parameters.Actor.SimDescription.YoungAdultOrAbove && parameters.Actor.SimDescription.IsHuman && !parameters.Actor.SimDescription.IsRobot && !parameters.Autonomous);
                }
            }

            public override InteractionInstanceParameters GetInteractionParameters()
            {
                return new InteractionInstanceParameters(InteractionObjectPair, InstanceActor, GetPriority(), false, false, mHit);
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
                return Common.EditSpecialOutfit(Actor, sLocalizationKey, kTowelSpecialOutfitKey, GetTowelOutfitName(Actor), ProductVersion.EP3, Actor.SimDescription.GetOutfit(OutfitCategories.Swimwear, 0));
            }
        }

        public class ResetTowelOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public GameObjectHit mHit = GameObjectHit.NoHit;

            public const string sLocalizationKey = "CustomTowelOutfit/ResetTowelOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ResetTowelOutfit>
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
                    return true;
                }

                public override InteractionTestResult Test(ref InteractionInstanceParameters parameters, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return InteractionDefinitionUtilities.FromBool(((Tuning.kShowObjectMenu && ((Pool.GetPoolNearestPoint(parameters.Hit.mPoint) != null && parameters.Hit.mType != GameObjectHitType.WaterFountain) || parameters.Target is HotTubBase)) || (Tuning.kShowSimMenu && parameters.Target is Sim && parameters.Actor == parameters.Target)) && parameters.Actor.SimDescription.YoungAdultOrAbove && parameters.Actor.SimDescription.IsHuman && !parameters.Actor.SimDescription.IsRobot && !parameters.Autonomous && parameters.Actor.SimDescription.HasSpecialOutfit(kTowelSpecialOutfitKey));
                }
            }

            public override InteractionInstanceParameters GetInteractionParameters()
            {
                return new InteractionInstanceParameters(InteractionObjectPair, InstanceActor, GetPriority(), false, false, mHit);
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
                Actor.SimDescription.RemoveSpecialOutfit(kTowelSpecialOutfitKey);
                Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                return true;
            }
        }

        static void AddInteractions(GameObject gameObject)
        {
            if (gameObject != null && !gameObject.Interactions.Exists(interaction => interaction.InteractionDefinition.GetType() == EditTowelOutfit.Singleton.GetType()) && GameUtils.IsInstalled(ProductVersion.EP3))
            {
                gameObject.AddInteraction(EditTowelOutfit.Singleton);
                gameObject.AddInteraction(ResetTowelOutfit.Singleton);
            }
        }

        //[ReplaceMethod(typeof(SkinnyDipClothingPile), "ChangeSimToTowelOutfit")]
        public static void ChangeSimToTowelOutfit(Sim actor)
        {
            SimDescription simDescription = actor.SimDescription;
            simDescription.RemoveOutfits(OutfitCategories.SkinnyDippingTowel, true);
            SimOutfit simOutfit;
            if (simDescription.HasSpecialOutfit(kTowelSpecialOutfitKey))
            {
                simDescription.AddOutfit(simDescription.GetSpecialOutfit(kTowelSpecialOutfitKey), OutfitCategories.SkinnyDippingTowel, true);
                simOutfit = simDescription.GetOutfit(OutfitCategories.SkinnyDippingTowel, 0);
            }
            else
            {
                simOutfit = OutfitUtils.CreateOutfitForSim(simDescription, ResourceKey.CreateOutfitKeyFromProductVersion(GetTowelOutfitName(actor), ProductVersion.EP3), OutfitCategories.SkinnyDippingTowel, OutfitCategories.Swimwear, true);
            }
            if (simOutfit != null)
            {
                actor.SwitchToOutfitWithSpin(simOutfit.Key);
            }
            actor.BuffManager.AddElement(BuffNames.EmbarrassedClothesHidden, Origin.FromClothingHidden);
            if (actor.IsInActiveHousehold)
            {
                if (SkinnyDipClothingPile.sSimsShownClothingStolenTNS == null)
                {
                    SkinnyDipClothingPile.sSimsShownClothingStolenTNS = new PairedListDictionary<ulong, bool>();
                }
                if (!SkinnyDipClothingPile.sSimsShownClothingStolenTNS.ContainsKey(simDescription.SimDescriptionId))
                {
                    SkinnyDipClothingPile.sSimsShownClothingStolenTNS.Add(simDescription.SimDescriptionId, true);
                    NotificationSystem.Show(TNSNames.SkinnyDippingClothesHidden, null, actor, null, null, new bool[]
                    {
                        actor.IsFemale
                    }, false, null, simDescription);
                }
            }
            if (actor.HasTrait(TraitNames.HotHeaded) || actor.HasTrait(TraitNames.NoSenseOfHumor) || actor.HasTrait(TraitNames.Grumpy))
            {
                actor.PlayReaction(ReactionTypes.TantrumMild, ReactionSpeed.ImmediateWithoutOverlay);
                return;
            }
            if (actor.HasTrait(TraitNames.OverEmotional) || actor.HasTrait(TraitNames.Dramatic))
            {
                actor.PlayReaction(ReactionTypes.Cry, ReactionSpeed.ImmediateWithoutOverlay);
                return;
            }
            ReactionTypes[] randomList = new ReactionTypes[]
            {
                ReactionTypes.Annoyed,
                ReactionTypes.Awkward,
                ReactionTypes.WhyMe
            };
            actor.PlayReaction(RandomUtil.GetRandomObjectFromList(randomList), ReactionSpeed.ImmediateWithoutOverlay);
        }

        public static string GetTowelOutfitName(Sim actor)
        {
            return (actor.IsMale ? "m" : "f") + OutfitUtils.GetAgePrefix(actor.SimDescription.Age, true) + "_towel";
        }

        static void OnObjectPlacedInLot(object sender, EventArgs e)
        {
            World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs = e as World.OnObjectPlacedInLotEventArgs;
            if (onObjectPlacedInLotEventArgs != null)
            {
                AddInteractions(GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId) as HotTubBase);
            }
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
            new List<HotTubBase>(Sims3.Gameplay.Queries.GetObjects<HotTubBase>()).ForEach(AddInteractions);
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