using System;
using System.Text;
using System.Xml.Serialization;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using Sandbox.ModAPI;
using SEpedia.Core;

namespace SEpedia.UI
{
    public sealed class BindingConfigController
    {
        private const string ConfigFileName = "SEpediaBindings.xml";
        private const string BindGroupName = "SEpedia";
        private const string ToggleBindName = "ToggleEncyclopedia";

        private readonly IBindGroup bindGroup;
        private readonly IBind toggleBind;
        private readonly RebindPage rebindPage;
        private string savedSignature;
        private bool closed;

        public event Action ToggleRequested;

        public BindingConfigController()
        {
            BindGroupInitializer defaults = CreateDefaults();
            bindGroup = BindManager.GetOrCreateGroup(BindGroupName);
            if (bindGroup == null)
                throw new InvalidOperationException("Rich HUD did not create the SEpedia bind group.");

            if (!bindGroup.DoesBindExist(ToggleBindName))
                bindGroup.RegisterBinds(defaults);

            LoadOrUseDefaults(defaults.GetBindDefinitions());

            toggleBind = bindGroup.GetBind(ToggleBindName);
            if (toggleBind == null)
                throw new InvalidOperationException("Rich HUD did not register ToggleEncyclopedia.");

            toggleBind.NewPressed += OnTogglePressed;

            rebindPage = new RebindPage
            {
                Name = "Controls",
                Enabled = true
            };
            rebindPage.Add(bindGroup, defaults.GetBindDefinitions());
            RichHudTerminal.Root.AddRange(new IModRootMember[] { rebindPage });

            savedSignature = GetSignature(bindGroup.GetBindDefinitions());
        }

        public void PollForChanges()
        {
            if (closed)
                return;

            BindDefinition[] current = bindGroup.GetBindDefinitions();
            string signature = GetSignature(current);
            if (!string.Equals(signature, savedSignature, StringComparison.Ordinal))
                Save(current, signature);
        }

        public void Save()
        {
            if (!closed)
            {
                BindDefinition[] current = bindGroup.GetBindDefinitions();
                Save(current, GetSignature(current));
            }
        }

        public void Close()
        {
            Close(true);
        }

        public void Close(bool save)
        {
            if (closed)
                return;

            if (save)
                Save();

            try
            {
                toggleBind.NewPressed -= OnTogglePressed;
            }
            catch (Exception exception)
            {
                SEpediaLog.Warning("Could not release the Rich HUD toggle subscription: " + exception.Message);
            }

            ToggleRequested = null;
            closed = true;
        }

        private static BindGroupInitializer CreateDefaults()
        {
            var defaults = new BindGroupInitializer();
            defaults.Add(ToggleBindName, RichHudControls.Control, RichHudControls.F1);
            return defaults;
        }

        private void LoadOrUseDefaults(BindDefinition[] defaults)
        {
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(ConfigFileName, typeof(BindingConfigController)))
                {
                    bindGroup.TryLoadBindData(defaults);
                    return;
                }

                BindingConfiguration configuration;
                using (var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(ConfigFileName, typeof(BindingConfigController)))
                    configuration = MyAPIGateway.Utilities.SerializeFromXML<BindingConfiguration>(reader.ReadToEnd());

                if (configuration == null || configuration.Binds == null ||
                    !bindGroup.TryLoadBindData(configuration.Binds))
                {
                    throw new InvalidOperationException("The saved bind definitions were invalid.");
                }
            }
            catch (Exception exception)
            {
                SEpediaLog.Warning("Could not load bind configuration; restored Ctrl+F1. " + exception.Message);
                bindGroup.TryLoadBindData(defaults);
                Save(defaults, GetSignature(defaults));
            }
        }

        private void Save(BindDefinition[] definitions, string signature)
        {
            try
            {
                var configuration = new BindingConfiguration { Binds = definitions };
                string xml = MyAPIGateway.Utilities.SerializeToXML(configuration);
                using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(ConfigFileName, typeof(BindingConfigController)))
                    writer.Write(xml);

                savedSignature = signature;
            }
            catch (Exception exception)
            {
                SEpediaLog.Warning("Could not save bind configuration: " + exception.Message);
            }
        }

        private void OnTogglePressed(object sender, EventArgs args)
        {
            if (ToggleRequested != null)
                ToggleRequested();
        }

        private static string GetSignature(BindDefinition[] definitions)
        {
            var signature = new StringBuilder();
            for (int bindIndex = 0; bindIndex < definitions.Length; bindIndex++)
            {
                BindDefinition definition = definitions[bindIndex];
                signature.Append(definition.name).Append(':');
                AppendControls(signature, definition.controlNames);

                if (definition.aliases != null)
                {
                    for (int aliasIndex = 0; aliasIndex < definition.aliases.Length; aliasIndex++)
                    {
                        signature.Append('|');
                        AppendControls(signature, definition.aliases[aliasIndex].controlNames);
                    }
                }

                signature.Append(';');
            }

            return signature.ToString();
        }

        private static void AppendControls(StringBuilder target, string[] controls)
        {
            if (controls == null)
                return;

            for (int index = 0; index < controls.Length; index++)
                target.Append(controls[index]).Append(',');
        }

        [XmlRoot("SEpediaBindings")]
        public sealed class BindingConfiguration
        {
            [XmlArray("Binds")]
            public BindDefinition[] Binds;
        }
    }
}
