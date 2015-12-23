// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.EntityModel.Builders;
    using Newtonsoft.Json.Linq;
    using System.Linq;
    using System.IO;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Plugins;
    using System.Reflection;

    class BuildCommand : ICommand
    {
        private DocumentBuilder _builder;
        private TemplateManager _templateManager;
        private string _helpMessage = null;
        public BuildJsonConfig Config { get; private set; }

        public BuildCommand(CommandContext context) : this(new BuildJsonConfig(), context)
        {
        }

        public BuildCommand(JToken value, CommandContext context) : this(CommandFactory.ConvertJTokenTo<BuildJsonConfig>(value), context)
        {
        }

        public BuildCommand(BuildJsonConfig config, CommandContext context)
        {
            InitBuildCommand(config, context);
        }

        public BuildCommand(Options options, CommandContext context)
        {
            var buildCommandOptions = options.BuildCommand;
            if (buildCommandOptions.IsHelp)
            {
                _helpMessage = HelpTextGenerator.GetHelpMessage(options, "build");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(buildCommandOptions.Log)) Logger.RegisterListener(new ReportLogListener(buildCommandOptions.Log));
                if (buildCommandOptions.LogLevel.HasValue) Logger.LogLevelThreshold = buildCommandOptions.LogLevel.Value;
                InitBuildCommand(GetConfigFromOptions(buildCommandOptions), context);
            }
        }

        private void InitBuildCommand(BuildJsonConfig config, CommandContext context)
        {
            Config = MergeConfig(config, context);
            SetDefaultConfig();

            var assembly = typeof(Program).Assembly;
            _templateManager = new TemplateManager(assembly, "Template", Config.Templates, Config.Themes, Config.BaseDirectory);
           
            _builder = LoadBuilder(Config);
            
        }

        private void SetDefaultConfig()
        {
            if (Config.Templates == null || Config.Templates.Count == 0)
            {
                Config.Templates = new ListWithStringFallback { Constants.DefaultTemplateName };
            }
        }

        private DocumentBuilder LoadBuilder(BuildJsonConfig config)
        {
            // 1. If plugin folder is not defined, use plugin defined in template
            // 1. If plugin folder is defined, use the plugin folder
            List<Assembly> pluginAssemblies = new List<Assembly> { typeof(DocumentBuilder).Assembly};
            if (Config.PluginFolders == null || Config.PluginFolders.Count == 0)
            {
                pluginAssemblies.AddRange(_templateManager.GetTemplatePlugins());
            }
            else
            {
                foreach(var folder in Config.PluginFolders)
                {
                    var pluginDir = Path.Combine(Config.BaseDirectory, folder);
                    if (Directory.Exists(pluginDir))
                    {
                        foreach (var file in Directory.EnumerateFiles(pluginDir, "*.dll"))
                        {
                            try
                            {
                                pluginAssemblies.Add(Assembly.LoadFile(file));
                            }
                            catch (BadImageFormatException e)
                            {
                                Logger.LogWarning($"{file} is not a valid Managed dll, ignored: {e.Message}");
                            }
                        }
                    }
                }
            }

            return new DocumentBuilder(pluginAssemblies);
        }
        public void Exec(RunningContext context)
        {
            if (_helpMessage != null)
            {
                Console.WriteLine(_helpMessage);
            }
            else
            {
                InternalExec(Config, context);
            }
        }

        private void InternalExec(BuildJsonConfig config, RunningContext context)
        {
            var parameters = ConfigToParameter(config);
            if (parameters.Files.Count == 0)
            {
                Logger.LogWarning("No files found, nothing is to be generated");
                return;
            }
            try
            {
                _builder.Build(parameters);
            }
            catch (AggregateDocumentException aggEx)
            {
                Logger.LogWarning("following document error:" + Environment.NewLine + string.Join(Environment.NewLine, from ex in aggEx.InnerExceptions select ex.Message));
                return;
            }
            catch (DocumentException ex)
            {
                Logger.LogWarning("document error:" + ex.Message);
                return;
            }

            var documentContext = DocumentBuildContext.DeserializeFrom(parameters.OutputBaseDir);
            // If RootOutput folder is specified from command line, use it instead of the base directory
            var outputFolder = Path.Combine(config.OutputFolder ?? config.BaseDirectory ?? string.Empty, config.Destination ?? string.Empty);
            _templateManager.ProcessTemplateAndTheme(documentContext, outputFolder, true);

            // TODO: SEARCH DATA

            if (config?.Serve ?? false)
            {
                ServeCommand.Serve(outputFolder, config.Port);
            }
        }

        private BuildJsonConfig MergeConfig(BuildJsonConfig config, CommandContext context)
        {
            config.BaseDirectory = context?.BaseDirectory ?? config.BaseDirectory;
            if (context?.SharedOptions != null)
            {
                config.OutputFolder = context.SharedOptions.RootOutputFolder ?? config.OutputFolder;
                var templates = context.SharedOptions.Templates;
                if (templates != null) config.Templates = new ListWithStringFallback(templates);
                var themes = context.SharedOptions.Themes;
                if (themes != null) config.Themes = new ListWithStringFallback(themes);
                var pluginFolders = context.SharedOptions.PluginFolders;
                if (pluginFolders != null) config.PluginFolders = new ListWithStringFallback(pluginFolders);
                config.Force |= context.SharedOptions.ForceRebuild;
                config.Serve |= context.SharedOptions.Serve;
                config.Port = context.SharedOptions.Port?.ToString();
            }
            return config;
        }

        private static DocumentBuildParameters ConfigToParameter(BuildJsonConfig config)
        {
            var parameters = new DocumentBuildParameters();
            var baseDirectory = config.BaseDirectory ?? Environment.CurrentDirectory;

            parameters.OutputBaseDir = Path.Combine(baseDirectory, "obj");
            if (config.GlobalMetadata != null) parameters.Metadata = config.GlobalMetadata.ToImmutableDictionary();
            if (config.FileMetadata != null) parameters.FileMetadata = ConvertToFileMetadataItem(baseDirectory, config.FileMetadata);
            parameters.ExternalReferencePackages = GetFilesFromFileMapping(GlobUtility.ExpandFileMapping(baseDirectory, config.ExternalReference)).ToImmutableArray();
            parameters.Files = GetFileCollectionFromFileMapping(baseDirectory,
               Tuple.Create(DocumentType.Article, GlobUtility.ExpandFileMapping(baseDirectory, config.Content)),
               Tuple.Create(DocumentType.Override, GlobUtility.ExpandFileMapping(baseDirectory, config.Overwrite)),
               Tuple.Create(DocumentType.Resource, GlobUtility.ExpandFileMapping(baseDirectory, config.Resource)));
            return parameters;
        }

        private static FileMetadata ConvertToFileMetadataItem(string baseDirectory, Dictionary<string, FileMetadataPairs> fileMetadata)
        {
            var result = new Dictionary<string, ImmutableArray<FileMetadataItem>>();
            foreach (var item in fileMetadata)
            {
                var list = new List<FileMetadataItem>();
                foreach(var pair in item.Value.Items)
                {
                    list.Add(new FileMetadataItem(pair.Glob, item.Key, pair.Value));
                }
                result.Add(item.Key, list.ToImmutableArray());
            }

            return new FileMetadata(baseDirectory, result);
        }

        private static IEnumerable<string> GetFilesFromFileMapping(FileMapping mapping)
        {
            if (mapping == null) yield break;
            foreach (var file in mapping.Items)
            {
                foreach (var item in file.Files)
                {
                    yield return Path.Combine(file.CurrentWorkingDirectory ?? Environment.CurrentDirectory, item);
                }
            }
        }

        private static FileCollection GetFileCollectionFromFileMapping(string baseDirectory, params Tuple<DocumentType, FileMapping>[] files)
        {
            var fileCollection = new FileCollection(baseDirectory);
            foreach (var file in files)
            {
                if (file.Item2 != null)
                {
                    foreach (var mapping in file.Item2.Items)
                    {
                        fileCollection.Add(file.Item1, mapping.CurrentWorkingDirectory, mapping.Files);
                    }
                }
            }

            return fileCollection;
        }

        private static BuildJsonConfig GetConfigFromOptions(BuildCommandOptions options)
        {
            string configFile = options.ConfigFile;
            if (string.IsNullOrEmpty(configFile) && options.Content == null && options.Resource == null)
            {
                if (!File.Exists(Constants.ConfigFileName))
                {
                    throw new ArgumentException("Either provide config file or specify content files to start building documentation.");
                }
                else
                {
                    Logger.Log(LogLevel.Info, $"Config file {Constants.ConfigFileName} found, start building...");
                    configFile = Constants.ConfigFileName;
                }
            }

            BuildJsonConfig config;
            if (!string.IsNullOrEmpty(configFile))
            {
                var command = (BuildCommand)CommandFactory.ReadConfig(configFile, null).Commands.FirstOrDefault(s => s is BuildCommand);
                if (command == null) throw new ApplicationException($"Unable to find {CommandType.Build} subcommand config in file '{Constants.ConfigFileName}'.");
                config = command.Config;
                config.BaseDirectory = Path.GetDirectoryName(configFile);
            }
            else
            {
                config = new BuildJsonConfig();
            }

            config.OutputFolder = options.OutputFolder;
            string optionsBaseDirectory = Environment.CurrentDirectory;
            // Override config file with options from command line
            if (options.Templates != null && options.Templates.Count > 0) config.Templates = new ListWithStringFallback(options.Templates);

            if (options.Themes != null && options.Themes.Count > 0) config.Themes = new ListWithStringFallback(options.Themes);
            if (!string.IsNullOrEmpty(options.OutputFolder)) config.Destination = Path.GetFullPath(Path.Combine(options.OutputFolder, config.Destination ?? string.Empty));
            if (options.Content != null)
            {
                if (config.Content == null)
                    config.Content = new FileMapping(new FileMappingItem());
                config.Content.Add(new FileMappingItem() { Files = new FileItems(options.Content), CurrentWorkingDirectory = optionsBaseDirectory });
            }
            if (options.Resource != null)
            {
                if (config.Resource == null)
                    config.Resource = new FileMapping(new FileMappingItem());
                config.Resource.Add(new FileMappingItem() { Files = new FileItems(options.Resource), CurrentWorkingDirectory = optionsBaseDirectory });
            }
            if (options.Overwrite != null)
            {
                if (config.Overwrite == null)
                    config.Overwrite = new FileMapping(new FileMappingItem());
                config.Overwrite.Add(new FileMappingItem() { Files = new FileItems(options.Overwrite), CurrentWorkingDirectory = optionsBaseDirectory });
            }
            if (options.ExternalReference != null)
            {
                if (config.ExternalReference == null)
                    config.ExternalReference = new FileMapping(new FileMappingItem());
                config.ExternalReference.Add(new FileMappingItem() { Files = new FileItems(options.ExternalReference), CurrentWorkingDirectory = optionsBaseDirectory });
            }
            if (options.Serve) config.Serve = options.Serve;
            if (options.Port.HasValue) config.Port = options.Port.Value.ToString();
            return config;
        }
    }
}
