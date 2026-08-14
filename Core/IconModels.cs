using System.Collections.Generic;

namespace SEpedia.Core
{
    internal enum IconResolutionKind
    {
        None = 0,
        PackagedAlias = 1,
        Unresolved = 2,
        LayerLimit = 3
    }

    internal sealed class DefinitionIconData
    {
        public IReadOnlyList<string> TexturePaths { get; private set; }
        public IReadOnlyList<string> MaterialIds { get; private set; }
        public IconResolutionKind Resolution { get; private set; }

        public bool IsRenderable
        {
            get
            {
                return Resolution == IconResolutionKind.PackagedAlias &&
                    TexturePaths.Count > 0 && TexturePaths.Count == MaterialIds.Count;
            }
        }

        public DefinitionIconData(
            IList<string> texturePaths,
            IList<string> materialIds,
            IconResolutionKind resolution)
        {
            TexturePaths = new List<string>(texturePaths).AsReadOnly();
            MaterialIds = new List<string>(materialIds).AsReadOnly();
            Resolution = resolution;
        }
    }

    internal sealed class DefinitionIconStats
    {
        public int DefinitionsWithIcons { get; private set; }
        public int RenderableDefinitions { get; private set; }
        public int UnresolvedDefinitions { get; private set; }
        public int LayerLimitDefinitions { get; private set; }

        public DefinitionIconStats(
            int definitionsWithIcons,
            int renderableDefinitions,
            int unresolvedDefinitions,
            int layerLimitDefinitions)
        {
            DefinitionsWithIcons = definitionsWithIcons;
            RenderableDefinitions = renderableDefinitions;
            UnresolvedDefinitions = unresolvedDefinitions;
            LayerLimitDefinitions = layerLimitDefinitions;
        }
    }
}
