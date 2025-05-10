using MonoPatcherLib;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Controllers;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.ShelvesStorage;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.Store.Objects;
using Sims3.UI;
using System;
using System.Collections.Generic;
using static Destrospean.Common;
using static Sims3.Gameplay.Destrospean.CustomOutfits;

namespace Destrospean
{
    public class CustomSingedOutfit
    {
        [Tunable]
        protected static bool kInstantiator;

        public static readonly string kSingedSpecialOutfitKey = "Singed";

        [PersistableStatic]
        static EventListener sObjectBoughtListener;

        [PersistableStatic]
        static EventListener sSimSelectedListener;

        static CustomSingedOutfit()
        {
            kInstantiator = false;
            sObjectBoughtListener = null;
            sSimSelectedListener = null;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            World.sOnWorldQuitEventHandler += OnWorldQuit;
        }

        public class EditSingedOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomSingedOutfit/EditSingedOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditSingedOutfit>
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
                    return !((target is Sim && actor != target) || actor.SimDescription.ToddlerOrBelow || !actor.SimDescription.IsHuman || actor.SimDescription.IsRobot || isAutonomous);
                }
            }

            public override bool Run()
            {
                if (!Actor.SimDescription.HasSpecialOutfit(kSingedSpecialOutfitKey) && !string.IsNullOrEmpty(OutfitUtils.GetSingedOutfit(Actor)))
                {
                    Actor.SimDescription.AddSpecialOutfit(CreateSingedOutfit(Actor), kSingedSpecialOutfitKey);
                }
                return EditSpecialOutfit(Actor, sLocalizationKey, kSingedSpecialOutfitKey);
            }
        }

        public class ResetSingedOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomSingedOutfit/ResetSingedOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ResetSingedOutfit>
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
                    return !(!actor.SimDescription.HasSpecialOutfit(kSingedSpecialOutfitKey) || (target is Sim && actor != target) || actor.SimDescription.ToddlerOrBelow || !actor.SimDescription.IsHuman || actor.SimDescription.IsRobot || isAutonomous);
                }
            }

            public override bool Run()
            {
                Actor.SimDescription.RemoveSpecialOutfit(kSingedSpecialOutfitKey);
                Notify(Localize(Actor.IsFemale, sLocalizationKey + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                return true;
            }
        }

        public class ShootFireball : BabyDragon.ShootFireball
        {
            public class DefinitionModified : InteractionDefinition<Sim, BabyDragon, ShootFireball>
            {
                Definition mDefinitionBase = new Definition();

                public override bool Test(Sim actor, BabyDragon target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return mDefinitionBase.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
                }

                public override string GetInteractionName(Sim actor, BabyDragon target, InteractionObjectPair interaction)
                {
                    return mDefinitionBase.GetInteractionName(actor, target, interaction);
                }

                public override void PopulatePieMenuPicker(ref InteractionInstanceParameters parameters, out List<ObjectPicker.TabInfo> listObjs, out List<ObjectPicker.HeaderInfo> headers, out int NumSelectableRows)
                {
                    mDefinitionBase.PopulatePieMenuPicker(ref parameters, out listObjs, out headers, out NumSelectableRows);
                }
            }

            public new void OnAnimationEvent(StateMachineClient stateMachineClient, IEvent e)
            {
                if (stateMachineClient == null)
                {
                    return;
                }
                switch (e.EventId)
                {
                    case 201u:
                        Target.StartDragonVFX(ref Target.mInitialFXHandle, "store_babyDragon_summonR");
                        break;
                    case 202u:
                        Target.StopVFX(ref Target.mInitialFXHandle);
                        Target.StartDragonVFX(ref Target.mCastSpellFXHandle, "store_babyDragon_fireball");
                        break;
                    case 302u:
                        Vector3 position = TargetSim.Position;
                        if (mMissTarget)
                        {
                            position = Actor.Position + Actor.ForwardVector * 2.75f;
                            FireManager.AddFire(position);
                            VisualEffect visualEffect = VisualEffect.Create("store_babyDragon_fbtarget");
                            visualEffect.SetPosAndOrient(position, Actor.ForwardVector, Actor.UpVector);
                            visualEffect.Start(VisualEffect.TransitionType.SoftTransition, false);
                            visualEffect.SetAutoDestroy(true);
                        }
                        else
                        {
                            Target.StartSimVFX(TargetSim, ref Target.mFireballFXHandle, "store_babyDragon_fbtarget", Sim.FXJoints.Pelvis);
                            SimDescription simDescription = TargetSim.SimDescription;
                            bool hasSingedOutfit = simDescription.HasSpecialOutfit(kSingedSpecialOutfitKey);
                            if (hasSingedOutfit || !string.IsNullOrEmpty(OutfitUtils.GetSingedOutfit(TargetSim)))
                            {
                                SimOutfit simOutfit = hasSingedOutfit ? simDescription.GetSpecialOutfit(kSingedSpecialOutfitKey) : CreateSingedOutfit(TargetSim);
                                if (simDescription.GetOutfit(OutfitCategories.Singed, 0) != simOutfit)
                                {
                                    simDescription.AddOutfit(simOutfit, OutfitCategories.Singed, true);
                                }
                            }
                            TargetSim.SwitchToOutfitWithoutSpin(Sim.ClothesChangeReason.Force, OutfitCategories.Singed, true);
                            TargetSim.BuffManager.AddElement(BuffNames.Singed, Origin.None);
                        }
                        LotLocation location = LotLocation.Invalid;
                        ulong lotLocation = World.GetLotLocation(position, ref location);
                        FireManager.BurnTile(lotLocation, location);
                        break;
                }
            }
        }

        static void AddInteractions(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }
            foreach (InteractionObjectPair interaction in gameObject.Interactions)
            {
                if (interaction.InteractionDefinition.GetType() == EditSingedOutfit.Singleton.GetType())
                {
                    return;
                }
            }
            gameObject.AddInteraction(EditSingedOutfit.Singleton);
            gameObject.AddInteraction(ResetSingedOutfit.Singleton);
        }

        public static SimOutfit CreateSingedOutfit(Sim actor)
        {
            SimBuilder simBuilder = new SimBuilder
            {
                UseCompression = true
            };
            SimDescription simDescription = actor.SimDescription;
            SimOutfit simOutfit = new SimOutfit(OutfitUtils.ApplyUniformToOutfit(simDescription.GetOutfit(OutfitCategories.Everyday, 0), new SimOutfit(ResourceKey.CreateOutfitKey(OutfitUtils.GetSingedOutfit(actor), 0u)), simDescription, "CreateSingedOutfit"));
            OutfitUtils.SetOutfit(simBuilder, simOutfit, simDescription);
            foreach (CASPart part in simOutfit.Parts)
            {
                if (part.BodyType == BodyTypes.Blush)
                {
                    simBuilder.RemovePart(part);
                }
            }
            simBuilder.AddPart(new CASPart(new ResourceKey(ResourceUtils.HashString64((simDescription.Child ? "cu" : "af") + "Scalp_burnt"), 55242443u, 0u)));
            return new SimOutfit(simBuilder.CacheOutfit("Singed" + simDescription.SimDescriptionId));
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

        static void OnPreLoad()
        {
            BabyDragon.ShootFireball.Singleton = new ShootFireball.DefinitionModified();
            CopyTuning(typeof(BabyDragon), typeof(BabyDragon.ShootFireball.Definition), typeof(ShootFireball.DefinitionModified));
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

        [ReplaceMethod(typeof(BuffSinged), nameof(BuffSinged.SetupSingedOutfit))]
        public static void SetupSingedOutfit(Sim actor)
        {
            SimDescription simDescription = actor.SimDescription;
            bool hasSingedOutfit = simDescription.HasSpecialOutfit(kSingedSpecialOutfitKey);
            if (hasSingedOutfit || !string.IsNullOrEmpty(OutfitUtils.GetSingedOutfit(actor)))
            {
                SimOutfit simOutfit = hasSingedOutfit ? simDescription.GetSpecialOutfit(kSingedSpecialOutfitKey) : CreateSingedOutfit(actor);
                if (simDescription.GetOutfit(OutfitCategories.Singed, 0) != simOutfit)
                {
                    simDescription.AddOutfit(simOutfit, OutfitCategories.Singed, true);
                }
            }
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