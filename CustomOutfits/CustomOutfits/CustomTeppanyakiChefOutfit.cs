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
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.CookingObjects;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.Store.Objects;
using Sims3.UI;
using Sims3.UI.Hud;
using Tuning = Sims3.Gameplay.Destrospean.CustomOutfits;

namespace Destrospean
{
    public class CustomTeppanyakiChefOutfit
    {
        public static readonly string kChefSpecialOutfitKey = "TeppanyakiChef";

        [Tunable]
        protected static bool kInstantiator;

        [PersistableStatic(true)]
        static List<ulong> sChefOutfitDisabledList;

        static EventListener sSimDestroyedListener, sSimSelectedListener;

        static CustomTeppanyakiChefOutfit()
        {
            kInstantiator = false;
            sChefOutfitDisabledList = new List<ulong>();
            sSimDestroyedListener = null;
            sSimSelectedListener = null;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnObjectPlacedInLotEventHandler += OnObjectPlacedInLot;
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            World.sOnWorldQuitEventHandler += OnWorldQuit;
        }

        public class EditChefOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomTeppanyakiChefOutfit/EditChefOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditChefOutfit>
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
                    return ((target is Sim && actor == target && Tuning.kShowSimMenu) || (!(target is Sim) && Tuning.kShowObjectMenu)) && actor.SimDescription.TeenOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public override bool Run()
            {
                return Common.EditSpecialOutfit(Actor, sLocalizationKey, kChefSpecialOutfitKey, GetChefOutfitName(Actor), ProductVersion.BaseGame);
            }
        }

        public class JuggleEggTrick : TeppanyakiGrill.JuggleEggTrick
        {
            public class DefinitionModified : InteractionDefinition<Sim, TeppanyakiGrill, JuggleEggTrick>
            {
                Definition mDefinitionBase = new Definition();

                public override string GetInteractionName(Sim actor, TeppanyakiGrill target, InteractionObjectPair interaction)
                {
                    return mDefinitionBase.GetInteractionName(actor, target, interaction);
                }

                public override bool Test(Sim actor, TeppanyakiGrill target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return mDefinitionBase.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
                }
            }

            public override void Cleanup()
            {
                InteractionCleanup(Actor, this, Target);
            }

            public override bool Run()
            {
                if (!Actor.RouteToSlot(Target, Slot.RoutingSlot_0))
                {
                    return false;
                }
                Target.mCookingSim = Actor;
                if (!Actor.SkillManager.HasElement(SkillNames.Cooking))
                {
                    Actor.SkillManager.AddElement(SkillNames.Cooking);
                }
                StandardEntry();
                BeginCommodityUpdates();
                if (GetChefOutfitEnabled(Actor.SimDescription))
                {
                    ChangeSimToChefOutfit(Actor, Target);
                }
                EnterStateMachine("teppanyakigrill_store", "Enter", "x", "Grill");
                bool eggTrickDone = Target.DoEggTrick(this, "EggLoopsPractice", "EggSuccessPractice");
                if (eggTrickDone)
                {
                    AnimateSim("Exit");
                }
                EndCommodityUpdates(eggTrickDone);
                StandardExit();
                return eggTrickDone;
            }
        }

        public class OnionVolcano : TeppanyakiGrill.OnionVolcano
        {
            public class DefinitionModified : InteractionDefinition<Sim, TeppanyakiGrill, OnionVolcano>
            {
                Definition mDefinitionBase = new Definition();

                public override string GetInteractionName(Sim actor, TeppanyakiGrill target, InteractionObjectPair interaction)
                {
                    return mDefinitionBase.GetInteractionName(actor, target, interaction);
                }

                public override bool Test(Sim actor, TeppanyakiGrill target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return mDefinitionBase.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
                }
            }

            public override void Cleanup()
            {
                InteractionCleanup(Actor, this, Target);
            }

            public override bool Run()
            {
                if (!Actor.RouteToSlot(Target, Slot.RoutingSlot_0))
                {
                    return false;
                }
                Target.mCookingSim = Actor;
                if (!Actor.SkillManager.HasElement(SkillNames.Cooking))
                {
                    Actor.SkillManager.AddElement(SkillNames.Cooking);
                }
                StandardEntry();
                BeginCommodityUpdates();
                Actor.ClearExitReasons();
                if (GetChefOutfitEnabled(Actor.SimDescription))
                {
                    ChangeSimToChefOutfit(Actor, Target);
                }
                EnterStateMachine("teppanyakigrill_store", "Enter", "x", "Grill");
                bool onionVolcanoDone = Target.DoOnionVolcano(this, "OnionStartPractice");
                if (onionVolcanoDone)
                {
                    AnimateSim("Exit");
                }
                EndCommodityUpdates(onionVolcanoDone);
                StandardExit();
                return onionVolcanoDone;
            }
        }

        public class ResetChefOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomTeppanyakiChefOutfit/ResetChefOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ResetChefOutfit>
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
                    return ((target is Sim && actor == target && Tuning.kShowSimMenu) || (!(target is Sim) && Tuning.kShowObjectMenu)) && actor.SimDescription.TeenOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous && actor.SimDescription.HasSpecialOutfit(kChefSpecialOutfitKey);
                }
            }

            public override bool Run()
            {
                Actor.SimDescription.RemoveSpecialOutfit(kChefSpecialOutfitKey);
                Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "Feedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                return true;
            }
        }

        public class TGCook : TeppanyakiGrill.TGCook
        {
            public class DefinitionModified : FoodMenuInteractionDefinition<TeppanyakiGrill, TGCook>
            {
                public TeppanyakiGrill.TGFoodInfo mTGFoodInfo;

                public DefinitionModified()
                {
                }

                public DefinitionModified(string menuText, Recipe recipe, string[] menuPath, GameObject objectClickedOn, Recipe.MealDestination destination, Recipe.MealQuantity quantity, Recipe.MealRepetition repetition, bool bWasHaveSomething, int cost)
                    : base(menuText, recipe, menuPath, objectClickedOn, destination, quantity, repetition, bWasHaveSomething, cost)
                {
                }

                public override void AddInteractions(InteractionObjectPair interaction, Sim actor, TeppanyakiGrill target, List<InteractionObjectPair> results)
                {
                    Recipe.MealTime currentMealTime = Food.GetCurrentMealTime();
                    string[] menuPath =
                    {
                        TeppanyakiGrill.LocalizeString("Serve", new object[0]) + " " + Food.GetMealTimeString(currentMealTime) + Localization.Ellipsis
                    };
                    foreach (Recipe current in Recipe.Recipes)
                    {
                        if (current.SpecificNameKey.Contains("TGCook"))
                        {
                            Recipe.CanMakeFoodTestResult canMakeFoodTestResult = Food.CanMake(current, true, false, currentMealTime, Recipe.MealRepetition.MakeOne, target.LotCurrent, actor, Recipe.MealQuantity.Group, current.CalculateCost(actor), target);
                            if (canMakeFoodTestResult == Recipe.CanMakeFoodTestResult.Fail_NotEnoughMoney)
                            {
                                bool needIngredients;
                                canMakeFoodTestResult = target.CanMakeRecipe(current, true, false, currentMealTime, Recipe.MealRepetition.MakeOne, target.LotCurrent, actor, Recipe.MealQuantity.Group, current.CalculateCost(actor), target, out needIngredients);
                                if (!needIngredients)
                                {
                                    canMakeFoodTestResult = Recipe.CanMakeFoodTestResult.Pass;
                                }
                            }
                            if (canMakeFoodTestResult == Recipe.CanMakeFoodTestResult.Pass)
                            {
                                DefinitionModified definition = new DefinitionModified(current.GenericName, current, menuPath, target, Recipe.MealDestination.SurfaceAndCallToMeal, Recipe.MealQuantity.Group, Recipe.MealRepetition.MakeOne, false, current.CalculateCost(actor));
                                InteractionObjectPair item = new InteractionObjectPair(definition, interaction.Target);
                                results.Add(item);
                            }
                        }
                    }
                }

                public override FoodMenuInteractionDefinition<TeppanyakiGrill, TGCook> Create(string menuText, Recipe recipe, string[] menuPath, GameObject objectClickedOn, Recipe.MealDestination destination, Recipe.MealQuantity quantity, Recipe.MealRepetition repetition, bool bWasHaveSomething, int cost)
                {
                    return new DefinitionModified(menuText, recipe, menuPath, objectClickedOn, destination, quantity, repetition, bWasHaveSomething, cost);
                }

                public override bool SpecificTest(Sim actor, TeppanyakiGrill target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    mTGFoodInfo = null;
                    Cooking skill = actor.SkillManager.GetSkill<Cooking>(SkillNames.Cooking);
                    bool needIngredients;
                    if (isAutonomous)
                    {
                        int recipeIndex = RandomUtil.GetInt(7);
                        int totalRecipesChecked = 0;
                        Recipe recipe;
                        TeppanyakiGrill.TGFoodInfo tGFoodInfo;
                        while (true)
                        {
                            recipe = null;
                            tGFoodInfo = TeppanyakiGrill.TGFoodInfos[recipeIndex];
                            Recipe.NameToRecipeHash.TryGetValue(tGFoodInfo.mName, out recipe);
                            if (recipe != null && target.CanMakeRecipe(recipe, true, false, Food.GetCurrentMealTime(), Recipe.MealRepetition.MakeOne, target.LotCurrent, actor, Recipe.MealQuantity.Group, recipe.CalculateCost(actor), target, out needIngredients) == Recipe.CanMakeFoodTestResult.Pass && (recipe.CookingSkillLevelRequired == 0 || (skill != null && skill.SkillLevel >= recipe.CookingSkillLevelRequired)))
                            {
                                break;
                            }
                            if (++totalRecipesChecked >= 8)
                            {
                                return false;
                            }
                            if (++recipeIndex >= 8)
                            {
                                recipeIndex = 0;
                            }
                        }
                        target.mCurrentRecipe = recipe;
                        ChosenRecipe = recipe;
                        mTGFoodInfo = tGFoodInfo;
                        return true;
                    }
                    target.mCurrentRecipe = ChosenRecipe;
                    mTGFoodInfo = null;
                    foreach (TeppanyakiGrill.TGFoodInfo current in TeppanyakiGrill.TGFoodInfos)
                    {
                        if (ChosenRecipe.SpecificNameKey.Substring(ChosenRecipe.SpecificNameKey.IndexOf(":") + 1) == current.mName)
                        {
                            mTGFoodInfo = current;
                            break;
                        }
                    }
                    if (mTGFoodInfo == null)
                    {
                        return false;
                    }
                    if (ChosenRecipe.CookingSkillLevelRequired != 0 && (skill == null || skill.SkillLevel < ChosenRecipe.CookingSkillLevelRequired))
                    {
                        greyedOutTooltipCallback = () => TeppanyakiGrill.LocalizeString("RequiredCookingSkill", new object[0]) + "\n" + ChosenRecipe.CookingSkillLevelRequired;
                        return false;
                    }
                    Recipe.CanMakeFoodTestResult canMakeFoodTestResult = target.CanMakeRecipe(ChosenRecipe, true, false, Food.GetCurrentMealTime(), Recipe.MealRepetition.MakeOne, target.LotCurrent, actor, Recipe.MealQuantity.Group, ChosenRecipe.CalculateCost(actor), target, out needIngredients);
                    if (needIngredients)
                    {
                        if (canMakeFoodTestResult != Recipe.CanMakeFoodTestResult.Fail_NotEnoughMoney)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (target.CheckForMotiveFailure(actor))
                        {
                            greyedOutTooltipCallback = () => Localization.LocalizeString("Gameplay/Actors/Sim:MoodFailureString", new object[]
                            {
                                actor
                            });
                            return false;
                        }
                        if (canMakeFoodTestResult == Recipe.CanMakeFoodTestResult.Fail_NotEnoughMoney)
                        {
                            canMakeFoodTestResult = Recipe.CanMakeFoodTestResult.Pass;
                        }
                    }
                    return Food.PrepareTestResultCheckAndGrayedOutPieMenuSet(actor, ChosenRecipe, canMakeFoodTestResult, ref greyedOutTooltipCallback);
                }
            }

            public override void Cleanup()
            {
                InteractionCleanup(Actor, this, Target);
            }

            public override string GetInteractionName()
            {
                string interactionSubstring = TeppanyakiGrill.LocalizeString("Serve", new object[0]);
                DefinitionModified definition = InteractionDefinition as DefinitionModified;
                if (definition.mTGFoodInfo != null)
                {
                    interactionSubstring += " " + Localization.LocalizeString("Gameplay/Excel/RecipeMasterList/Data:" + definition.mTGFoodInfo.mName, new object[0]);
                }
                return interactionSubstring;
            }

            public new void OnAnimationEvent(StateMachineClient smc, IEvent evt)
            {
                if (smc != null)
                {
                    uint eventId = evt.EventId;
                    switch (eventId)
                    {
                        case 101u:
                            Target.SetGrillGeometryState("stage1");
                            return;
                        case 102u:
                            Target.SetGrillGeometryState("stage2");
                            return;
                        case 103u:
                            Target.SetGrillGeometryState("stage3");
                            return;
                        default:
                            {
                                if (eventId != 200u)
                                {
                                    return;
                                }
                                DefinitionModified definition = InteractionDefinition as DefinitionModified;
                                Target.GetRecipeFood(Actor, definition.mTGFoodInfo);
                                break;
                            }
                    }
                }
            }

            public override bool Run()
            {
                StandardEntry();
                Target.mTrickFailed = false;
                Actor.ClearExitReasons();
                DefinitionModified definition = InteractionDefinition as DefinitionModified;
                mCookRecipe = definition.ChosenRecipe;
                FoodTray foodTray = Target.CreateFoodTray(mCookRecipe);
                if (foodTray == null || !Target.FindAndGetIngredients(Actor, this, mCookRecipe, foodTray) || definition.mTGFoodInfo == null)
                {
                    StandardExit();
                    return false;
                }
                TeppanyakiGrill.TGFoodInfo mTGFoodInfo = definition.mTGFoodInfo;
                if (!Actor.RouteToSlot(Target, Slot.RoutingSlot_0) || Actor.ExitReason != ExitReason.None)
                {
                    StandardExit();
                    return false;
                }
                Target.mCookingSim = Actor;
                if (GetChefOutfitEnabled(Actor.SimDescription))
                {
                    ChangeSimToChefOutfit(Actor, Target);
                }
                Target.mFailedToCook = false;
                if (!Actor.SkillManager.HasElement(SkillNames.Cooking))
                {
                    Actor.SkillManager.AddElement(SkillNames.Cooking);
                }
                BeginCommodityUpdates();
                EnterStateMachine("teppanyakigrill_store", "Enter", "x", "Grill");
                FoodProp foodProp = FoodProp.Create("accessoryBowl");
                if (foodProp != null)
                {
                    mCurrentStateMachine.SetPropActor("bowl", foodProp.ObjectId);
                    foodProp.ActorsUsingMe.Add(Actor);
                }
                SetParameter("SkillLevel", Target.GetCookingSkillParameter(Actor));
                AddOneShotScriptEventHandler(101u, new SacsEventHandler(OnAnimationEvent));
                AddOneShotScriptEventHandler(102u, new SacsEventHandler(OnAnimationEvent));
                AddOneShotScriptEventHandler(103u, new SacsEventHandler(OnAnimationEvent));
                AddOneShotScriptEventHandler(200u, new SacsEventHandler(OnAnimationEvent));
                mCurrentStateMachine.SetPropActor("FoodTray", foodTray.ObjectId);
                if (Actor.CarryStateMachine == null)
                {
                    mCurrentStateMachine.RequestState(true, "x", "GetTray");
                }
                else
                {
                    CarrySystem.ExitCarry(Actor);
                }
                mCurrentStateMachine.RequestState(true, "x", "ChopLoop");
                Target.StartSteamVFX();
                Target.mCookingTime = TeppanyakiGrill.kChoppingTime;
                DoLoop(ExitReason.Default, new InsideLoopFunction(ChopLoopDelegate), mCurrentStateMachine);
                Target.mCookingTime = TeppanyakiGrill.kCookingTime;
                Target.RemoveFoodItems(Actor);
                if ((Actor.ExitReason & ExitReason.StageComplete) == ExitReason.None)
                {
                    EarlyExit(mTGFoodInfo);
                    return false;
                }
                Target.mCurrentRecipe = mCookRecipe;
                Target.CheckBurn(mCurrentStateMachine);
                IFoodContainer foodContainer = Target.mCurrentRecipe.CreateFinishedFood(Recipe.MealQuantity.Group, Target.GetFoodQuality(Actor));
                if (foodContainer != null)
                {
                    FoodProp containedFood = (foodContainer as ServingContainer).GetContainedFood();
                    if (containedFood != null)
                    {
                        Target.mCookedFood = containedFood;
                    }
                }
                if (!Target.mFailedToCook)
                {
                    if (RandomUtil.RandomChance(TeppanyakiGrill.kPerformTrickWhileCookingChance))
                    {
                        if (!((!RandomUtil.RandomChance(50f)) ? Target.DoEggTrick(this, "EggLoops", "EggSuccess") : Target.DoOnionVolcano(this, "OnionStart")))
                        {
                            Target.mTrickFailed = true;
                        }
                        Target.SetGrillGeometryState("stage2");
                    }
                    else
                    {
                        TeppanyakiGrill.SpectatorsReactToTrick(Target);
                    }
                    Actor.ClearExitReasons();
                    AnimateSim("cookLoop");
                    DoLoop(ExitReason.Default, new InsideLoopFunction(CookLoopDelegate), mCurrentStateMachine);
                    AddCookingBuffs(mTGFoodInfo);
                }
                if ((Actor.ExitReason & ExitReason.StageComplete) == ExitReason.None)
                {
                    EarlyExit(mTGFoodInfo);
                    Target.SetGrillGeometryState("default");
                    return false;
                }
                AnimateSim("Exit");
                Target.SetDirty(true, Actor);
                if (Target.mGotRecipeFood)
                {
                    if (Actor.OccultManager.HasOccultType(OccultTypes.Vampire))
                    {
                        Actor.Motives.SetMax(CommodityKind.Hunger);
                    }
                    else
                    {
                        List<Sim> allActors = Actor.LotCurrent.GetAllActors();
                        allActors.Remove(Actor);
                        ServingContainerGroup.CallToMeal.Definition callToMealDefinition = new ServingContainerGroup.CallToMeal.Definition(allActors, string.Empty, new string[0]);
                        InteractionInstance instance = callToMealDefinition.CreateInstance(Target.mFoodContainer, Actor, new InteractionPriority(InteractionPriorityLevel.High), false, true);
                        Actor.InteractionQueue.PushAsContinuation(this, instance, false, true);
                    }
                    if (Target.mDoneEatingEventListener == null)
                    {
                        Target.mDoneEatingEventListener = EventTracker.AddListener(EventTypeId.kAteMeal, new ProcessEventDelegate(Target.OnDoneEating), null, null);
                    }
                    EndCommodityUpdates(true);
                    StandardExit();
                    ServingContainer servingContainer = Target.mFoodContainer as ServingContainer;
                    if (servingContainer != null)
                    {
                        servingContainer.StartSmokeSteamVFX();
                        if (!servingContainer.IsActorUsingMe(Actor))
                        {
                            servingContainer.AddToUseList(Actor);
                        }
                        EventTracker.SendEvent(EventTypeId.kCookedMeal, Actor, servingContainer);
                    }
                    if (Target.mFoodBurned)
                    {
                        Actor.InteractionQueue.PushAsContinuation(ServingContainer.ServingContainer_CleanUp.Singleton, Target.mFoodContainer, false);
                        EventTracker.SendEvent(EventTypeId.kBurntMeal, Actor, servingContainer);
                    }
                    return true;
                }
                Target.mFailedToCook = true;
                EndCommodityUpdates(true);
                StandardExit();
                return false;
            }
        }

        public class ToggleChefOutfit : ImmediateInteraction<Sim, GameObject>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = "CustomTeppanyakiChefOutfit/ToggleChefOutfit/";

            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ToggleChefOutfit>
            {
                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair interaction)
                {
                    if (GetChefOutfitEnabled(actor.SimDescription))
                    {
                        return Common.Localize(actor.IsFemale, sLocalizationKey + "DisableInteractionName");
                    }
                    return Common.Localize(actor.IsFemale, sLocalizationKey + "EnableInteractionName");
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
                    return ((target is Sim && actor == target && Tuning.kShowSimMenu) || (!(target is Sim) && Tuning.kShowObjectMenu)) && actor.SimDescription.TeenOrAbove && actor.SimDescription.IsHuman && !actor.SimDescription.IsRobot && !isAutonomous;
                }
            }

            public override bool Run()
            {
                if (GetChefOutfitEnabled(Actor.SimDescription))
                {
                    DisableChefOutfit(Actor.SimDescription);
                    Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "DisabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                else
                {
                    EnableChefOutfit(Actor.SimDescription);
                    Common.Notify(Common.Localize(Actor.IsFemale, sLocalizationKey + "EnabledFeedback", Actor.Name), Actor.SimDescription, StyledNotification.NotificationStyle.kSystemMessage);
                }
                return true;
            }
        }

        static void AddInteractions(GameObject gameObject)
        {
            if (gameObject != null && !gameObject.Interactions.Exists(interaction => interaction.InteractionDefinition.GetType() == EditChefOutfit.Singleton.GetType()))
            {
                gameObject.AddInteraction(EditChefOutfit.Singleton);
                gameObject.AddInteraction(ResetChefOutfit.Singleton);
                gameObject.AddInteraction(ToggleChefOutfit.Singleton);
            }
        }

        public static void ChangeSimToChefOutfit(Sim actor, TeppanyakiGrill target)
        {
            if (target.mBackupSO != null || actor.CurrentOutfitCategory == OutfitCategories.Singed || actor.Service is GrimReaper || actor.SimDescription.TeenOrBelow)
            {
                return;
            }
            SimOutfit simOutfit;
            SimDescription simDescription = actor.SimDescription;
            if (simDescription.HasSpecialOutfit(kChefSpecialOutfitKey))
            {
                simDescription.AddOutfit(simDescription.GetSpecialOutfit(kChefSpecialOutfitKey), OutfitCategories.Career);
                simOutfit = simDescription.GetOutfit(OutfitCategories.Career, simDescription.GetOutfitCount(OutfitCategories.Career) - 1);
            }
            else
            {
                simOutfit = OutfitUtils.CreateOutfitForSim(simDescription, ResourceKey.CreateOutfitKeyFromProductVersion(GetChefOutfitName(actor), ProductVersion.BaseGame), OutfitCategories.Career, true);
            }
            if (!(simOutfit == null || actor.CurrentOutfit.Key == simOutfit.Key))
            {
                target.mBackupSO = actor.CurrentOutfit;
                target.mBackupOC = actor.CurrentOutfitCategory;
                actor.SwitchToOutfitWithSpin(simOutfit.Key);
            }
        }

        public static void ChangeSimToPreviousOutfit(Sim actor, TeppanyakiGrill target)
        {
            if (target.mBackupSO == null || actor.CurrentOutfitCategory == OutfitCategories.Singed)
            {
                return;
            }
            if (actor.CurrentOutfitCategory == OutfitCategories.Career)
            {
                actor.SimDescription.RemoveOutfit(OutfitCategories.Career, actor.CurrentOutfitIndex, true);
                if (actor.SimDescription.HasOutfit(target.mBackupOC, target.mBackupSO.Key) > -1)
                {
                    actor.SwitchToOutfitWithSpin(target.mBackupSO.Key);
                    return;
                }
            }
            actor.SwitchToOutfitWithoutSpin(Sim.ClothesChangeReason.Force, OutfitCategories.Everyday);
            target.mBackupSO = null;
        }

        static void DisableChefOutfit(SimDescription simDescription)
        {
            if (GetChefOutfitEnabled(simDescription))
            {
                sChefOutfitDisabledList.Add(simDescription.SimDescriptionId);
            }
        }

        static void EnableChefOutfit(SimDescription simDescription)
        {
            if (!GetChefOutfitEnabled(simDescription))
            {
                sChefOutfitDisabledList.Remove(simDescription.SimDescriptionId);
            }
        }

        static bool GetChefOutfitEnabled(SimDescription simDescription)
        {
            return !sChefOutfitDisabledList.Contains(simDescription.SimDescriptionId);
        }

        public static string GetChefOutfitName(Sim actor)
        {
            return "career_execchef_" + (actor.IsMale ? "male" : "female") + (actor.SimDescription.Elder ? "elder" : "");
        }

        static void Init()
        {
            UpdateListeners();
        }

        public static void InteractionCleanup(Sim actor, InteractionInstance interactionInstance, TeppanyakiGrill target)
        {
            if (target.mCookingSim != null && target.mCleanupAfterLoad)
            {
                return;
            }
            if (actor == null || actor != target.mCookingSim)
            {
                return;
            }
            InteractionInstance headInteraction = actor.InteractionQueue.GetHeadInteraction();
            if (!(headInteraction == null || headInteraction == interactionInstance || !interactionInstance.Cancelled || actor.CurrentInteraction == null))
            {
                if (actor.CurrentInteraction == interactionInstance)
                {
                    target.SetGrillGeometryState("default");
                }
                return;
            }
            foreach (IGameObject current in target.GetContainedObjectList<IGameObject>(target.GetContainmentSlots()))
            {
                current.Destroy();
            }
            target.SetGrillGeometryState("default");
            target.ResetParentingHierarchy(true);
            if (target.mBackupSO != null)
            {
                if (headInteraction == null || headInteraction == actor.CurrentInteraction)
                {
                    ChangeSimToPreviousOutfit(actor, target);
                }
                else
                {
                    if (headInteraction.Target == target)
                    {
                        if (!(headInteraction is TGCook || headInteraction is JuggleEggTrick || headInteraction is OnionVolcano))
                        {
                            ChangeSimToPreviousOutfit(actor, target);
                        }
                    }
                    else
                    {
                        ChangeSimToPreviousOutfit(actor, target);
                    }
                }
            }
            target.StopSteamVFX();
            target.mCookingSim = null;
        }

        static void OnObjectPlacedInLot(object sender, EventArgs e)
        {
            World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs = e as World.OnObjectPlacedInLotEventArgs;
            if (onObjectPlacedInLotEventArgs != null)
            {
                AddInteractions(GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId) as TeppanyakiGrill);
            }
        }

        static void OnPreLoad()
        {
            TeppanyakiGrill.JuggleEggTrick.Singleton = new JuggleEggTrick.DefinitionModified();
            TeppanyakiGrill.OnionVolcano.Singleton = new OnionVolcano.DefinitionModified();
            TeppanyakiGrill.TGCook.Singleton = new TGCook.DefinitionModified();
            Common.CopyTuning(typeof(TeppanyakiGrill), typeof(TeppanyakiGrill.JuggleEggTrick.Definition), typeof(JuggleEggTrick.DefinitionModified));
            Common.CopyTuning(typeof(TeppanyakiGrill), typeof(TeppanyakiGrill.OnionVolcano.Definition), typeof(OnionVolcano.DefinitionModified));
            Common.CopyTuning(typeof(TeppanyakiGrill), typeof(TeppanyakiGrill.TGCook.Definition), typeof(TGCook.DefinitionModified));
        }

        static ListenerAction OnSimDestroyed(Event e)
        {
            try
            {
                Sim sim = e.TargetObject as Sim;
                if (sim != null)
                {
                    EnableChefOutfit(sim.SimDescription);
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
            new List<TeppanyakiGrill>(Sims3.Gameplay.Queries.GetObjects<TeppanyakiGrill>()).ForEach(AddInteractions);
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