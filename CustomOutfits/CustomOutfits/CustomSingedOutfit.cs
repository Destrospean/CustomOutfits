using System;
using System.Collections.Generic;
using Destrospean.CustomOutfits;
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
using Tuning = Sims3.Gameplay.Destrospean.CustomOutfits;

namespace Destrospean
{
    public class CustomSingedOutfit
    {
        [Tunable]
        protected static bool kInstantiator;

        public static readonly string kSingedSpecialOutfitKey = "Singed";

        static EventListener sSimSelectedListener;

        static CustomSingedOutfit()
        {
            Common.ReplaceMethod(typeof(BuffSinged).GetMethod("SetupSingedOutfit"), typeof(CustomSingedOutfit).GetMethod("SetupSingedOutfit"));
            sSimSelectedListener = null;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnObjectPlacedInLotEventHandler += OnObjectPlacedInLot;
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
                    return (actor == target && Tuning.kShowSimMenu || target as Sim == null && Tuning.kShowObjectMenu) && actor.SimDescription.ChildOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public override bool Run()
            {
                if (!Actor.SimDescription.HasSpecialOutfit(kSingedSpecialOutfitKey) && !string.IsNullOrEmpty(OutfitUtils.GetSingedOutfit(Actor)))
                {
                    Actor.SimDescription.AddSpecialOutfit(CreateSingedOutfit(Actor), kSingedSpecialOutfitKey);
                }
                return Common.EditSpecialOutfit(Actor, sLocalizationKey, kSingedSpecialOutfitKey);
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
                    return (actor == target && Tuning.kShowSimMenu || target as Sim == null && Tuning.kShowObjectMenu) && actor.SimDescription.ChildOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous && actor.SimDescription.HasSpecialOutfit(kSingedSpecialOutfitKey);
                }
            }

            public override bool Run()
            {
                Actor.SimDescription.RemoveSpecialOutfit(kSingedSpecialOutfitKey);
                Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
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
                    case 201:
                        Target.StartDragonVFX(ref Target.mInitialFXHandle, "store_babyDragon_summonR");
                        break;
                    case 202:
                        Target.StopVFX(ref Target.mInitialFXHandle);
                        Target.StartDragonVFX(ref Target.mCastSpellFXHandle, "store_babyDragon_fireball");
                        break;
                    case 302:
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
                            SetupSingedOutfit(TargetSim);
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
            if (gameObject != null && !gameObject.Interactions.Exists(interaction => interaction.InteractionDefinition.GetType() == EditSingedOutfit.Singleton.GetType()))
            {
                gameObject.AddInteraction(EditSingedOutfit.Singleton);
                gameObject.AddInteraction(ResetSingedOutfit.Singleton);
            }
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
            simBuilder.RemoveParts(BodyTypes.Blush);
            simBuilder.AddPart(new CASPart(new ResourceKey(ResourceUtils.HashString64((simDescription.Child ? "cu" : "af") + "Scalp_burnt"), 55242443, 0)));
            return new SimOutfit(simBuilder.CacheOutfit("Singed" + simDescription.SimDescriptionId));
        }

        static void OnObjectPlacedInLot(object sender, EventArgs e)
        {
            World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs = e as World.OnObjectPlacedInLotEventArgs;
            if (onObjectPlacedInLotEventArgs != null)
            {
                AddInteractions(GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId) as Dresser);
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

        static void OnPreLoad()
        {
            BabyDragon.ShootFireball.Singleton = new ShootFireball.DefinitionModified();
            Common.CopyTuning(typeof(BabyDragon), typeof(BabyDragon.ShootFireball.Definition), typeof(ShootFireball.DefinitionModified));
        }

        static void OnWorldLoadFinished(object sender, EventArgs e)
        {
            UpdateListeners();
            new List<Dresser>(Sims3.Gameplay.Queries.GetObjects<Dresser>()).ForEach(AddInteractions);
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

        //[ReplaceMethod(typeof(BuffSinged), "SetupSingedOutfit")]
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
            if (sSimSelectedListener != null)
            {
                EventTracker.RemoveListener(sSimSelectedListener);
                sSimSelectedListener = null;
            }
            sSimSelectedListener = EventTracker.AddListener(EventTypeId.kEventSimSelected, OnSimSelected);
        }
    }
}