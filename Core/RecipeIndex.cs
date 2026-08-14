using System.Collections.Generic;
using VRage.Game;

namespace SEpedia.Core
{
    internal sealed class RecipeIndex
    {
        #region State

        private static readonly IReadOnlyList<RecipeDocument> EmptyRecipes = new List<RecipeDocument>().AsReadOnly();

        private readonly Dictionary<MyDefinitionId, IReadOnlyList<RecipeDocument>> menuProducingByItem;
        private readonly Dictionary<MyDefinitionId, IReadOnlyList<RecipeDocument>> menuConsumingByItem;

        public int Count { get; private set; }
        public int MenuCount { get; private set; }

        #endregion

        #region Index Construction

        public RecipeIndex(IEnumerable<RecipeDocument> recipes)
        {
            var menuProducing = new Dictionary<MyDefinitionId, List<RecipeDocument>>();
            var menuConsuming = new Dictionary<MyDefinitionId, List<RecipeDocument>>();

            foreach (RecipeDocument recipe in recipes)
            {
                Count++;
                if (recipe.IsProductionMenuReachable)
                {
                    MenuCount++;
                    AddRelations(menuProducing, recipe.Results, recipe);
                    AddRelations(menuConsuming, recipe.Prerequisites, recipe);
                }
            }

            menuProducingByItem = Freeze(menuProducing);
            menuConsumingByItem = Freeze(menuConsuming);
        }

        #endregion

        #region Relationship Queries

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

        #endregion

        #region Index Helpers

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

        #endregion
    }
}
