using System.Collections.Generic;
using VRage;
using VRage.Game;

namespace SEpedia.Core
{
    internal sealed class DefinitionAmount
    {
        public MyDefinitionId DefinitionId { get; private set; }
        public MyFixedPoint Amount { get; private set; }

        public DefinitionAmount(MyDefinitionId definitionId, MyFixedPoint amount)
        {
            DefinitionId = definitionId;
            Amount = amount;
        }
    }

    internal sealed class RecipeDocument
    {
        public MyDefinitionId DefinitionId { get; private set; }
        public float BaseProductionTimeSeconds { get; private set; }
        public bool IsAtomic { get; private set; }
        public IReadOnlyList<DefinitionAmount> Prerequisites { get; private set; }
        public IReadOnlyList<DefinitionAmount> Results { get; private set; }
        public bool IsProductionMenuReachable { get; private set; }
        public IReadOnlyList<MyDefinitionId> ProductionBlocks { get; private set; }

        public RecipeDocument(
            MyDefinitionId definitionId,
            float baseProductionTimeSeconds,
            bool isAtomic,
            IList<DefinitionAmount> prerequisites,
            IList<DefinitionAmount> results,
            IList<MyDefinitionId> productionBlocks)
        {
            DefinitionId = definitionId;
            BaseProductionTimeSeconds = baseProductionTimeSeconds;
            IsAtomic = isAtomic;
            Prerequisites = new List<DefinitionAmount>(prerequisites).AsReadOnly();
            Results = new List<DefinitionAmount>(results).AsReadOnly();
            ProductionBlocks = new List<MyDefinitionId>(productionBlocks).AsReadOnly();
            IsProductionMenuReachable = ProductionBlocks.Count > 0;
        }
    }
}
