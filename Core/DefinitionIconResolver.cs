using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Game;

namespace SEpedia.Core
{
    internal sealed class DefinitionIconResolver
    {
        public const int LayerLimit = 8;

        private readonly Dictionary<string, string> materialsByContextAndPath;
        private readonly HashSet<string> registeredMaterialIds;
        private int definitionsWithIcons;
        private int renderableDefinitions;
        private int pathAliasDefinitions;
        private int sameOriginDefinitions;
        private int mixedDefinitions;
        private int unresolvedDefinitions;
        private int layerLimitDefinitions;

        public DefinitionIconResolver(MyDefinitionManager manager, Action<string> logWarning)
        {
            materialsByContextAndPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            registeredMaterialIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IndexMaterials(manager, logWarning);
        }

        public DefinitionIconData Resolve(MyDefinitionBase definition, IList<string> texturePaths)
        {
            if (texturePaths == null || texturePaths.Count == 0)
                return new DefinitionIconData(new List<string>(), new List<string>());

            definitionsWithIcons++;
            if (texturePaths.Count > LayerLimit)
            {
                layerLimitDefinitions++;
                return new DefinitionIconData(texturePaths, new List<string>());
            }

            var materialIds = new List<string>(texturePaths.Count);
            bool usedSameOrigin = false;
            bool usedPathAlias = false;
            for (int index = 0; index < texturePaths.Count; index++)
            {
                string normalizedPath = NormalizeTexturePath(definition.Context, texturePaths[index]);
                string materialId;
                if (materialsByContextAndPath.TryGetValue(
                    GetContextKey(definition.Context) + "|" + normalizedPath,
                    out materialId))
                {
                    materialIds.Add(materialId);
                    usedSameOrigin = true;
                }
                else if (registeredMaterialIds.Contains(normalizedPath))
                {
                    materialIds.Add(normalizedPath);
                    usedPathAlias = true;
                }
                else
                {
                    unresolvedDefinitions++;
                    return new DefinitionIconData(texturePaths, new List<string>());
                }
            }

            renderableDefinitions++;
            if (usedSameOrigin && usedPathAlias)
                mixedDefinitions++;
            else if (usedSameOrigin)
                sameOriginDefinitions++;
            else
                pathAliasDefinitions++;
            return new DefinitionIconData(texturePaths, materialIds);
        }

        public DefinitionIconStats GetStats()
        {
            return new DefinitionIconStats(
                definitionsWithIcons,
                renderableDefinitions,
                pathAliasDefinitions,
                sameOriginDefinitions,
                mixedDefinitions,
                unresolvedDefinitions,
                layerLimitDefinitions);
        }

        private void IndexMaterials(MyDefinitionManager manager, Action<string> logWarning)
        {
            try
            {
                foreach (MyTransparentMaterialDefinition material in manager.GetTransparentMaterialDefinitions())
                {
                    if (material == null || string.IsNullOrWhiteSpace(material.Id.SubtypeName))
                        continue;

                    string materialId = material.Id.SubtypeName.Trim();
                    registeredMaterialIds.Add(materialId);
                    if (string.IsNullOrWhiteSpace(material.Texture))
                        continue;

                    string key = GetContextKey(material.Context) + "|" +
                        NormalizeTexturePath(material.Context, material.Texture);
                    if (!materialsByContextAndPath.ContainsKey(key))
                        materialsByContextAndPath.Add(key, materialId);
                }
            }
            catch (Exception exception)
            {
                if (logWarning != null)
                    logWarning("Could not index registered icon materials: " + exception.Message);
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

        private static string GetContextKey(MyModContext context)
        {
            if (context == null)
                return "unknown";
            if (context.IsBaseGame)
                return "base-game";
            if (!string.IsNullOrWhiteSpace(context.ModId))
                return "published:" + (context.ModServiceName ?? string.Empty) + ":" + context.ModId;
            if (!string.IsNullOrWhiteSpace(context.ModPath))
                return "local-path:" + NormalizePath(context.ModPath).TrimEnd('\\');
            if (!string.IsNullOrWhiteSpace(context.ModName))
                return "local-name:" + context.ModName;
            return "unknown";
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Trim().Replace('/', '\\');
        }
    }
}
