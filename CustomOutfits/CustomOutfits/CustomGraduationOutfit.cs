using System;
using System.Collections.Generic;
using Destrospean.CustomOutfits;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Academics;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.RabbitHoles;
using Sims3.Gameplay.Objects.ShelvesStorage;
using Sims3.SimIFace;
using Sims3.UI;
using Sims3.UI.Hud;
using Tuning = Sims3.Gameplay.Destrospean.CustomOutfits;

namespace Destrospean
{
    public class CustomGraduationOutfit
    {
        [Tunable]
        protected static bool kInstantiator;

        [PersistableStatic(true)]
        static List<ulong> sGraduationBusinessOutfitDisabledList;

        [PersistableStatic(true)]
        static List<ulong> sGraduationCommOutfitDisabledList;

        [PersistableStatic(true)]
        static List<ulong> sGraduationFineArtsOutfitDisabledList;

        [PersistableStatic(true)]
        static List<ulong> sGraduationPhysEdOutfitDisabledList;

        [PersistableStatic(true)]
        static List<ulong> sGraduationScienceMedOutfitDisabledList;

        [PersistableStatic(true)]
        static List<ulong> sGraduationTechnologyOutfitDisabledList;

        static EventListener sSimDescriptionDisposedListener, sSimSelectedListener;

        static CustomGraduationOutfit()
        {
            kInstantiator = false;
            sGraduationBusinessOutfitDisabledList = new List<ulong>();
            sGraduationCommOutfitDisabledList = new List<ulong>();
            sGraduationFineArtsOutfitDisabledList = new List<ulong>();
            sGraduationPhysEdOutfitDisabledList = new List<ulong>();
            sGraduationScienceMedOutfitDisabledList = new List<ulong>();
            sGraduationTechnologyOutfitDisabledList = new List<ulong>();
            sSimDescriptionDisposedListener = null;
            sSimSelectedListener = null;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnObjectPlacedInLotEventHandler += OnObjectPlacedInLot;
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            World.sOnWorldQuitEventHandler += OnWorldQuit;
        }

        public class EditGraduationOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public AcademicDegreeNames mAcademicDegreeName;

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditGraduationOutfit>
            {
                public AcademicDegreeNames mAcademicDegreeName;

                public Definition()
                {
                }

                public Definition(AcademicDegreeNames academicDegreeName)
                {
                    mAcademicDegreeName = academicDegreeName;
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, GameObject target, List<InteractionObjectPair> results)
                {
                    foreach (AcademicDegreeNames academicDegreeName in Enum.GetValues(typeof(AcademicDegreeNames)))
                    {
                        switch (academicDegreeName)
                        {
                            case AcademicDegreeNames.Undefined:
                                break;
                            case AcademicDegreeNames.MaxDegreeNames:
                                break;
                            default:
                                results.Add(new InteractionObjectPair(new Definition(academicDegreeName), target));
                                break;
                        }
                    }
                }

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    EditGraduationOutfit editGraduationOutfit = new EditGraduationOutfit();
                    editGraduationOutfit.SetAcademicDegreeName(mAcademicDegreeName);
                    editGraduationOutfit.Init(ref parameters);
                    return editGraduationOutfit;
                }

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return Common.Localize(actor.IsFemale, GetLocalizationKey(mAcademicDegreeName) + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, GetLocalizationKey(mAcademicDegreeName) + "Path0"),
                        Common.Localize(isFemale, GetLocalizationKey(mAcademicDegreeName) + "Path1")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return (actor == target && Tuning.kShowSimMenu || target as Sim == null && Tuning.kShowObjectMenu) && actor.SimDescription.YoungAdultOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public static string GetLocalizationKey(AcademicDegreeNames academicDegreeName)
            {
                return "CustomGraduationOutfit/" + new Dictionary<AcademicDegreeNames, string>
                {
                    {
                        AcademicDegreeNames.Business,
                        "EditGraduationBusinessOutfit/"
                    },
                    {
                        AcademicDegreeNames.Technology,
                        "EditGraduationTechnologyOutfit/"
                    },
                    {
                        AcademicDegreeNames.Science,
                        "EditGraduationScienceMedOutfit/"
                    },
                    {
                        AcademicDegreeNames.FineArts,
                        "EditGraduationFineArtsOutfit/"
                    },
                    {
                        AcademicDegreeNames.Communications,
                        "EditGraduationCommOutfit/"
                    },
                    {
                        AcademicDegreeNames.PhysEd,
                        "EditGraduationPhysEdOutfit/"
                    }
                }[academicDegreeName];
            }

            public override bool Run()
            {
                string outfitName = GetGraduationOutfitName(Actor, mAcademicDegreeName);
                return Common.EditSpecialOutfit(Actor, GetLocalizationKey(mAcademicDegreeName), outfitName, outfitName, ProductVersion.EP9);
            }

            public void SetAcademicDegreeName(AcademicDegreeNames academicDegreeName)
            {
                mAcademicDegreeName = academicDegreeName;
            }
        }

        public class GraduateInPlace : CollegeGraduation.GraduateInPlace
        {
            public class DefinitionModified : SoloSimInteractionDefinition<GraduateInPlace>
            {
            }

            public override bool Run()
            {
                ResourceKey graduationUniformKey = GetGraduationUniformKey(Actor);
                if (graduationUniformKey != ResourceKey.kInvalidResourceKey)
                {
                    Actor.GraduationOutfitKey = graduationUniformKey;
                    Sim.SwitchOutfitHelper spin = new Sim.SwitchOutfitHelper(Actor, graduationUniformKey);
                    Actor.SwitchToOutfitWithSpin(spin);
                }
                CollegeGraduation.ApplyGraduationEffects(Actor, mDegree, AcademicsUtility.kUniversityName);
                InteractionInstance instance = CollegeGraduation.CelebrateGraduation.Singleton.CreateInstance(Actor, Actor, GetPriority(), Autonomous, CancellableByPlayer);
                Actor.InteractionQueue.PushAsContinuation(instance, true);
                return true;
            }
        }

        public class ResetGraduationOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public AcademicDegreeNames mAcademicDegreeName;

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ResetGraduationOutfit>
            {
                public AcademicDegreeNames mAcademicDegreeName;

                public Definition()
                {
                }

                public Definition(AcademicDegreeNames academicDegreeName)
                {
                    mAcademicDegreeName = academicDegreeName;
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, GameObject target, List<InteractionObjectPair> results)
                {
                    foreach (AcademicDegreeNames academicDegreeName in Enum.GetValues(typeof(AcademicDegreeNames)))
                    {
                        switch (academicDegreeName)
                        {
                            case AcademicDegreeNames.Undefined:
                                break;
                            case AcademicDegreeNames.MaxDegreeNames:
                                break;
                            default:
                                results.Add(new InteractionObjectPair(new Definition(academicDegreeName), target));
                                break;
                        }
                    }
                }

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    ResetGraduationOutfit resetGraduationOutfit = new ResetGraduationOutfit();
                    resetGraduationOutfit.SetAcademicDegreeName(mAcademicDegreeName);
                    resetGraduationOutfit.Init(ref parameters);
                    return resetGraduationOutfit;
                }

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return Common.Localize(actor.IsFemale, GetLocalizationKey(mAcademicDegreeName) + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, GetLocalizationKey(mAcademicDegreeName) + "Path0"),
                        Common.Localize(isFemale, GetLocalizationKey(mAcademicDegreeName) + "Path1")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return (actor == target && Tuning.kShowSimMenu || target as Sim == null && Tuning.kShowObjectMenu) && actor.SimDescription.YoungAdultOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous && actor.SimDescription.HasSpecialOutfit(GetGraduationOutfitName(actor, mAcademicDegreeName));
                }
            }

            public static string GetLocalizationKey(AcademicDegreeNames academicDegreeName)
            {
                return "CustomGraduationOutfit/" + new Dictionary<AcademicDegreeNames, string>
                {
                    {
                        AcademicDegreeNames.Business,
                        "ResetGraduationBusinessOutfit/"
                    },
                    {
                        AcademicDegreeNames.Technology,
                        "ResetGraduationTechnologyOutfit/"
                    },
                    {
                        AcademicDegreeNames.Science,
                        "ResetGraduationScienceMedOutfit/"
                    },
                    {
                        AcademicDegreeNames.FineArts,
                        "ResetGraduationFineArtsOutfit/"
                    },
                    {
                        AcademicDegreeNames.Communications,
                        "ResetGraduationCommOutfit/"
                    },
                    {
                        AcademicDegreeNames.PhysEd,
                        "ResetGraduationPhysEdOutfit/"
                    }
                }[academicDegreeName];
            }

            public override bool Run()
            {
                Actor.SimDescription.RemoveSpecialOutfit(GetGraduationOutfitName(Actor, mAcademicDegreeName));
                Common.Notify(Common.Localize(Actor.IsFemale, GetLocalizationKey(mAcademicDegreeName) + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                return true;
            }

            public void SetAcademicDegreeName(AcademicDegreeNames academicDegreeName)
            {
                mAcademicDegreeName = academicDegreeName;
            }
        }

        public class ToggleGraduationOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public AcademicDegreeNames mAcademicDegreeName;

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ToggleGraduationOutfit>
            {
                public AcademicDegreeNames mAcademicDegreeName;

                public Definition()
                {
                }

                public Definition(AcademicDegreeNames academicDegreeName)
                {
                    mAcademicDegreeName = academicDegreeName;
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, GameObject target, List<InteractionObjectPair> results)
                {
                    foreach (AcademicDegreeNames academicDegreeName in Enum.GetValues(typeof(AcademicDegreeNames)))
                    {
                        switch (academicDegreeName)
                        {
                            case AcademicDegreeNames.Undefined:
                                break;
                            case AcademicDegreeNames.MaxDegreeNames:
                                break;
                            default:
                                results.Add(new InteractionObjectPair(new Definition(academicDegreeName), target));
                                break;
                        }
                    }
                }

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    ToggleGraduationOutfit toggleGraduationOutfit = new ToggleGraduationOutfit();
                    toggleGraduationOutfit.SetAcademicDegreeName(mAcademicDegreeName);
                    toggleGraduationOutfit.Init(ref parameters);
                    return toggleGraduationOutfit;
                }

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    if (GetGraduationOutfitEnabled(actor.SimDescription, mAcademicDegreeName))
                    {
                        return Common.Localize(actor.IsFemale, GetLocalizationKey(mAcademicDegreeName) + "DisableInteractionName");
                    }
                    return Common.Localize(actor.IsFemale, GetLocalizationKey(mAcademicDegreeName) + "EnableInteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, GetLocalizationKey(mAcademicDegreeName) + "Path0"),
                        Common.Localize(isFemale, GetLocalizationKey(mAcademicDegreeName) + "Path1")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return (actor == target && Tuning.kShowSimMenu || target as Sim == null && Tuning.kShowObjectMenu) && actor.SimDescription.YoungAdultOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public static string GetLocalizationKey(AcademicDegreeNames academicDegreeName)
            {
                return "CustomGraduationOutfit/" + new Dictionary<AcademicDegreeNames, string>
                {
                    {
                        AcademicDegreeNames.Business,
                        "ToggleGraduationBusinessOutfit/"
                    },
                    {
                        AcademicDegreeNames.Technology,
                        "ToggleGraduationTechnologyOutfit/"
                    },
                    {
                        AcademicDegreeNames.Science,
                        "ToggleGraduationScienceMedOutfit/"
                    },
                    {
                        AcademicDegreeNames.FineArts,
                        "ToggleGraduationFineArtsOutfit/"
                    },
                    {
                        AcademicDegreeNames.Communications,
                        "ToggleGraduationCommOutfit/"
                    },
                    {
                        AcademicDegreeNames.PhysEd,
                        "ToggleGraduationPhysEdOutfit/"
                    }
                }[academicDegreeName];
            }

            public override bool Run()
            {
                if (GetGraduationOutfitEnabled(Actor.SimDescription, mAcademicDegreeName))
                {
                    DisableGraduationOutfit(Actor.SimDescription, mAcademicDegreeName);
                    Common.Notify(Common.Localize(Actor.IsFemale, GetLocalizationKey(mAcademicDegreeName) + "DisabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                else
                {
                    EnableGraduationOutfit(Actor.SimDescription, mAcademicDegreeName);
                    Common.Notify(Common.Localize(Actor.IsFemale, GetLocalizationKey(mAcademicDegreeName) + "EnabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                return true;
            }

            public void SetAcademicDegreeName(AcademicDegreeNames academicDegreeName)
            {
                mAcademicDegreeName = academicDegreeName;
            }
        }

        public class UniversityGraduationCeremony : Annex.UniversityGraduationCeremony
        {
            public class DefinitionModified : InteractionDefinition<Sim, Annex, UniversityGraduationCeremony>
            {
                public override bool Test(Sim actor, Annex target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (target.IsAGraduationCeremonyInProgressAndIfNecessaryStartOne())
                    {
                        return target.IsSimGraduating(actor.SimDescription);
                    }
                    return false;
                }
            }

            public override bool BeforeEnteringRabbitHole()
            {
                ResourceKey graduationUniformKey = GetGraduationUniformKey(Actor);
                if (graduationUniformKey != ResourceKey.kInvalidResourceKey)
                {
                    Actor.GraduationOutfitKey = graduationUniformKey;
                    Sim.SwitchOutfitHelper spin = new Sim.SwitchOutfitHelper(Actor, graduationUniformKey);
                    Actor.SwitchToOutfitWithSpin(spin);
                }
                return true;
            }
        }

        static void AddInteractions(GameObject gameObject)
        {
            if (gameObject != null && !gameObject.Interactions.Exists(interaction => interaction.InteractionDefinition.GetType() == EditGraduationOutfit.Singleton.GetType()) && GameUtils.IsInstalled(ProductVersion.EP9))
            {
                gameObject.AddInteraction(EditGraduationOutfit.Singleton);
                gameObject.AddInteraction(ResetGraduationOutfit.Singleton);
                gameObject.AddInteraction(ToggleGraduationOutfit.Singleton);
            }
        }

        static void DisableGraduationBusinessOutfit(SimDescription simDescription)
        {
            if (GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.Business))
            {
                sGraduationBusinessOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
        }

        static void DisableGraduationCommOutfit(SimDescription simDescription)
        {
            if (GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.Communications))
            {
                sGraduationCommOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
        }

        static void DisableGraduationFineArtsOutfit(SimDescription simDescription)
        {
            if (GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.FineArts))
            {
                sGraduationFineArtsOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
        }

        static void DisableGraduationOutfit(SimDescription simDescription, AcademicDegreeNames academicDegreeName)
        {
            switch (academicDegreeName)
            {
                case AcademicDegreeNames.Business:
                    DisableGraduationBusinessOutfit(simDescription);
                    break;
                case AcademicDegreeNames.Technology:
                    DisableGraduationTechnologyOutfit(simDescription);
                    break;
                case AcademicDegreeNames.Science:
                    DisableGraduationScienceMedOutfit(simDescription);
                    break;
                case AcademicDegreeNames.FineArts:
                    DisableGraduationFineArtsOutfit(simDescription);
                    break;
                case AcademicDegreeNames.Communications:
                    DisableGraduationCommOutfit(simDescription);
                    break;
                case AcademicDegreeNames.PhysEd:
                    DisableGraduationPhysEdOutfit(simDescription);
                    break;
            }
        }

        static void DisableGraduationPhysEdOutfit(SimDescription simDescription)
        {
            if (GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.PhysEd))
            {
                sGraduationPhysEdOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
        }

        static void DisableGraduationScienceMedOutfit(SimDescription simDescription)
        {
            if (GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.Science))
            {
                sGraduationScienceMedOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
        }

        static void DisableGraduationTechnologyOutfit(SimDescription simDescription)
        {
            if (GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.Technology))
            {
                sGraduationTechnologyOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
        }

        static void EnableGraduationBusinessOutfit(SimDescription simDescription)
        {
            if (!GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.Business))
            {
                sGraduationBusinessOutfitDisabledList.Remove(simDescription.SimDescriptionId);
            }
        }

        static void EnableGraduationCommOutfit(SimDescription simDescription)
        {
            if (!GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.Communications))
            {
                sGraduationCommOutfitDisabledList.Remove(simDescription.SimDescriptionId);
            }
        }

        static void EnableGraduationFineArtsOutfit(SimDescription simDescription)
        {
            if (!GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.FineArts))
            {
                sGraduationFineArtsOutfitDisabledList.Remove(simDescription.SimDescriptionId);
            }
        }

        static void EnableGraduationOutfit(SimDescription simDescription, AcademicDegreeNames academicDegreeName)
        {
            switch (academicDegreeName)
            {
                case AcademicDegreeNames.Business:
                    EnableGraduationBusinessOutfit(simDescription);
                    break;
                case AcademicDegreeNames.Technology:
                    EnableGraduationTechnologyOutfit(simDescription);
                    break;
                case AcademicDegreeNames.Science:
                    EnableGraduationScienceMedOutfit(simDescription);
                    break;
                case AcademicDegreeNames.FineArts:
                    EnableGraduationFineArtsOutfit(simDescription);
                    break;
                case AcademicDegreeNames.Communications:
                    EnableGraduationCommOutfit(simDescription);
                    break;
                case AcademicDegreeNames.PhysEd:
                    EnableGraduationPhysEdOutfit(simDescription);
                    break;
            }
        }

        static void EnableGraduationPhysEdOutfit(SimDescription simDescription)
        {
            if (!GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.PhysEd))
            {
                sGraduationPhysEdOutfitDisabledList.Remove(simDescription.SimDescriptionId);
            }
        }

        static void EnableGraduationScienceMedOutfit(SimDescription simDescription)
        {
            if (!GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.Science))
            {
                sGraduationScienceMedOutfitDisabledList.Remove(simDescription.SimDescriptionId);
            }
        }

        static void EnableGraduationTechnologyOutfit(SimDescription simDescription)
        {
            if (!GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.Technology))
            {
                sGraduationTechnologyOutfitDisabledList.Remove(simDescription.SimDescriptionId);
            }
        }

        static bool GetGraduationOutfitEnabled(SimDescription simDescription, AcademicDegreeNames academicDegreeName)
        {
            return !new Dictionary<AcademicDegreeNames, bool>
            {
                {
                    AcademicDegreeNames.Undefined,
                    true
                },
                {
                    AcademicDegreeNames.Business,
                    sGraduationBusinessOutfitDisabledList.Contains(simDescription.SimDescriptionId)
                },
                {
                    AcademicDegreeNames.Technology,
                    sGraduationTechnologyOutfitDisabledList.Contains(simDescription.SimDescriptionId)
                },
                {
                    AcademicDegreeNames.Science,
                    sGraduationScienceMedOutfitDisabledList.Contains(simDescription.SimDescriptionId)
                },
                {
                    AcademicDegreeNames.FineArts,
                    sGraduationFineArtsOutfitDisabledList.Contains(simDescription.SimDescriptionId)
                },
                {
                    AcademicDegreeNames.Communications,
                    sGraduationCommOutfitDisabledList.Contains(simDescription.SimDescriptionId)
                },
                {
                    AcademicDegreeNames.PhysEd,
                    sGraduationPhysEdOutfitDisabledList.Contains(simDescription.SimDescriptionId)
                }
            }[academicDegreeName];
        }

        public static string GetGraduationOutfitName(Sim actor, AcademicDegreeNames academicDegreeName)
        {
            return (actor.SimDescription.Elder ? "e" : "a") + (actor.IsMale ? "m" : "f") + CollegeGraduation.kBaseGradUniformName + new Dictionary<AcademicDegreeNames, string>
            {
                {
                    AcademicDegreeNames.Business,
                    "Business"
                },
                {
                    AcademicDegreeNames.Technology,
                    "Technology"
                },
                {
                    AcademicDegreeNames.Science,
                    "ScienceMed"
                },
                {
                    AcademicDegreeNames.FineArts,
                    "FineArts"
                },
                {
                    AcademicDegreeNames.Communications,
                    "Comm"
                },
                {
                    AcademicDegreeNames.PhysEd,
                    "PhysEd"
                }
            }[academicDegreeName];
        }

        public static ResourceKey GetGraduationUniformKey(Sim actor)
        {
            AcademicDegreeNames academicDegreeName = AcademicDegreeNames.Undefined;
            if (!(actor.CareerManager == null || actor.CareerManager.OccupationAsAcademicCareer == null || actor.CareerManager.OccupationAsAcademicCareer.DegreeInformation == null))
            {
                academicDegreeName = actor.CareerManager.OccupationAsAcademicCareer.DegreeInformation.AcademicDegreeName;
            }
            if (!(GetGraduationOutfitEnabled(actor.SimDescription, academicDegreeName) || actor.CareerManager == null || actor.CareerManager.DegreeManager == null))
            {
                academicDegreeName = AcademicDegreeNames.Undefined;
                List<AcademicDegreeNames> completedDegreeNames = new List<AcademicDegreeNames>();
                foreach (IDegreeEntry degreeEntry in actor.CareerManager.DegreeManager.GetCompletedDegreeEntries())
                {
                    completedDegreeNames.Add((AcademicDegreeNames)degreeEntry.DegreeGuid);
                }
                while (completedDegreeNames.Count > 0)
                {
                    academicDegreeName = RandomUtil.GetRandomObjectFromList(completedDegreeNames);
                    if (GetGraduationOutfitEnabled(actor.SimDescription, academicDegreeName))
                    {
                        break;
                    }
                    completedDegreeNames.Remove(academicDegreeName);
                    academicDegreeName = AcademicDegreeNames.Undefined;
                }
            }
            if (academicDegreeName == AcademicDegreeNames.Undefined)
            {
                return ResourceKey.kInvalidResourceKey;
            }
            string outfitName = GetGraduationOutfitName(actor, academicDegreeName);
            return Common.CreateAndAddSpecialOutfit(actor, outfitName, ResourceKey.CreateOutfitKeyFromProductVersion(outfitName, ProductVersion.EP9));
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

        static void OnPreLoad()
        {
            Annex.UniversityGraduationCeremony.Singleton = new UniversityGraduationCeremony.DefinitionModified();
            CollegeGraduation.GraduateInPlace.Singleton = new GraduateInPlace.DefinitionModified();
            Common.CopyTuning(typeof(Annex), typeof(Annex.UniversityGraduationCeremony.Definition), typeof(UniversityGraduationCeremony.DefinitionModified));
            Common.CopyTuning(typeof(CollegeGraduation), typeof(CollegeGraduation.GraduateInPlace.Definition), typeof(GraduateInPlace.DefinitionModified));
        }

        static ListenerAction OnSimDescriptionDisposed(Event e)
        {
            try
            {
                Sim sim = e.TargetObject as Sim;
                if (sim != null)
                {
                    foreach (AcademicDegreeNames academicDegreeName in Enum.GetValues(typeof(AcademicDegreeNames)))
                    {
                        switch (academicDegreeName)
                        {
                            case AcademicDegreeNames.Undefined:
                                break;
                            case AcademicDegreeNames.MaxDegreeNames:
                                break;
                            default:
                                EnableGraduationOutfit(sim.SimDescription, academicDegreeName);
                                break;
                        }
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
            EventTracker.RemoveListener(sSimDescriptionDisposedListener);
            EventTracker.RemoveListener(sSimSelectedListener);
            sSimDescriptionDisposedListener = null;
            sSimSelectedListener = null;
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