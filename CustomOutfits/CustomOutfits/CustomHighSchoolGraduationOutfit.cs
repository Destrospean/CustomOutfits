using Destrospean.CustomOutfits;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Careers;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.ChildAndTeenUpdates;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.Miscellaneous;
using Sims3.Gameplay.Objects.ShelvesStorage;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using System;
using System.Collections.Generic;
using Tuning = Sims3.Gameplay.Destrospean.CustomOutfits;

namespace Destrospean
{
    public class CustomHighSchoolGraduationOutfit
    {
        public static readonly string kHighSchoolGraduationSpecialOutfitKey = "HighSchoolGraduation";

        [Tunable]
        protected static bool kInstantiator;

        [PersistableStatic]
        static List<ulong> sHighSchoolGraduationOutfitDisabledList;

        [PersistableStatic]
        static EventListener sSimDestroyedListener;

        [PersistableStatic]
        static EventListener sSimSelectedListener;

        static CustomHighSchoolGraduationOutfit()
        {
            kInstantiator = false;
            sHighSchoolGraduationOutfitDisabledList = new List<ulong>();
            sSimDestroyedListener = null;
            sSimSelectedListener = null;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnObjectPlacedInLotEventHandler += OnObjectPlacedInLot;
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            World.sOnWorldQuitEventHandler += OnWorldQuit;
        }

        public class EditHighSchoolGraduationOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomHighSchoolGraduationOutfit/EditHighSchoolGraduationOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditHighSchoolGraduationOutfit>
            {
                public Definition()
                {
                }

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return Common.Localize(actor.IsFemale, sLocalizationKey + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, sLocalizationKey + "Path0"),
                        Common.Localize(isFemale, sLocalizationKey + "Path1")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return ((target is Sim && actor == target && Tuning.kShowSimMenu) || (!(target is Sim) && Tuning.kShowObjectMenu)) && actor.SimDescription.YoungAdultOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public override bool Run()
            {
                return Common.EditSpecialOutfit(Actor, sLocalizationKey, kHighSchoolGraduationSpecialOutfitKey, GetHighSchoolGraduationOutfitName(Actor), ProductVersion.EP4);
            }
        }

        public class GraduateInCityHall : School.GraduateInCityHall
        {
            public class DefinitionModified : InteractionDefinition<Sim, RabbitHole, GraduateInCityHall>
            {
                public override bool Test(Sim actor, RabbitHole target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (!School.IsSimGraduating(actor.SimDescription))
                    {
                        return false;
                    }
                    foreach (InteractionInstance interaction in actor.InteractionQueue.InteractionList)
                    {
                        if (interaction.InteractionDefinition == this)
                        {
                            return true;
                        }
                    }
                    return !actor.InteractionQueue.HasInteractionOfType(this);
                }
            }

            public override bool RouteNearEntranceAndIntoBuilding(bool canUseCar, Route.RouteMetaType routeMetaType)
            {
                School.sGraduatingSims[Actor.SimDescription].State = School.GraduationState.EnRoute;
                if (Actor.CurrentOutfit.Key == Actor.GraduationOutfitKey)
                {
                    while (mLeader != null)
                    {
                        if (Actor.WaitForExitReason(Sim.kWaitForExitReasonDefaultTime, ExitReason.Default))
                        {
                            return false;
                        }
                    }
                    if (mWasFollower)
                    {
                        mPriority.Value = mOldPriority;
                        return Target.RouteNearEntranceAndEnterRabbitHole(Actor, this, BeforeEnteringRabbitHole, canUseCar, routeMetaType, true);
                    }
                }
                else if (Autonomous)
                {
                    Actor.ShowTNSIfSelectable(TNSNames.PushedToAttendGraduationTNS, Actor, null, Actor);
                }
                Actor.BuffManager.RemoveElement(BuffNames.OnFire);
                Actor.BuffManager.RemoveElement(BuffNames.Singed);
                Actor.BuffManager.RemoveElement(BuffNames.SingedElectricity);
                List<Sim> graduates = new List<Sim>();
                mOtherPossibleFollowingGraduates = new List<Sim>();
                if (mLeader == null && !Actor.Household.IsSpecialHousehold)
                {
                    foreach (Sim sim in Actor.Household.Sims)
                    {
                        if (sim == Actor)
                        {
                            continue;
                        }
                        if (sim.LotCurrent == Actor.LotCurrent)
                        {
                            graduates.Add(sim);
                            if (School.IsSimGraduating(sim.SimDescription))
                            {
                                mOtherPossibleFollowingGraduates.Add(sim);
                                if (!(sim.InteractionQueue.HasInteractionOfType(Singleton) || sim.InteractionQueue.HasInteractionOfType(GraduateInPlace.Singleton)))
                                {
                                    GraduateInCityHall graduateInCityHall = Singleton.CreateInstance(Target, sim, GetPriority(), false, true) as GraduateInCityHall;
                                    graduateInCityHall.MustRun = true;
                                    sim.InteractionQueue.AddNext(graduateInCityHall);
                                }
                            }
                            else if (sim.SimDescription.ToddlerOrAbove)
                            {
                                sim.PushSwitchToOutfitInteraction(Sim.ClothesChangeReason.Force, OutfitCategories.Formalwear, GetPriority());
                            }
                        }
                        else if (School.IsSimGraduating(sim.SimDescription))
                        {
                            if (!sim.InteractionQueue.HasInteractionOfType(Singleton) && !sim.InteractionQueue.HasInteractionOfType(GraduateInPlace.Singleton))
                            {
                                InteractionInstance interactionInstance = Singleton.CreateInstance(Target, sim, GetPriority(), false, true);
                                interactionInstance.MustRun = true;
                                sim.InteractionQueue.Add(interactionInstance);
                            }
                        }
                        else
                        {
                            InteractionInstance entry = School.AttendGraduation.Singleton.CreateInstance(Target, sim, GetPriority(), false, true);
                            sim.InteractionQueue.Add(entry);
                        }
                    }
                }
                if (Actor.CurrentOutfit.Key != Actor.GraduationOutfitKey && GetHighSchoolGraduationOutfitEnabled(Actor.SimDescription))
                {
                    ResourceKey highSchoolGraduationUniformKey = GetHighSchoolGraduationUniformKey(Actor.SimDescription);
                    if (highSchoolGraduationUniformKey != ResourceKey.kInvalidResourceKey)
                    {
                        Actor.GraduationOutfitKey = highSchoolGraduationUniformKey;
                        Sim.SwitchOutfitHelper switchOutfitHelper = new Sim.SwitchOutfitHelper(Actor, highSchoolGraduationUniformKey);
                        switchOutfitHelper.OverrideProductVersion = ProductVersion.EP4;
                        switchOutfitHelper.OverrideAnimation = "a_graduation_changeClothes_x";
                        Actor.SwitchToOutfitWithSpin(switchOutfitHelper);
                    }
                }
                AddCarryingPosturePrecondition(this);
                if (!Actor.HasExitReason() && mLeader == null)
                {
                    foreach (Sim mOtherPossibleFollowingGraduate in mOtherPossibleFollowingGraduates)
                    {
                        foreach (InteractionInstance interaction in mOtherPossibleFollowingGraduate.InteractionQueue.InteractionList)
                        {
                            if (interaction is GraduateInCityHall)
                            {
                                GraduateInCityHall graduateInCityHall = (GraduateInCityHall)interaction;
                                graduateInCityHall.mReaddOnClean = true;
                                graduateInCityHall.mLeader = Actor;
                                graduateInCityHall.mWasFollower = true;
                                break;
                            }
                        }
                    }
                    Target.RouteOutside(Actor, graduates);
                    Lot.ValidateFollowers(graduates);
                    foreach (Sim otherPossibleFollowingGraduate in mOtherPossibleFollowingGraduates)
                    {
                        foreach (InteractionInstance interaction in otherPossibleFollowingGraduate.InteractionQueue.InteractionList)
                        {
                            if (interaction is GraduateInCityHall)
                            {
                                GraduateInCityHall graduateInCityHall = (GraduateInCityHall)interaction;
                                graduateInCityHall.mReaddOnClean = false;
                                graduateInCityHall.mLeader = null;
                                break;
                            }
                        }
                    }
                    if (!Actor.HasExitReason())
                    {
                        foreach (Sim sim in graduates)
                        {
                            if (!School.IsSimGraduating(sim.SimDescription))
                            {
                                sim.InteractionQueue.Add(School.AttendGraduation.Singleton.CreateInstance(Target, sim, GetPriority(), false, true));
                            }
                        }
                        return Target.RouteNearEntranceAndEnterRabbitHole(Actor, this, BeforeEnteringRabbitHole, canUseCar, routeMetaType, true);
                    }
                }
                return false;
            }
        }

        public class GraduateInPlace : School.GraduateInPlace
        {
            [DoesntRequireTuning]
            public class DefinitionModified : InteractionDefinition<Sim, IGameObject, GraduateInPlace>
            {
                public override string GetInteractionName(Sim actor, IGameObject target, InteractionObjectPair interaction)
                {
                    return GraduateInPlace.LocalizeString("InteractionName");
                }

                public override bool Test(Sim actor, IGameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }
            }

            public override bool Run()
            {
                if (Actor.CurrentOutfit.Key != Actor.GraduationOutfitKey && GetHighSchoolGraduationOutfitEnabled(Actor.SimDescription))
                {
                    ResourceKey highSchoolGraduationUniformKey = GetHighSchoolGraduationUniformKey(Actor.SimDescription);
                    if (highSchoolGraduationUniformKey != ResourceKey.kInvalidResourceKey)
                    {
                        Sim.SwitchOutfitHelper switchOutfitHelper = new Sim.SwitchOutfitHelper(Actor, highSchoolGraduationUniformKey);
                        Actor.GraduationOutfitKey = highSchoolGraduationUniformKey;
                        switchOutfitHelper.OverrideProductVersion = ProductVersion.EP4;
                        switchOutfitHelper.OverrideAnimation = "a_graduation_changeClothes_x";
                        Actor.SwitchToOutfitWithSpin(switchOutfitHelper);
                    }
                }
                School.ApplyGraduation(Actor.SimDescription, AlmaMater.Community);
                if (GameUtils.IsInstalled(ProductVersion.EP4))
                {
                    InteractionInstance instance = School.TossDiploma.Singleton.CreateInstance(Actor, Actor, GetPriority(), Autonomous, CancellableByPlayer);
                    Actor.InteractionQueue.PushAsContinuation(instance, true);
                }
                return true;
            }
        }

        public class PutOnGraduationRobesDresser : Dresser.PutOnGraduationRobes
        {
            public class DefinitionModified : InteractionDefinition<Sim, Dresser, PutOnGraduationRobesDresser>
            {
                public override bool Test(Sim actor, Dresser target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (actor.SimDescription.AlmaMater == AlmaMater.None || actor.CurrentOutfit.Key == actor.GraduationOutfitKey)
                    {
                        return false;
                    }
                    if (actor.SimDescription.IsUsingMaternityOutfits)
                    {
                        greyedOutTooltipCallback = Dresser.ChangeClothes.PregnantCallback;
                        return false;
                    }
                    if (target.IsActorTrailerVariant)
                    {
                        return false;
                    }
                    return Dresser.ChangeClothes.Test(actor, target, ref greyedOutTooltipCallback);
                }
            }

            public override bool Run()
            {
                ResourceKey highSchoolGraduationUniformKey = GetHighSchoolGraduationUniformKey(Actor.SimDescription);
                mSwitchOutfitHelper = new Sim.SwitchOutfitHelper(Actor, highSchoolGraduationUniformKey);
                mSwitchOutfitHelper.Start();
                if (!Target.RouteAndOpenDrawer(this, Actor))
                {
                    return false;
                }
                BeginCommodityUpdates();
                mSwitchOutfitHelper.Wait(true);
                mSwitchOutfitHelper.AddScriptEventHandler(this);
                AnimateSim("ClothesSpin");
                Actor.GraduationOutfitKey = highSchoolGraduationUniformKey;
                Actor.MarkUserDirectedClothingChange();
                bool drawerClosedAndExited = Target.CloseDrawerAndExit(this, Actor);
                EndCommodityUpdates(drawerClosedAndExited);
                return drawerClosedAndExited;
            }
        }

        public class PutOnGraduationRobesFairyHouse : FairyHouse.PutOnGraduationRobes
        {
            public class DefinitionModified : InteractionDefinition<Sim, FairyHouse, PutOnGraduationRobesDresser>, IOverridesVisualType, IHasTraitIcon
            {
                public InteractionVisualTypes GetVisualType
                {
                    get
                    {
                        return InteractionVisualTypes.Trait;
                    }
                }

                public override bool Test(Sim actor, FairyHouse target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (!target.IsAllowedSim(actor))
                    {
                        return false;
                    }
                    if (actor.SimDescription.AlmaMater == AlmaMater.None || actor.CurrentOutfit.Key == actor.GraduationOutfitKey)
                    {
                        return false;
                    }
                    if (actor.SimDescription.IsUsingMaternityOutfits)
                    {
                        greyedOutTooltipCallback = FairyHouse.ChangeClothes.PregnantCallback;
                        return false;
                    }
                    return FairyHouse.ChangeClothes.Test(actor, target, ref greyedOutTooltipCallback);
                }

                public ResourceKey GetTraitIcon(Sim actor, GameObject target)
                {
                    return ResourceKey.CreatePNGKey("trait_Fairy_s", ResourceUtils.ProductVersionToGroupId(ProductVersion.EP7));
                }
            }

            public override bool Run()
            {
                if (!(Actor.Posture is FairyHouse.FairyHousePosture))
                {
                    return false;
                }
                ResourceKey highSchoolGraduationUniformKey = GetHighSchoolGraduationUniformKey(Actor.SimDescription);
                mSwitchOutfitHelper = new Sim.SwitchOutfitHelper(Actor, highSchoolGraduationUniformKey);
                mSwitchOutfitHelper.Start();
                StandardEntry(false);
                BeginCommodityUpdates();
                mSwitchOutfitHelper.ChangeOutfit();
                Actor.GraduationOutfitKey = highSchoolGraduationUniformKey;
                Actor.MarkUserDirectedClothingChange();
                StandardExit(false, false);
                EndCommodityUpdates(true);
                return true;
            }
        }

        public class ResetHighSchoolGraduationOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomHighSchoolGraduationOutfit/ResetHighSchoolGraduationOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ResetHighSchoolGraduationOutfit>
            {
                public Definition()
                {
                }

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return Common.Localize(actor.IsFemale, sLocalizationKey + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, sLocalizationKey + "Path0"),
                        Common.Localize(isFemale, sLocalizationKey + "Path1")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return ((target is Sim && actor == target && Tuning.kShowSimMenu) || (!(target is Sim) && Tuning.kShowObjectMenu)) && actor.SimDescription.YoungAdultOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous && actor.SimDescription.HasSpecialOutfit(kHighSchoolGraduationSpecialOutfitKey);
                }
            }

            public override bool Run()
            {
                Actor.SimDescription.RemoveSpecialOutfit(kHighSchoolGraduationSpecialOutfitKey);
                Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                return true;
            }
        }

        public class SwitchToGraduationOutfit : School.SwitchToGraduationOutfit
        {
            [DoesntRequireTuning]
            public class DefinitionModified : InteractionDefinition<Sim, IGameObject, SwitchToGraduationOutfit>
            {
                public override string GetInteractionName(Sim actor, IGameObject target, InteractionObjectPair interaction)
                {
                    return "Switch To Outfit";
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        "Outfits..."
                    };
                }

                public override bool Test(Sim actor, IGameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }
            }

            public override bool Run()
            {
                ResourceKey highSchoolGraduationUniformKey = GetHighSchoolGraduationUniformKey(Actor.SimDescription);
                if (highSchoolGraduationUniformKey != ResourceKey.kInvalidResourceKey)
                {
                    Sim.SwitchOutfitHelper switchOutfitHelper = new Sim.SwitchOutfitHelper(Actor, highSchoolGraduationUniformKey);
                    Actor.GraduationOutfitKey = highSchoolGraduationUniformKey;
                    switchOutfitHelper.OverrideProductVersion = ProductVersion.EP4;
                    switchOutfitHelper.OverrideAnimation = "a_graduation_changeClothes_x";
                    Actor.SwitchToOutfitWithSpin(switchOutfitHelper);
                }
                return true;
            }
        }

        public class ToggleHighSchoolGraduationOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomHighSchoolGraduationOutfit/ToggleHighSchoolGraduationOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ToggleHighSchoolGraduationOutfit>
            {
                public Definition()
                {
                }

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    if (GetHighSchoolGraduationOutfitEnabled(actor.SimDescription))
                    {
                        return Common.Localize(actor.IsFemale, sLocalizationKey + "DisableInteractionName");
                    }
                    return Common.Localize(actor.IsFemale, sLocalizationKey + "EnableInteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, sLocalizationKey + "Path0"),
                        Common.Localize(isFemale, sLocalizationKey + "Path1")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return ((target is Sim && actor == target && Tuning.kShowSimMenu) || (!(target is Sim) && Tuning.kShowObjectMenu)) && actor.SimDescription.YoungAdultOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public override bool Run()
            {
                if (GetHighSchoolGraduationOutfitEnabled(Actor.SimDescription))
                {
                    DisableHighSchoolGraduationOutfit(Actor.SimDescription);
                    Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "DisabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                else
                {
                    EnableHighSchoolGraduationOutfit(Actor.SimDescription);
                    Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "EnabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                return true;
            }
        }

        static void AddInteractions(GameObject gameObject)
        {
            if (gameObject != null && !gameObject.Interactions.Exists(interaction => interaction.InteractionDefinition.GetType() == EditHighSchoolGraduationOutfit.Singleton.GetType()) && GameUtils.IsInstalled(ProductVersion.EP4))
            {
                gameObject.AddInteraction(EditHighSchoolGraduationOutfit.Singleton);
                gameObject.AddInteraction(ResetHighSchoolGraduationOutfit.Singleton);
                gameObject.AddInteraction(ToggleHighSchoolGraduationOutfit.Singleton);
            }
        }

        static void DisableHighSchoolGraduationOutfit(SimDescription simDescription)
        {
            if (GetHighSchoolGraduationOutfitEnabled(simDescription))
            {
                sHighSchoolGraduationOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
            UpdateListeners();
        }

        static void EnableHighSchoolGraduationOutfit(SimDescription simDescription)
        {
            if (!GetHighSchoolGraduationOutfitEnabled(simDescription))
            {
                sHighSchoolGraduationOutfitDisabledList.Remove(simDescription.SimDescriptionId);
            }
            UpdateListeners();
        }

        static bool GetHighSchoolGraduationOutfitEnabled(SimDescription simDescription)
        {
            return !sHighSchoolGraduationOutfitDisabledList.Contains(simDescription.SimDescriptionId);
        }

        public static string GetHighSchoolGraduationOutfitName(Sim actor)
        {
            SimDescription simDescription = actor.SimDescription;
            if (BoardingSchool.DidSimGraduate(simDescription, BoardingSchool.BoardingSchoolTypes.None, false))
            {
                BoardingSchool.BoardingSchoolData boardingSchoolData;
                BoardingSchool.BoardingSchoolData.sBoardingSchoolDataList.TryGetValue(simDescription.BoardingSchool.GraduatedHigh, out boardingSchoolData);
                return "a" + (simDescription.IsMale ? "m" : "f") + boardingSchoolData.GraduationUniform;
            }
            return (simDescription.Elder ? "e" : "a") + (simDescription.IsMale ? "m" : "f") + "BodyEP4Graduation" + (simDescription.TraitManager.HasAnyElement(TraitNames.PartyAnimal, TraitNames.GoodSenseOfHumor, TraitNames.Childish) ? "Party" : "");
        }

        public static ResourceKey GetHighSchoolGraduationUniformKey(SimDescription simDescription)
        {
            Sim sim = simDescription.CreatedSim;
            if (sim != null)
            {
                return Common.CreateAndAddSpecialOutfit(sim, kHighSchoolGraduationSpecialOutfitKey, ResourceKey.CreateOutfitKeyFromProductVersion(GetHighSchoolGraduationOutfitName(sim), ProductVersion.EP4));
            }
            return ResourceKey.kInvalidResourceKey;
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
                if (gameObject is Dresser)
                {
                    AddInteractions(gameObject);
                }
            }
        }

        static void OnPreLoad()
        {
            Dresser.PutOnGraduationRobes.Singleton = new PutOnGraduationRobesDresser.DefinitionModified();
            FairyHouse.PutOnGraduationRobes.Singleton = new PutOnGraduationRobesFairyHouse.DefinitionModified();
            School.GraduateInCityHall.Singleton = new GraduateInCityHall.DefinitionModified();
            School.GraduateInPlace.Singleton = new GraduateInPlace.DefinitionModified();
            School.SwitchToGraduationOutfit.Singleton = new SwitchToGraduationOutfit.DefinitionModified();
            Common.CopyTuning(typeof(Dresser), typeof(Dresser.PutOnGraduationRobes.Definition), typeof(PutOnGraduationRobesDresser.DefinitionModified));
            Common.CopyTuning(typeof(FairyHouse), typeof(FairyHouse.PutOnGraduationRobes.Definition), typeof(PutOnGraduationRobesFairyHouse.DefinitionModified));
            Common.CopyTuning(typeof(School), typeof(School.GraduateInCityHall.Definition), typeof(GraduateInCityHall.DefinitionModified));
        }

        static ListenerAction OnSimDestroyed(Event e)
        {
            try
            {
                if (e.TargetObject is Sim)
                {
                    EnableHighSchoolGraduationOutfit(((Sim)e.TargetObject).SimDescription);
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
            new List<Dresser>(Sims3.Gameplay.Queries.GetObjects<Dresser>()).ForEach(AddInteractions);
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