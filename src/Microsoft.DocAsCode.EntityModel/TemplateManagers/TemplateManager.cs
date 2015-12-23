// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Builders;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Utility;

    public class TemplateManager
    {
        private const string TemplateEntry = "index.html";

        private const string TocApi = @"
- name: Api {0}
  href: {0}
";

        private const string TocConceputal = @"
- name: {0}
  href: {0}
";

        public const string DefaultTocEntry = "toc.yml";
        private List<string> _templates = new List<string>();
        private List<string> _themes = new List<string>();
        private ResourceFinder _finder;
        public TemplateManager(Assembly assembly, string rootNamespace, List<string> templates, List<string> themes, string baseDirectory)
        {
            _finder = new ResourceFinder(assembly, rootNamespace, baseDirectory);
            if (templates == null || templates.Count == 0)
            {
                Logger.Log(LogLevel.Info, "Template is not specified, files will not be transformed.");
            }
            else
            {
                _templates = templates;
            }
            
            if (themes == null || themes.Count == 0)
            {
                Logger.Log(LogLevel.Info, "Theme is not specified, no additional theme will be applied to the documentation.");
            }
            else
            {
                _themes = themes;
            }
        }

        /// <summary>
        /// Template can contain a set of plugins to define the behavior of how to generate the output YAML data model
        /// The name of plugin folder is always "plugins"
        /// </summary>
        public IEnumerable<Assembly> GetTemplatePlugins()
        {
            using (var templateResource = new CompositeResourceCollectionWithOverridden(_templates.Select(s => _finder.Find(s)).Where(s => s != null)))
            {
                if (templateResource.IsEmpty)
                {
                    yield break;
                }
                else
                {
                    foreach (var pair in templateResource.GetResourceStreams(@"^fonts/.*\.dll"))
                    {
                        using (var stream = pair.Value)
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                stream.CopyTo(memoryStream);
                                Assembly assembly;
                                try
                                {
                                    assembly = Assembly.Load(memoryStream.ToArray());
                                }
                                catch (BadImageFormatException e)
                                {
                                    Logger.LogWarning($"{pair.Key} is not a valid Managed dll, ignored: {e.Message}");
                                    continue;
                                }
                                yield return assembly;
                            }
                        }
                    }
                }
            }
        }

        public void ProcessTemplateAndTheme(DocumentBuildContext context, string outputDirectory, bool overwrite)
        {
            ProcessTemplate(context, outputDirectory);
            ProcessTheme(outputDirectory, overwrite);
        }

        public static void GenerateDefaultToc(IEnumerable<string> apiFolder, IEnumerable<string> conceptualFolder, string outputFolder, bool overwrite)
        {
            if (string.IsNullOrEmpty(outputFolder)) outputFolder = Environment.CurrentDirectory;
            var targetTocPath = Path.Combine(outputFolder, DefaultTocEntry);
            var message = overwrite ? $"Root toc.yml {targetTocPath} is overwritten." : $"Root toc.yml {targetTocPath} is not found, default toc.yml is generated.";
            Copy(s =>
            {
                using (var writer = new StreamWriter(s))
                {
                    if (apiFolder != null)
                        foreach (var i in apiFolder)
                        {
                            var relativePath = PathUtility.MakeRelativePath(outputFolder, i);
                            writer.Write(string.Format(TocApi, relativePath));
                        }
                    if (conceptualFolder != null)
                        foreach (var i in conceptualFolder)
                        {
                            var relativePath = PathUtility.MakeRelativePath(outputFolder, i);
                            writer.Write(string.Format(TocConceputal, relativePath));
                        }
                    Logger.Log(LogLevel.Info, message);
                }
            }, targetTocPath, overwrite);
        }

        private void ProcessTemplate(DocumentBuildContext context, string outputDirectory)
        {
            using (var templateResource = new CompositeResourceCollectionWithOverridden(_templates.Select(s => _finder.Find(s)).Where(s => s != null)))
            {
                if (templateResource.IsEmpty)
                {
                    Logger.Log(LogLevel.Warning, $"No template resource found for [{_templates.ToDelimitedString()}].");
                }
                else
                {
                    Logger.Log(LogLevel.Verbose, "Template resource found, starting applying template.");
                    using (var processor = new TemplateProcessor(templateResource))
                    {
                        processor.Process(context, outputDirectory);
                    }
                }
            }
        }

        private void ProcessTheme(string outputDirectory, bool overwrite)
        {

            using (var themeResources = new CompositeResourceCollectionWithOverridden(_themes.Select(s => _finder.Find(s)).Where(s => s != null)))
            {

                if (themeResources.IsEmpty)
                {
                    Logger.Log(LogLevel.Warning, $"No theme resource found for [{_themes.ToDelimitedString()}].");
                }
                else
                {
                    Logger.Log(LogLevel.Verbose, "Theme resource found, starting copying theme.");
                    foreach (var resourceName in themeResources.Names)
                    {
                        using (var stream = themeResources.GetResourceStream(resourceName))
                        {
                            var outputPath = Path.Combine(outputDirectory, resourceName);
                            CopyResource(stream, outputPath, overwrite);
                            Logger.Log(LogLevel.Info, $"Theme resource {resourceName} copied to {outputPath}.");
                        }
                    }
                }
            }
        }

        private static void CopyResource(Stream stream, string filePath, bool overwrite)
        {
            Copy(fs =>
            {
                stream.CopyTo(fs);
            }, filePath, overwrite);
        }

        private static void Copy(Action<Stream> streamHandler, string filePath, bool overwrite)
        {
            FileMode fileMode = overwrite ? FileMode.Create : FileMode.CreateNew;
            try
            {
                var subfolder = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(subfolder) && !Directory.Exists(subfolder))
                {
                    Directory.CreateDirectory(subfolder);
                }

                using (var fs = new FileStream(filePath, fileMode, FileAccess.ReadWrite, FileShare.ReadWrite))
                    streamHandler(fs);
            }
            catch (IOException e)
            {
                // If the file already exists, skip
                Logger.Log(LogLevel.Info, $"File {filePath}: {e.Message}, skipped");
            }
        }
    }
    
}
