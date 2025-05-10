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
using System;
using System.Collections.Generic;
using static Destrospean.Common;
using static Sims3.Gameplay.Destrospean.CustomOutfits;

namespace Destrospean
{
    public class CustomGraduationOutfit
    {
        [Tunable]
        protected static bool kInstantiator;

        [PersistableStatic]
        static List<ulong> sGraduationBusinessOutfitDisabledList;

        [PersistableStatic]
        static List<ulong> sGraduationCommOutfitDisabledList;

        [PersistableStatic]
        static List<ulong> sGraduationFineArtsOutfitDisabledList;

        [PersistableStatic]
        static List<ulong> sGraduationPhysEdOutfitDisabledList;

        [PersistableStatic]
        static List<ulong> sGraduationScienceMedOutfitDisabledList;

        [PersistableStatic]
        static List<ulong> sGraduationTechnologyOutfitDisabledList;

        [PersistableStatic]
        static EventListener sObjectBoughtListener;

        [PersistableStatic]
        static EventListener sSimDestroyedListener;

        [PersistableStatic]
        static EventListener sSimSelectedListener;

        static CustomGraduationOutfit()
        {
            kInstantiator = false;
            sGraduationBusinessOutfitDisabledList = new List<ulong>();
            sGraduationCommOutfitDisabledList = new List<ulong>();
            sGraduationFineArtsOutfitDisabledList = new List<ulong>();
            sGraduationPhysEdOutfitDisabledList = new List<ulong>();
            sGraduationScienceMedOutfitDisabledList = new List<ulong>();
            sGraduationTechnologyOutfitDisabledList = new List<ulong>();
            sObjectBoughtListener = null;
            sSimDestroyedListener = null;
            sSimSelectedListener = null;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
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
                    return Localize(actor.IsFemale, GetLocalizationKey(mAcademicDegreeName) + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Localize(isFemale, GetLocalizationKey(mAcademicDegreeName) + "Path0"),
                        Localize(isFemale, GetLocalizationKey(mAcademicDegreeName) + "Path1")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return !((target is Sim && actor != target) || actor.SimDescription.TeenOrBelow || !actor.SimDescription.IsHuman || actor.SimDescription.IsRobot || isAutonomous);
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
                return EditSpecialOutfit(Actor, GetLocalizationKey(mAcademicDegreeName), outfitName, outfitName, ProductVersion.EP9);
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
                    return Localize(actor.IsFemale, GetLocalizationKey(mAcademicDegreeName) + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Localize(isFemale, GetLocalizationKey(mAcademicDegreeName) + "Path0"),
                        Localize(isFemale, GetLocalizationKey(mAcademicDegreeName) + "Path1")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return !(!actor.SimDescription.HasSpecialOutfit(GetGraduationOutfitName(actor, mAcademicDegreeName)) || (target is Sim && actor != target) || actor.SimDescription.TeenOrBelow || !actor.SimDescription.IsHuman || actor.SimDescription.IsRobot || isAutonomous);
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
                Notify(Localize(Actor.IsFemale, GetLocalizationKey(mAcademicDegreeName) + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
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
                        return Localize(actor.IsFemale, GetLocalizationKey(mAcademicDegreeName) + "DisableInteractionName");
                    }
                    return Localize(actor.IsFemale, GetLocalizationKey(mAcademicDegreeName) + "EnableInteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Localize(isFemale, GetLocalizationKey(mAcademicDegreeName) + "Path0"),
                        Localize(isFemale, GetLocalizationKey(mAcademicDegreeName) + "Path1")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return !((target is Sim && actor != target) || actor.SimDescription.TeenOrBelow || !actor.SimDescription.IsHuman || actor.SimDescription.IsRobot || isAutonomous);
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
                    Notify(Localize(Actor.IsFemale, GetLocalizationKey(mAcademicDegreeName) + "DisabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                else
                {
                    EnableGraduationOutfit(Actor.SimDescription, mAcademicDegreeName);
                    Notify(Localize(Actor.IsFemale, GetLocalizationKey(mAcademicDegreeName) + "EnabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
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
            if (gameObject == null || !GameUtils.IsInstalled(ProductVersion.EP9))
            {
                return;
            }
            foreach (InteractionObjectPair interaction in gameObject.Interactions)
            {
                if (interaction.InteractionDefinition.GetType() == EditGraduationOutfit.Singleton.GetType())
                {
                    return;
                }
            }
            gameObject.AddInteraction(EditGraduationOutfit.Singleton);
            gameObject.AddInteraction(ResetGraduationOutfit.Singleton);
            gameObject.AddInteraction(ToggleGraduationOutfit.Singleton);
        }

        static void DisableGraduationBusinessOutfit(SimDescription simDescription)
        {
            if (sGraduationBusinessOutfitDisabledList == null)
            {
                sGraduationBusinessOutfitDisabledList = new List<ulong>();
            }
            if (GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.Business))
            {
                sGraduationBusinessOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
            UpdateListeners();
        }

        static void DisableGraduationCommOutfit(SimDescription simDescription)
        {
            if (sGraduationCommOutfitDisabledList == null)
            {
                sGraduationCommOutfitDisabledList = new List<ulong>();
            }
            if (GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.Communications))
            {
                sGraduationCommOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
            UpdateListeners();
        }

        static void DisableGraduationFineArtsOutfit(SimDescription simDescription)
        {
            if (sGraduationFineArtsOutfitDisabledList == null)
            {
                sGraduationFineArtsOutfitDisabledList = new List<ulong>();
            }
            if (GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.FineArts))
            {
                sGraduationFineArtsOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
            UpdateListeners();
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
            if (sGraduationPhysEdOutfitDisabledList == null)
            {
                sGraduationPhysEdOutfitDisabledList = new List<ulong>();
            }
            if (GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.PhysEd))
            {
                sGraduationPhysEdOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
            UpdateListeners();
        }

        static void DisableGraduationScienceMedOutfit(SimDescription simDescription)
        {
            if (sGraduationScienceMedOutfitDisabledList == null)
            {
                sGraduationScienceMedOutfitDisabledList = new List<ulong>();
            }
            if (GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.Science))
            {
                sGraduationScienceMedOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
            UpdateListeners();
        }

        static void DisableGraduationTechnologyOutfit(SimDescription simDescription)
        {
            if (sGraduationTechnologyOutfitDisabledList == null)
            {
                sGraduationTechnologyOutfitDisabledList = new List<ulong>();
            }
            if (GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.Technology))
            {
                sGraduationTechnologyOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
            UpdateListeners();
        }

        static void EnableGraduationBusinessOutfit(SimDescription simDescription)
        {
            if (!GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.Business))
            {
                sGraduationBusinessOutfitDisabledList.Remove(simDescription.SimDescriptionId);
                UpdateListeners();
            }
        }

        static void EnableGraduationCommOutfit(SimDescription simDescription)
        {
            if (!GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.Communications))
            {
                sGraduationCommOutfitDisabledList.Remove(simDescription.SimDescriptionId);
                UpdateListeners();
            }
        }

        static void EnableGraduationFineArtsOutfit(SimDescription simDescription)
        {
            if (!GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.FineArts))
            {
                sGraduationFineArtsOutfitDisabledList.Remove(simDescription.SimDescriptionId);
                UpdateListeners();
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
                UpdateListeners();
            }
        }

        static void EnableGraduationScienceMedOutfit(SimDescription simDescription)
        {
            if (!GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.Science))
            {
                sGraduationScienceMedOutfitDisabledList.Remove(simDescription.SimDescriptionId);
                UpdateListeners();
            }
        }

        static void EnableGraduationTechnologyOutfit(SimDescription simDescription)
        {
            if (!GetGraduationOutfitEnabled(simDescription, AcademicDegreeNames.Technology))
            {
                sGraduationTechnologyOutfitDisabledList.Remove(simDescription.SimDescriptionId);
                UpdateListeners();
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
            return CreateAndAddSpecialOutfit(actor, outfitName, ResourceKey.CreateOutfitKeyFromProductVersion(outfitName, ProductVersion.EP9));
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
            Annex.UniversityGraduationCeremony.Singleton = new UniversityGraduationCeremony.DefinitionModified();
            CollegeGraduation.GraduateInPlace.Singleton = new GraduateInPlace.DefinitionModified();
            CopyTuning(typeof(Annex), typeof(Annex.UniversityGraduationCeremony.Definition), typeof(UniversityGraduationCeremony.DefinitionModified));
            CopyTuning(typeof(CollegeGraduation), typeof(CollegeGraduation.GraduateInPlace.Definition), typeof(GraduateInPlace.DefinitionModified));
        }

        static ListenerAction OnSimDestroyed(Event e)
        {
            try
            {
                if (e.Actor is Sim sim)
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
            EventTracker.RemoveListener(sSimDestroyedListener);
            EventTracker.RemoveListener(sSimSelectedListener);
            sObjectBoughtListener = null;
            sSimDestroyedListener = null;
            sSimSelectedListener = null;
        }

        static void UpdateListeners()
        {
            if (sObjectBoughtListener != null)
            {
                EventTracker.RemoveListener(sObjectBoughtListener);
                sObjectBoughtListener = null;
            }
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
            sObjectBoughtListener = EventTracker.AddListener(EventTypeId.kBoughtObject, OnObjectBought);
            sSimDestroyedListener = EventTracker.AddListener(EventTypeId.kSimDescriptionDisposed, OnSimDestroyed);
            sSimSelectedListener = EventTracker.AddListener(EventTypeId.kEventSimSelected, OnSimSelected);
        }
    }
}