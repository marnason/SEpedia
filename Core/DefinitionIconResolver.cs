using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Game;

namespace SEpedia.Core
{
    internal sealed class DefinitionIconResolver
    {
        public const int LayerLimit = 8;

        private readonly HashSet<string> packagedAliases;
        private int definitionsWithIcons;
        private int renderableDefinitions;
        private int unresolvedDefinitions;
        private int layerLimitDefinitions;

        public DefinitionIconResolver(
            MyDefinitionManager manager,
            DefinitionBuildDiagnostics diagnostics)
        {
            packagedAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IndexPackagedAliases(manager, diagnostics);
        }

        public DefinitionIconData Resolve(MyDefinitionBase definition, IList<string> texturePaths)
        {
            if (texturePaths == null || texturePaths.Count == 0)
                return new DefinitionIconData(
                    new List<string>(),
                    new List<string>(),
                    IconResolutionKind.None);

            definitionsWithIcons++;
            if (texturePaths.Count > LayerLimit)
            {
                layerLimitDefinitions++;
                return new DefinitionIconData(
                    texturePaths,
                    new List<string>(),
                    IconResolutionKind.LayerLimit);
            }

            var materialIds = new List<string>(texturePaths.Count);
            for (int index = 0; index < texturePaths.Count; index++)
            {
                string normalizedPath = NormalizeTexturePath(definition.Context, texturePaths[index]);
                if (packagedAliases.Contains(normalizedPath))
                {
                    materialIds.Add(normalizedPath);
                }
                else
                {
                    unresolvedDefinitions++;
                    return new DefinitionIconData(
                        texturePaths,
                        new List<string>(),
                        IconResolutionKind.Unresolved);
                }
            }

            renderableDefinitions++;
            return new DefinitionIconData(
                texturePaths,
                materialIds,
                IconResolutionKind.PackagedAlias);
        }

        public DefinitionIconStats GetStats()
        {
            return new DefinitionIconStats(
                definitionsWithIcons,
                renderableDefinitions,
                unresolvedDefinitions,
                layerLimitDefinitions);
        }

        private void IndexPackagedAliases(
            MyDefinitionManager manager,
            DefinitionBuildDiagnostics diagnostics)
        {
            try
            {
                foreach (MyTransparentMaterialDefinition material in manager.GetTransparentMaterialDefinitions())
                {
                    if (material == null || string.IsNullOrWhiteSpace(material.Id.SubtypeName))
                        continue;

                    if (string.IsNullOrWhiteSpace(material.Texture))
                        continue;

                    string materialId = NormalizePath(material.Id.SubtypeName);
                    string texturePath = NormalizeTexturePath(material.Context, material.Texture);
                    if (materialId.StartsWith("Textures\\GUI\\Icons\\", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(materialId, texturePath, StringComparison.OrdinalIgnoreCase))
                        packagedAliases.Add(materialId);
                }
            }
            catch (Exception exception)
            {
                diagnostics.Report(
                    "icon-alias-registry",
                    "Could not index packaged icon aliases",
                    exception);
            }
        }

        private static string NormalizeTexturePath(MyModContext context, string path)
        {
            string normalized = NormalizePath(path);
            string contextPath = context != null ? NormalizePath(context.ModPath).TrimEnd('\\') : string.Empty;
            if (contextPath.Length > 0 && normalized.StartsWith(contextPath + "\\", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(contextPath.Length + 1);
            return normalized.TrimStart('\\');
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Trim().Replace('/', '\\');
        }
    }
}
