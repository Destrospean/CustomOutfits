using System;
using System.Collections.Generic;
using Destrospean.CustomOutfits;
using MonoPatcherLib;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.ShelvesStorage;
using Sims3.Gameplay.Situations;
using Sims3.Gameplay.Socializing;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using Tuning = Sims3.Gameplay.Destrospean.CustomOutfits;

namespace Destrospean
{
    public class CustomBachelorPartyOutfit
    {
        [Tunable]
        protected static bool kInstantiator;

        [PersistableStatic(true)]
        static List<ulong> sBachelorPartyGuestOutfitDisabledList;

        [PersistableStatic(true)]
        static List<ulong> sBachelorPartyHostOutfitDisabledList;

        [PersistableStatic(true)]
        static List<ulong> sBachelorPartyUnderwearDisabledList;

        static EventListener sSimDestroyedListener, sSimSelectedListener;

        public enum BachelorPartyOutfitTypes
        {
            Guest,
            Host,
            Underwear
        }

        static CustomBachelorPartyOutfit()
        {
            kInstantiator = false;
            sBachelorPartyGuestOutfitDisabledList = new List<ulong>();
            sBachelorPartyHostOutfitDisabledList = new List<ulong>();
            sBachelorPartyUnderwearDisabledList = new List<ulong>();
            sSimDestroyedListener = null;
            sSimSelectedListener = null;
            World.sOnObjectPlacedInLotEventHandler += OnObjectPlacedInLot;
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            World.sOnWorldQuitEventHandler += OnWorldQuit;
        }

        [TypePatch(typeof(BachelorParty))]
        public class BachelorPartyPatch
        {
            public static void OnSprayedWithFizzyNectar(Sim actor)
            {
                float chance = MathHelpers.LinearInterpolate(MoodManager.kMoodMinValue, MoodManager.kMoodSuperBarEnd, BachelorParty.kChanceOfSpinningIntoUnderwearMin, BachelorParty.kChanceOfSpinningIntoUnderwearMax, actor.MoodManager.MoodValue);
                if (RandomUtil.RandomChance01(chance) && !actor.SimDescription.HasSpecialOutfit(GetBachelorPartyOutfitName(actor, BachelorPartyOutfitTypes.Underwear)) && BachelorParty.CanSwitchIntoOutfit(actor.SimDescription) && GetBachelorPartyOutfitEnabled(actor.SimDescription, BachelorPartyOutfitTypes.Underwear))
                {
                    PushSwitchToBachelorPartyOutfitInteraction(actor, BachelorPartyOutfitTypes.Underwear);
                }
            }

            public void PushSwitchOutfit(Sim actor)
            {
                if (actor.SimDescription.TeenOrBelow)
                {
                    return;
                }
                BachelorParty self = (BachelorParty)(this as object);
                if (actor == self.Host)
                {
                    if (BachelorParty.CanSwitchIntoOutfit(actor.SimDescription) && GetBachelorPartyOutfitEnabled(actor.SimDescription, BachelorPartyOutfitTypes.Host))
                    {
                        PushSwitchToBachelorPartyOutfitInteraction(actor, BachelorPartyOutfitTypes.Host);
                    }
                }
                else if (self.ShouldSwitchIntoOutfit(actor) && GetBachelorPartyOutfitEnabled(actor.SimDescription, BachelorPartyOutfitTypes.Guest))
                {
                    if (BachelorParty.kSpecialOutfitCount > 0)
                    {
                        PushSwitchToBachelorPartyOutfitInteraction(actor, BachelorPartyOutfitTypes.Guest);
                    }
                    else
                    {
                        actor.PushSwitchToOutfitInteraction(Sim.ClothesChangeReason.GoingToSituation, self.GetClothingStyle());
                    }
                }
                else
                {
                    actor.PushSwitchToOutfitInteraction(Sim.ClothesChangeReason.GoingToSituation, self.GetClothingStyle());
                }
            }

            public void RemoveBachelorPartyEffects(Sim actor)
            {
                ActiveTopic.RemoveTopicFromSim(actor, "Bachelor Party");
                ((BachelorParty)(this as object)).RemoveIncreasedEffectivenesses(actor);
                foreach (BachelorPartyOutfitTypes outfitType in Enum.GetValues(typeof(BachelorPartyOutfitTypes)))
                {
                    if (actor.IsWearingSpecialOutfit(GetBachelorPartyOutfitName(actor, outfitType)))
                    {
                        PushSwitchFromBachelorPartyOutfitInteraction(actor, outfitType, OutfitCategories.Everyday);
                        break;
                    }
                }
            }
        }

        public class EditBachelorPartyOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public BachelorPartyOutfitTypes mOutfitType;

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditBachelorPartyOutfit>
            {
                public BachelorPartyOutfitTypes mOutfitType;

                public Definition()
                {
                }

                public Definition(BachelorPartyOutfitTypes outfitType)
                {
                    mOutfitType = outfitType;
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, GameObject target, List<InteractionObjectPair> results)
                {
                    foreach (BachelorPartyOutfitTypes outfitType in Enum.GetValues(typeof(BachelorPartyOutfitTypes)))
                    {
                        results.Add(new InteractionObjectPair(new Definition(outfitType), target));
                    }
                }

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    EditBachelorPartyOutfit editBachelorPartyOutfit = new EditBachelorPartyOutfit();
                    editBachelorPartyOutfit.SetOutfitType(mOutfitType);
                    editBachelorPartyOutfit.Init(ref parameters);
                    return editBachelorPartyOutfit;
                }

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return Common.Localize(actor.IsFemale, GetLocalizationKey(mOutfitType) + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, GetLocalizationKey(mOutfitType) + "Path0"),
                        Common.Localize(isFemale, GetLocalizationKey(mOutfitType) + "Path1")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return (actor == target && Tuning.kShowSimMenu || target as Sim == null && Tuning.kShowObjectMenu) && actor.SimDescription.YoungAdultOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public static string GetLocalizationKey(BachelorPartyOutfitTypes outfitType)
            {
                return "CustomBachelorPartyOutfit/" + new Dictionary<BachelorPartyOutfitTypes, string>
                {
                    {
                        BachelorPartyOutfitTypes.Guest,
                        "EditBachelorPartyGuestOutfit/"
                    },
                    {
                        BachelorPartyOutfitTypes.Host,
                        "EditBachelorPartyHostOutfit/"
                    },
                    {
                        BachelorPartyOutfitTypes.Underwear,
                        "EditBachelorPartyUnderwear/"
                    }
                }[outfitType];
            }

            public override bool Run()
            {
                string outfitName = GetBachelorPartyOutfitName(Actor, mOutfitType);
                if (!Actor.SimDescription.HasSpecialOutfit(outfitName))
                {
                    Actor.SimDescription.AddSpecialOutfit(CreateBachelorPartyOutfit(Actor, mOutfitType), outfitName);
                }
                return Common.EditSpecialOutfit(Actor, GetLocalizationKey(mOutfitType), outfitName);
            }

            public void SetOutfitType(BachelorPartyOutfitTypes outfitType)
            {
                mOutfitType = outfitType;
            }
        }

        public class ResetBachelorPartyOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public BachelorPartyOutfitTypes mOutfitType;

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ResetBachelorPartyOutfit>
            {
                public BachelorPartyOutfitTypes mOutfitType;

                public Definition()
                {
                }

                public Definition(BachelorPartyOutfitTypes outfitType)
                {
                    mOutfitType = outfitType;
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, GameObject target, List<InteractionObjectPair> results)
                {
                    foreach (BachelorPartyOutfitTypes outfitType in Enum.GetValues(typeof(BachelorPartyOutfitTypes)))
                    {
                        results.Add(new InteractionObjectPair(new Definition(outfitType), target));
                    }
                }

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    ResetBachelorPartyOutfit resetBachelorPartyOutfit = new ResetBachelorPartyOutfit();
                    resetBachelorPartyOutfit.SetOutfitType(mOutfitType);
                    resetBachelorPartyOutfit.Init(ref parameters);
                    return resetBachelorPartyOutfit;
                }

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return Common.Localize(actor.IsFemale, GetLocalizationKey(mOutfitType) + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, GetLocalizationKey(mOutfitType) + "Path0"),
                        Common.Localize(isFemale, GetLocalizationKey(mOutfitType) + "Path1")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return (actor == target && Tuning.kShowSimMenu || target as Sim == null && Tuning.kShowObjectMenu) && actor.SimDescription.YoungAdultOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous && actor.SimDescription.HasSpecialOutfit(GetBachelorPartyOutfitName(actor, mOutfitType));
                }
            }

            public static string GetLocalizationKey(BachelorPartyOutfitTypes outfitType)
            {
                return "CustomBachelorPartyOutfit/" + new Dictionary<BachelorPartyOutfitTypes, string>
                {
                    {
                        BachelorPartyOutfitTypes.Guest,
                        "ResetBachelorPartyGuestOutfit/"
                    },
                    {
                        BachelorPartyOutfitTypes.Host,
                        "ResetBachelorPartyHostOutfit/"
                    },
                    {
                        BachelorPartyOutfitTypes.Underwear,
                        "ResetBachelorPartyUnderwear/"
                    }
                }[outfitType];
            }

            public override bool Run()
            {
                Actor.SimDescription.RemoveSpecialOutfit(GetBachelorPartyOutfitName(Actor, mOutfitType));
                Common.Notify(Common.Localize(Actor.IsFemale, GetLocalizationKey(mOutfitType) + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                return true;
            }

            public void SetOutfitType(BachelorPartyOutfitTypes outfitType)
            {
                mOutfitType = outfitType;
            }
        }

        public class SwitchToBachelorPartyOutfit : Interaction<Sim, IGameObject>
        {
            [DoesntRequireTuning]
            public class Definition : InteractionDefinition<Sim, IGameObject, SwitchToBachelorPartyOutfit>
            {
                public override string GetInteractionName(Sim actor, IGameObject target, InteractionObjectPair interaction)
                {
                    return "";
                }

                public override bool Test(Sim actor, IGameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }
            }

            public static InteractionDefinition Singleton = new Definition();

            public bool IsRemove;

            public OutfitCategories OutfitCategory;

            public BachelorPartyOutfitTypes OutfitType;

            public override bool Run()
            {
                SimDescription simDescription = Actor.SimDescription;
                string specialOutfitKey = GetBachelorPartyOutfitName(Actor, OutfitType);
                if (IsRemove)
                {
                    if (simDescription.HasSpecialOutfit(specialOutfitKey))
                    {
                        Actor.SwitchToOutfitWithSpin(Sim.ClothesChangeReason.Force, OutfitCategory);
                    }
                    return true;
                }
                if (simDescription.HasSpecialOutfit(specialOutfitKey))
                {
                    Actor.SwitchToOutfitWithSpin(simDescription.GetSpecialOutfit(specialOutfitKey).Key);
                    return true;
                }
                SimOutfit simOutfit = CreateBachelorPartyOutfit(Actor, OutfitType);
                if (simOutfit == null)
                {
                    return false;
                }
                simDescription.AddSpecialOutfit(simOutfit, specialOutfitKey);
                Actor.SwitchToOutfitWithSpin(simOutfit.Key);
                return true;
            }
        }

        public class ToggleBachelorPartyOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public BachelorPartyOutfitTypes mOutfitType;

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ToggleBachelorPartyOutfit>
            {
                public BachelorPartyOutfitTypes mOutfitType;

                public Definition()
                {
                }

                public Definition(BachelorPartyOutfitTypes outfitType)
                {
                    mOutfitType = outfitType;
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, GameObject target, List<InteractionObjectPair> results)
                {
                    foreach (BachelorPartyOutfitTypes outfitType in Enum.GetValues(typeof(BachelorPartyOutfitTypes)))
                    {
                        results.Add(new InteractionObjectPair(new Definition(outfitType), target));
                    }
                }

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    ToggleBachelorPartyOutfit toggleBachelorPartyOutfit = new ToggleBachelorPartyOutfit();
                    toggleBachelorPartyOutfit.SetOutfitType(mOutfitType);
                    toggleBachelorPartyOutfit.Init(ref parameters);
                    return toggleBachelorPartyOutfit;
                }

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    if (GetBachelorPartyOutfitEnabled(actor.SimDescription, mOutfitType))
                    {
                        return Common.Localize(actor.IsFemale, GetLocalizationKey(mOutfitType) + "DisableInteractionName");
                    }
                    return Common.Localize(actor.IsFemale, GetLocalizationKey(mOutfitType) + "EnableInteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, GetLocalizationKey(mOutfitType) + "Path0"),
                        Common.Localize(isFemale, GetLocalizationKey(mOutfitType) + "Path1")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return (actor == target && Tuning.kShowSimMenu || target as Sim == null && Tuning.kShowObjectMenu) && actor.SimDescription.YoungAdultOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public static string GetLocalizationKey(BachelorPartyOutfitTypes outfitType)
            {
                return "CustomBachelorPartyOutfit/" + new Dictionary<BachelorPartyOutfitTypes, string>
                {
                    {
                        BachelorPartyOutfitTypes.Guest,
                        "ToggleBachelorPartyGuestOutfit/"
                    },
                    {
                        BachelorPartyOutfitTypes.Host,
                        "ToggleBachelorPartyHostOutfit/"
                    },
                    {
                        BachelorPartyOutfitTypes.Underwear,
                        "ToggleBachelorPartyUnderwear/"
                    }
                }[outfitType];
            }

            public override bool Run()
            {
                if (GetBachelorPartyOutfitEnabled(Actor.SimDescription, mOutfitType))
                {
                    DisableBachelorPartyOutfit(Actor.SimDescription, mOutfitType);
                    Common.Notify(Common.Localize(Actor.IsFemale, GetLocalizationKey(mOutfitType) + "DisabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                else
                {
                    EnableBachelorPartyOutfit(Actor.SimDescription, mOutfitType);
                    Common.Notify(Common.Localize(Actor.IsFemale, GetLocalizationKey(mOutfitType) + "EnabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                return true;
            }

            public void SetOutfitType(BachelorPartyOutfitTypes outfitType)
            {
                mOutfitType = outfitType;
            }
        }

        static void AddInteractions(GameObject gameObject)
        {
            if (gameObject != null && !gameObject.Interactions.Exists(interaction => interaction.InteractionDefinition.GetType() == EditBachelorPartyOutfit.Singleton.GetType()) && GameUtils.IsInstalled(ProductVersion.EP4))
            {
                gameObject.AddInteraction(EditBachelorPartyOutfit.Singleton);
                gameObject.AddInteraction(ResetBachelorPartyOutfit.Singleton);
                gameObject.AddInteraction(ToggleBachelorPartyOutfit.Singleton);
            }
        }

        public static SimOutfit CreateBachelorPartyOutfit(Sim actor, BachelorPartyOutfitTypes outfitType)
        {
            SimBuilder simBuilder = new SimBuilder
            {
                UseCompression = true
            };
            SimDescription simDescription = actor.SimDescription;
            SimOutfit resultOutfit;
            if (!OutfitUtils.TryApplyUniformToOutfit(simDescription.GetOutfit(outfitType == BachelorPartyOutfitTypes.Underwear ? OutfitCategories.Naked : OutfitCategories.Formalwear, 0), new SimOutfit(ResourceKey.CreateOutfitKeyFromProductVersion(GetBachelorPartyOutfitName(actor, outfitType), ProductVersion.EP4)), simDescription, "CreateBachelorPartyOutfit", out resultOutfit))
            {
                return null;
            }
            OutfitUtils.SetOutfit(simBuilder, resultOutfit, simDescription);
            foreach (CASPart part in resultOutfit.Parts)
            {
                if (part.BodyType == BodyTypes.Freckles)
                {
                    simBuilder.RemovePart(part);
                }
            }
            foreach (CASPart part in simDescription.GetOutfit(OutfitCategories.Everyday, 0).Parts)
            {
                if (part.BodyType == BodyTypes.Freckles)
                {
                    simBuilder.AddPart(part);
                }
            }
            return new SimOutfit(simBuilder.CacheOutfit("BachelorParty" + simDescription.SimDescriptionId));
        }

        static void DisableBachelorPartyGuestOutfit(SimDescription simDescription)
        {
            if (GetBachelorPartyOutfitEnabled(simDescription, BachelorPartyOutfitTypes.Guest))
            {
                sBachelorPartyGuestOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
        }

        static void DisableBachelorPartyHostOutfit(SimDescription simDescription)
        {
            if (GetBachelorPartyOutfitEnabled(simDescription, BachelorPartyOutfitTypes.Host))
            {
                sBachelorPartyHostOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
        }

        static void DisableBachelorPartyOutfit(SimDescription simDescription, BachelorPartyOutfitTypes outfitType)
        {
            switch (outfitType)
            {
                case BachelorPartyOutfitTypes.Guest:
                    DisableBachelorPartyGuestOutfit(simDescription);
                    break;
                case BachelorPartyOutfitTypes.Host:
                    DisableBachelorPartyHostOutfit(simDescription);
                    break;
                case BachelorPartyOutfitTypes.Underwear:
                    DisableBachelorPartyUnderwear(simDescription);
                    break;
            }
        }

        static void DisableBachelorPartyUnderwear(SimDescription simDescription)
        {
            if (GetBachelorPartyOutfitEnabled(simDescription, BachelorPartyOutfitTypes.Underwear))
            {
                sBachelorPartyUnderwearDisabledList.Add(simDescription.SimDescriptionId);
            }
        }

        static void EnableBachelorPartyGuestOutfit(SimDescription simDescription)
        {
            if (!GetBachelorPartyOutfitEnabled(simDescription, BachelorPartyOutfitTypes.Guest))
            {
                sBachelorPartyGuestOutfitDisabledList.Remove(simDescription.SimDescriptionId);
            }
        }

        static void EnableBachelorPartyHostOutfit(SimDescription simDescription)
        {
            if (!GetBachelorPartyOutfitEnabled(simDescription, BachelorPartyOutfitTypes.Host))
            {
                sBachelorPartyHostOutfitDisabledList.Remove(simDescription.SimDescriptionId);
            }
        }

        static void EnableBachelorPartyOutfit(SimDescription simDescription, BachelorPartyOutfitTypes outfitType)
        {
            switch (outfitType)
            {
                case BachelorPartyOutfitTypes.Guest:
                    EnableBachelorPartyGuestOutfit(simDescription);
                    break;
                case BachelorPartyOutfitTypes.Host:
                    EnableBachelorPartyHostOutfit(simDescription);
                    break;
                case BachelorPartyOutfitTypes.Underwear:
                    EnableBachelorPartyUnderwear(simDescription);
                    break;
            }
        }

        static void EnableBachelorPartyUnderwear(SimDescription simDescription)
        {
            if (!GetBachelorPartyOutfitEnabled(simDescription, BachelorPartyOutfitTypes.Underwear))
            {
                sBachelorPartyUnderwearDisabledList.Remove(simDescription.SimDescriptionId);
            }
        }

        static bool GetBachelorPartyOutfitEnabled(SimDescription simDescription, BachelorPartyOutfitTypes outfitType)
        {
            return !new Dictionary<BachelorPartyOutfitTypes, bool>
            {
                {
                    BachelorPartyOutfitTypes.Guest,
                    sBachelorPartyGuestOutfitDisabledList.Contains(simDescription.SimDescriptionId)
                },
                {
                    BachelorPartyOutfitTypes.Host,
                    sBachelorPartyHostOutfitDisabledList.Contains(simDescription.SimDescriptionId)
                },
                {
                    BachelorPartyOutfitTypes.Underwear,
                    sBachelorPartyUnderwearDisabledList.Contains(simDescription.SimDescriptionId)
                }
            }[outfitType];
        }

        public static string GetBachelorPartyOutfitName(Sim actor, BachelorPartyOutfitTypes outfitType)
        {
            return string.Format("{0}{1}Bachelor{2}", actor.SimDescription.Elder ? "e" : "a", actor.IsMale ? "m" : "f", new Dictionary<BachelorPartyOutfitTypes, string>
            {
                {
                    BachelorPartyOutfitTypes.Guest,
                    "Random1"
                },
                {
                    BachelorPartyOutfitTypes.Host,
                    "PartyHost"
                },
                {
                    BachelorPartyOutfitTypes.Underwear,
                    "Underwear1"
                }
            }[outfitType]);
        }

        static void Init()
        {
            UpdateListeners();
        }

        static void OnObjectPlacedInLot(object sender, EventArgs e)
        {
            World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs = e as World.OnObjectPlacedInLotEventArgs;
            if (onObjectPlacedInLotEventArgs != null)
            {
                AddInteractions(GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId) as Dresser);
            }
        }

        static ListenerAction OnSimDestroyed(Event e)
        {
            try
            {
                Sim sim = e.TargetObject as Sim;
                if (sim != null)
                {
                    foreach (BachelorPartyOutfitTypes outfitType in Enum.GetValues(typeof(BachelorPartyOutfitTypes)))
                    {
                        EnableBachelorPartyOutfit(sim.SimDescription, outfitType);
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

        public static void PushSwitchFromBachelorPartyOutfitInteraction(Sim actor, BachelorPartyOutfitTypes outfitType, OutfitCategories destinationCategory)
        {
            SwitchToBachelorPartyOutfit switchToBachelorPartyOutfit = SwitchToBachelorPartyOutfit.Singleton.CreateInstanceWithCallbacks(actor, actor, new InteractionPriority(InteractionPriorityLevel.Zero), false, false, null, null, delegate
            {
                actor.SwitchToOutfitWithoutSpin(destinationCategory);
            }) as SwitchToBachelorPartyOutfit;
            switchToBachelorPartyOutfit.Hidden = true;
            switchToBachelorPartyOutfit.IsRemove = true;
            switchToBachelorPartyOutfit.MustRun = true;
            switchToBachelorPartyOutfit.OutfitCategory = destinationCategory;
            switchToBachelorPartyOutfit.OutfitType = outfitType;
            actor.InteractionQueue.AddNext(switchToBachelorPartyOutfit);
        }

        public static void PushSwitchToBachelorPartyOutfitInteraction(Sim actor, BachelorPartyOutfitTypes outfitType)
        {
            SwitchToBachelorPartyOutfit switchToBachelorPartyOutfit = SwitchToBachelorPartyOutfit.Singleton.CreateInstance(actor, actor, new InteractionPriority(InteractionPriorityLevel.Zero), false, false) as SwitchToBachelorPartyOutfit;
            switchToBachelorPartyOutfit.Hidden = true;
            switchToBachelorPartyOutfit.MustRun = true;
            switchToBachelorPartyOutfit.OutfitType = outfitType;
            actor.InteractionQueue.AddNext(switchToBachelorPartyOutfit);
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