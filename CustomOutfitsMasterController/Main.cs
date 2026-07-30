using System;
using System.Collections.Generic;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Academics;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Careers;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Pools;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using Sims3.UI.CAS;
using Tuning = Sims3.Gameplay.Destrospean.CustomizableUncustomizableOutfits;

namespace Destrospean.CustomizableUncustomizableOutfits.MasterController
{
    public class Main
    {
        [Tunable]
        protected static bool kInstantiator;

        static Main()
        {
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
        }

        public class EditAfterschoolActivityOutfit : AfterschoolActivityOutfit.EditAfterschoolActivityOutfit
        {
            public class DefinitionModified : ImmediateInteractionDefinition<Sim, GameObject, EditAfterschoolActivityOutfit>
            {
                public AfterschoolActivityType mAfterschoolActivityType;

                public DefinitionModified()
                {
                }

                public DefinitionModified(AfterschoolActivityType afterschoolActivityType)
                {
                    mAfterschoolActivityType = afterschoolActivityType;
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, GameObject target, List<InteractionObjectPair> results)
                {
                    int index = 0;
                    foreach (AfterschoolActivityType afterschoolActivityType in Enum.GetValues(typeof(AfterschoolActivityType)))
                    {
                        results.Add(new InteractionObjectPair(new DefinitionModified(afterschoolActivityType), target));
                        if (index == 1)
                        {
                            break;
                        }
                        index++;
                    }
                }

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    EditAfterschoolActivityOutfit editAfterschoolActivityOutfit = new EditAfterschoolActivityOutfit();
                    editAfterschoolActivityOutfit.SetAfterschoolActivityType(mAfterschoolActivityType);
                    editAfterschoolActivityOutfit.Init(ref parameters);
                    return editAfterschoolActivityOutfit;
                }

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return Common.Localize(actor.IsFemale, GetLocalizationKey(mAfterschoolActivityType) + "InteractionName");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new string[]
                    {
                        Common.Localize(isFemale, GetLocalizationKey(mAfterschoolActivityType) + "Path")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return (actor == target && Tuning.kShowSimMenu || target as Sim == null && Tuning.kShowObjectMenu) && actor.SimDescription.Child && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous && AfterschoolActivity.HasAfterschoolActivityOfType(actor, mAfterschoolActivityType);
                }
            }

            public override bool Run()
            {
                string outfitName = AfterschoolActivityOutfit.GetAfterschoolActivityOutfitName(Actor, mAfterschoolActivityType);
                return EditSpecialOutfit(Actor, GetLocalizationKey(mAfterschoolActivityType), outfitName, outfitName, ProductVersion.EP4);
            }
        }

        public class EditBachelorPartyOutfit : BachelorPartyOutfit.EditBachelorPartyOutfit
        {
            public class DefinitionModified : ImmediateInteractionDefinition<Sim, GameObject, EditBachelorPartyOutfit>
            {
                public BachelorPartyOutfitTypes mOutfitType;

                public DefinitionModified()
                {
                }

                public DefinitionModified(BachelorPartyOutfitTypes outfitType)
                {
                    mOutfitType = outfitType;
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, GameObject target, List<InteractionObjectPair> results)
                {
                    foreach (BachelorPartyOutfitTypes outfitType in Enum.GetValues(typeof(BachelorPartyOutfitTypes)))
                    {
                        results.Add(new InteractionObjectPair(new DefinitionModified(outfitType), target));
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

            public override bool Run()
            {
                string outfitName = BachelorPartyOutfit.GetBachelorPartyOutfitName(Actor, mOutfitType);
                if (!Actor.SimDescription.HasSpecialOutfit(outfitName))
                {
                    Actor.SimDescription.AddSpecialOutfit(Replacements.BachelorParty.CreateBachelorPartyOutfit(Actor, mOutfitType), outfitName);
                }
                return EditSpecialOutfit(Actor, GetLocalizationKey(mOutfitType), outfitName);
            }
        }

        public class EditBeekeeperOutfit : BeekeeperOutfit.EditBeekeeperOutfit
        {
            public class DefinitionModified : ImmediateInteractionDefinition<Sim, GameObject, EditBeekeeperOutfit>
            {
                Definition mDefinitionBase = new Definition();

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return mDefinitionBase.GetInteractionName(actor, target, interaction);
                }

                public override string[] GetPath(bool isFemale)
                {
                    return mDefinitionBase.GetPath(isFemale);
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return mDefinitionBase.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
                }
            }

            public override bool Run()
            {
                string outfitName = BeekeeperOutfit.GetBeekeeperOutfitName(Actor);
                return EditSpecialOutfit(Actor, sLocalizationKey, outfitName, outfitName, ProductVersion.EP7);
            }
        }

        public class EditChefOutfit : TeppanyakiChefOutfit.EditChefOutfit
        {
            public class DefinitionModified : ImmediateInteractionDefinition<Sim, GameObject, EditChefOutfit>
            {
                Definition mDefinitionBase = new Definition();

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return mDefinitionBase.GetInteractionName(actor, target, interaction);
                }

                public override string[] GetPath(bool isFemale)
                {
                    return mDefinitionBase.GetPath(isFemale);
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return mDefinitionBase.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
                }
            }

            public override bool Run()
            {
                return EditSpecialOutfit(Actor, sLocalizationKey, TeppanyakiChefOutfit.kChefSpecialOutfitKey, TeppanyakiChefOutfit.GetChefOutfitName(Actor), ProductVersion.BaseGame);
            }
        }

        public class EditChemistryLabOutfit : ChemistryLabOutfit.EditChemistryLabOutfit
        {
            public class DefinitionModified : ImmediateInteractionDefinition<Sim, GameObject, EditChemistryLabOutfit>
            {
                Definition mDefinitionBase = new Definition();

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return mDefinitionBase.GetInteractionName(actor, target, interaction);
                }

                public override string[] GetPath(bool isFemale)
                {
                    return mDefinitionBase.GetPath(isFemale);
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return mDefinitionBase.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
                }
            }

            public override bool Run()
            {
                return EditSpecialOutfit(Actor, sLocalizationKey, ChemistryLabOutfit.kChemistryLabSpecialOutfitKey, ChemistryLabOutfit.GetChemistryLabOutfitName(Actor), ProductVersion.EP4);
            }
        }

        public class EditGraduationOutfit : GraduationOutfit.EditGraduationOutfit
        {
            public class DefinitionModified : ImmediateInteractionDefinition<Sim, GameObject, EditGraduationOutfit>
            {
                public AcademicDegreeNames mAcademicDegreeName;

                public DefinitionModified()
                {
                }

                public DefinitionModified(AcademicDegreeNames academicDegreeName)
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
                                results.Add(new InteractionObjectPair(new DefinitionModified(academicDegreeName), target));
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

            public override bool Run()
            {
                string outfitName = GraduationOutfit.GetGraduationOutfitName(Actor, mAcademicDegreeName);
                return EditSpecialOutfit(Actor, GetLocalizationKey(mAcademicDegreeName), outfitName, outfitName, ProductVersion.EP9);
            }
        }

        public class EditHighSchoolGraduationOutfit : HighSchoolGraduationOutfit.EditHighSchoolGraduationOutfit
        {
            public class DefinitionModified : ImmediateInteractionDefinition<Sim, GameObject, EditHighSchoolGraduationOutfit>
            {
                Definition mDefinitionBase = new Definition();

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return mDefinitionBase.GetInteractionName(actor, target, interaction);
                }

                public override string[] GetPath(bool isFemale)
                {
                    return mDefinitionBase.GetPath(isFemale);
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return mDefinitionBase.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
                }
            }

            public override bool Run()
            {
                return EditSpecialOutfit(Actor, sLocalizationKey, HighSchoolGraduationOutfit.kHighSchoolGraduationSpecialOutfitKey, HighSchoolGraduationOutfit.GetHighSchoolGraduationOutfitName(Actor), ProductVersion.EP4);
            }
        }

        public class EditMassageTableOutfit : MassageTableOutfit.EditMassageTableOutfit
        {
            public class DefinitionModified : ImmediateInteractionDefinition<Sim, GameObject, EditMassageTableOutfit>
            {
                Definition mDefinitionBase = new Definition();

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return mDefinitionBase.GetInteractionName(actor, target, interaction);
                }

                public override string[] GetPath(bool isFemale)
                {
                    return mDefinitionBase.GetPath(isFemale);
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return mDefinitionBase.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
                }
            }

            public override bool Run()
            {
                return EditSpecialOutfit(Actor, sLocalizationKey, MassageTableOutfit.kMassageTableSpecialOutfitKey, MassageTableOutfit.GetMassageTableOutfitName(Actor), ProductVersion.EP3, Actor.SimDescription.GetOutfit(OutfitCategories.Swimwear, 0));
            }
        }

        public class EditMechanicalBullOutfit : MechanicalBullOutfit.EditMechanicalBullOutfit
        {
            public class DefinitionModified : ImmediateInteractionDefinition<Sim, GameObject, EditMechanicalBullOutfit>
            {
                Definition mDefinitionBase = new Definition();

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return mDefinitionBase.GetInteractionName(actor, target, interaction);
                }

                public override string[] GetPath(bool isFemale)
                {
                    return mDefinitionBase.GetPath(isFemale);
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return mDefinitionBase.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
                }
            }

            public override bool Run()
            {
                return EditSpecialOutfit(Actor, sLocalizationKey, MechanicalBullOutfit.kMechanicalBullSpecialOutfitKey, MechanicalBullOutfit.GetMechanicalBullOutfitName(Actor), ProductVersion.EP6);
            }
        }

        public class EditSaunaOutfit : SaunaOutfit.EditSaunaOutfit
        {
            public class DefinitionModified : ImmediateInteractionDefinition<Sim, GameObject, EditSaunaOutfit>
            {
                Definition mDefinitionBase = new Definition();

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return mDefinitionBase.GetInteractionName(actor, target, interaction);
                }

                public override string[] GetPath(bool isFemale)
                {
                    return mDefinitionBase.GetPath(isFemale);
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return mDefinitionBase.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
                }
            }

            public override bool Run()
            {
                Actor.SimDescription.AddSpecialOutfit(new SimOutfit(Actor.SimDescription.GetOutfit(OutfitCategories.Swimwear, 0).Key), SaunaOutfit.kSaunaSpecialOutfitKey);
                return EditSpecialOutfit(Actor, sLocalizationKey, SaunaOutfit.kSaunaSpecialOutfitKey);
            }
        }

        public class EditSingedOutfit : SingedOutfit.EditSingedOutfit
        {
            public class DefinitionModified : ImmediateInteractionDefinition<Sim, GameObject, EditSingedOutfit>
            {
                Definition mDefinitionBase = new Definition();

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return mDefinitionBase.GetInteractionName(actor, target, interaction);
                }

                public override string[] GetPath(bool isFemale)
                {
                    return mDefinitionBase.GetPath(isFemale);
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return mDefinitionBase.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
                }
            }

            public override bool Run()
            {
                if (!Actor.SimDescription.HasSpecialOutfit(SingedOutfit.kSingedSpecialOutfitKey) && !string.IsNullOrEmpty(OutfitUtils.GetSingedOutfit(Actor)))
                {
                    Actor.SimDescription.AddSpecialOutfit(SingedOutfit.CreateSingedOutfit(Actor), SingedOutfit.kSingedSpecialOutfitKey);
                }
                return EditSpecialOutfit(Actor, sLocalizationKey, SingedOutfit.kSingedSpecialOutfitKey);
            }
        }

        public class EditSkatingOutfit : SkatingOutfit.EditSkatingOutfit
        {
            public class DefinitionModified : ImmediateInteractionDefinition<Sim, GameObject, EditSkatingOutfit>
            {
                public SkatingOutfit.SkatingTypes mSkatingType;

                public DefinitionModified()
                {
                }

                public DefinitionModified(SkatingOutfit.SkatingTypes skatingType)
                {
                    mSkatingType = skatingType;
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, GameObject target, List<InteractionObjectPair> results)
                {
                    foreach (SkatingOutfit.SkatingTypes skatingType in Enum.GetValues(typeof(SkatingOutfit.SkatingTypes)))
                    {
                        results.Add(new InteractionObjectPair(new DefinitionModified(skatingType), target));
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
                    return InteractionDefinitionUtilities.FromBool((Tuning.kShowObjectMenu && SkatableTerrain.GetPondSkatingAreaAtPoint(parameters.Hit.mPoint) != null && mSkatingType == SkatingOutfit.SkatingTypes.Ice && PondManager.ArePondsFrozen() || parameters.Target is ISkatableObject && mSkatingType == (((ISkatableObject)parameters.Target).IsIceRink ? SkatingOutfit.SkatingTypes.Ice : SkatingOutfit.SkatingTypes.Roller) || Tuning.kShowSimMenu && parameters.Actor == parameters.Target) && parameters.Actor.SimDescription.ChildOrAbove && parameters.Actor.SimDescription.IsHuman && !parameters.Actor.SimDescription.IsRobot && !parameters.Autonomous);
                }
            }

            public override bool Run()
            {
                string outfitName = SkatingOutfit.GetSkatingOutfitName(Actor, mSkatingType);
                if (!Actor.SimDescription.HasSpecialOutfit(outfitName))
                {
                    Actor.SimDescription.AddSpecialOutfit(SkatingOutfit.CreateSkatingOutfit(Actor, mSkatingType), outfitName);
                }
                return EditSpecialOutfit(Actor, GetLocalizationKey(mSkatingType), outfitName);
            }
        }

        public class EditTowelOutfit : TowelOutfit.EditTowelOutfit
        {
            public class DefinitionModified : ImmediateInteractionDefinition<Sim, GameObject, EditTowelOutfit>
            {
                Definition mDefinitionBase = new Definition();

                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    return mDefinitionBase.GetInteractionName(actor, target, interaction);
                }

                public override string[] GetPath(bool isFemale)
                {
                    return mDefinitionBase.GetPath(isFemale);
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return mDefinitionBase.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
                }

                public override InteractionTestResult Test(ref InteractionInstanceParameters parameters, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return mDefinitionBase.Test(ref parameters, ref greyedOutTooltipCallback);
                }
            }

            public override bool Run()
            {
                return EditSpecialOutfit(Actor, sLocalizationKey, TowelOutfit.kTowelSpecialOutfitKey, TowelOutfit.GetTowelOutfitName(Actor), ProductVersion.EP3, Actor.SimDescription.GetOutfit(OutfitCategories.Swimwear, 0));
            }
        }

        public static bool EditSpecialOutfit(Sim actor, string localizationKey, string specialOutfitKey)
        {
            SimDescription simDescription = actor.SimDescription;
            if (!simDescription.HasSpecialOutfit(specialOutfitKey))
            {
                simDescription.AddSpecialOutfit(simDescription.GetOutfit(OutfitCategories.Everyday, 0), specialOutfitKey);
            }
            OutfitCategories previousOutfitCategory = actor.CurrentOutfitCategory;
            int previousOutfitIndex = actor.CurrentOutfitIndex;
            simDescription.AddOutfit(simDescription.GetSpecialOutfit(specialOutfitKey), OutfitCategories.Everyday, 0);
            simDescription.RemoveSpecialOutfit(specialOutfitKey);
            actor.SwitchToOutfitWithoutSpin(OutfitCategories.Everyday, 0);
            CASLogic casLogic = CASLogic.GetSingleton();
            new NRaas.MasterControllerSpace.Sims.Stylist().Perform(new NRaas.CommonSpace.Options.GameHitParameters<GameObject>(actor, actor, GameObjectHit.NoHit));
            casLogic.ShowUI += Common.OnShowUI;
            //Common.Notify(Common.Localize(actor.IsFemale, localizationKey + "Warning", actor.Name), simDescription, StyledNotification.NotificationStyle.kSystemMessage);
            while (GameStates.NextInWorldStateId != 0)
            {
                NRaas.SpeedTrap.Sleep();
            }
            casLogic.ShowUI -= Common.OnShowUI;
            simDescription.AddSpecialOutfit(simDescription.GetOutfit(OutfitCategories.Everyday, 0), specialOutfitKey);
            simDescription.RemoveOutfit(OutfitCategories.Everyday, 0, true);
            actor.SwitchToOutfitWithoutSpin(previousOutfitCategory, previousOutfitIndex);
            if (!CASChangeReporter.Instance.CasCancelled)
            {
                Common.Notify(Common.Localize(actor.IsFemale, localizationKey + "Feedback", actor.Name), simDescription, StyledNotification.NotificationStyle.kSystemMessage);
            }
            return true;
        }

        public static bool EditSpecialOutfit(Sim actor, string localizationKey, string specialOutfitKey, string outfitName, uint group)
        {
            return EditSpecialOutfit(actor, localizationKey, specialOutfitKey, outfitName, group, actor.SimDescription.GetOutfit(OutfitCategories.Everyday, 0));
        }

        public static bool EditSpecialOutfit(Sim actor, string localizationKey, string specialOutfitKey, string outfitName, uint group, SimOutfit outfitToApplyTo)
        {
            SimDescription simDescription = actor.SimDescription;
            if (!simDescription.HasSpecialOutfit(specialOutfitKey))
            {
                SimOutfit resultOutfit;
                if (OutfitUtils.TryApplyUniformToOutfit(outfitToApplyTo, new SimOutfit(ResourceKey.CreateOutfitKey(outfitName, group)), simDescription, "EditSpecialOutfit", out resultOutfit))
                {
                    simDescription.AddSpecialOutfit(resultOutfit, specialOutfitKey);
                }
            }
            return EditSpecialOutfit(actor, localizationKey, specialOutfitKey);
        }

        public static bool EditSpecialOutfit(Sim actor, string localizationKey, string specialOutfitKey, string outfitName, ProductVersion productVersion)
        {
            return EditSpecialOutfit(actor, localizationKey, specialOutfitKey, outfitName, ResourceUtils.ProductVersionToGroupId(productVersion));
        }

        public static bool EditSpecialOutfit(Sim actor, string localizationKey, string specialOutfitKey, string outfitName, ProductVersion productVersion, SimOutfit outfitToApplyTo)
        {
            return EditSpecialOutfit(actor, localizationKey, specialOutfitKey, outfitName, ResourceUtils.ProductVersionToGroupId(productVersion), outfitToApplyTo);
        }

        static void OnPreLoad()
        {
            AfterschoolActivityOutfit.EditAfterschoolActivityOutfit.Singleton = new EditAfterschoolActivityOutfit.DefinitionModified();
            BachelorPartyOutfit.EditBachelorPartyOutfit.Singleton = new EditBachelorPartyOutfit.DefinitionModified();
            BeekeeperOutfit.EditBeekeeperOutfit.Singleton = new EditBeekeeperOutfit.DefinitionModified();
            ChemistryLabOutfit.EditChemistryLabOutfit.Singleton = new EditChemistryLabOutfit.DefinitionModified();
            GraduationOutfit.EditGraduationOutfit.Singleton = new EditGraduationOutfit.DefinitionModified();
            HighSchoolGraduationOutfit.EditHighSchoolGraduationOutfit.Singleton = new EditHighSchoolGraduationOutfit.DefinitionModified();
            MechanicalBullOutfit.EditMechanicalBullOutfit.Singleton = new EditMechanicalBullOutfit.DefinitionModified();
            MassageTableOutfit.EditMassageTableOutfit.Singleton = new EditMassageTableOutfit.DefinitionModified();
            SaunaOutfit.EditSaunaOutfit.Singleton = new EditSaunaOutfit.DefinitionModified();
            SingedOutfit.EditSingedOutfit.Singleton = new EditSingedOutfit.DefinitionModified();
            SkatingOutfit.EditSkatingOutfit.Singleton = new EditSkatingOutfit.DefinitionModified();
            TeppanyakiChefOutfit.EditChefOutfit.Singleton = new EditChefOutfit.DefinitionModified();
            TowelOutfit.EditTowelOutfit.Singleton = new EditTowelOutfit.DefinitionModified();
        }
    }
}
