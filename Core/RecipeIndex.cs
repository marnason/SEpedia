using System.Collections.Generic;
using VRage.Game;

namespace SEpedia.Core
{
    public sealed class RecipeIndex
    {
        private static readonly IReadOnlyList<RecipeDocument> EmptyRecipes = new List<RecipeDocument>().AsReadOnly();

        private readonly Dictionary<MyDefinitionId, RecipeDocument> byId;
        private readonly Dictionary<MyDefinitionId, IReadOnlyList<RecipeDocument>> producingByItem;
        private readonly Dictionary<MyDefinitionId, IReadOnlyList<RecipeDocument>> consumingByItem;
        private readonly Dictionary<MyDefinitionId, IReadOnlyList<RecipeDocument>> menuProducingByItem;
        private readonly Dictionary<MyDefinitionId, IReadOnlyList<RecipeDocument>> menuConsumingByItem;

        public int Count { get { return byId.Count; } }
        public int MenuCount { get; private set; }

        public RecipeIndex(IEnumerable<RecipeDocument> recipes)
        {
            byId = new Dictionary<MyDefinitionId, RecipeDocument>();

            var producing = new Dictionary<MyDefinitionId, List<RecipeDocument>>();
            var consuming = new Dictionary<MyDefinitionId, List<RecipeDocument>>();
            var menuProducing = new Dictionary<MyDefinitionId, List<RecipeDocument>>();
            var menuConsuming = new Dictionary<MyDefinitionId, List<RecipeDocument>>();

            foreach (RecipeDocument recipe in recipes)
            {
                if (!byId.ContainsKey(recipe.DefinitionId))
                    byId.Add(recipe.DefinitionId, recipe);

                AddRelations(producing, recipe.Results, recipe);
                AddRelations(consuming, recipe.Prerequisites, recipe);
                if (recipe.ProductionMenuReachable)
                {
                    MenuCount++;
                    AddRelations(menuProducing, recipe.Results, recipe);
                    AddRelations(menuConsuming, recipe.Prerequisites, recipe);
                }
            }

            producingByItem = Freeze(producing);
            consumingByItem = Freeze(consuming);
            menuProducingByItem = Freeze(menuProducing);
            menuConsumingByItem = Freeze(menuConsuming);
        }

        public bool TryGet(MyDefinitionId recipeId, out RecipeDocument recipe)
        {
            return byId.TryGetValue(recipeId, out recipe);
        }

        public IReadOnlyList<RecipeDocument> GetProducingRecipes(MyDefinitionId itemId)
        {
            IReadOnlyList<RecipeDocument> recipes;
            return producingByItem.TryGetValue(itemId, out recipes) ? recipes : EmptyRecipes;
        }

        public IReadOnlyList<RecipeDocument> GetConsumingRecipes(MyDefinitionId itemId)
        {
            IReadOnlyList<RecipeDocument> recipes;
            return consumingByItem.TryGetValue(itemId, out recipes) ? recipes : EmptyRecipes;
        }

        public IReadOnlyList<RecipeDocument> GetMenuProducingRecipes(MyDefinitionId itemId)
        {
            IReadOnlyList<RecipeDocument> recipes;
            return menuProducingByItem.TryGetValue(itemId, out recipes) ? recipes : EmptyRecipes;
        }

        public IReadOnlyList<RecipeDocument> GetMenuConsumingRecipes(MyDefinitionId itemId)
        {
            IReadOnlyList<RecipeDocument> recipes;
            return menuConsumingByItem.TryGetValue(itemId, out recipes) ? recipes : EmptyRecipes;
        }

        private static void AddRelations(
            IDictionary<MyDefinitionId, List<RecipeDocument>> target,
            IReadOnlyList<DefinitionAmount> items,
            RecipeDocument recipe)
        {
            for (int index = 0; index < items.Count; index++)
            {
                MyDefinitionId itemId = items[index].DefinitionId;
                List<RecipeDocument> recipes;

                if (!target.TryGetValue(itemId, out recipes))
                {
                    recipes = new List<RecipeDocument>();
                    target.Add(itemId, recipes);
                }

                if (!recipes.Contains(recipe))
                    recipes.Add(recipe);
            }
        }

        private static Dictionary<MyDefinitionId, IReadOnlyList<RecipeDocument>> Freeze(
            IDictionary<MyDefinitionId, List<RecipeDocument>> source)
        {
            var result = new Dictionary<MyDefinitionId, IReadOnlyList<RecipeDocument>>();

            foreach (KeyValuePair<MyDefinitionId, List<RecipeDocument>> pair in source)
                result.Add(pair.Key, pair.Value.AsReadOnly());

            return result;
        }
    }
}
