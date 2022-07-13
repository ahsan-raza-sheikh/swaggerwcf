﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using SwaggerWcf.Attributes;
using SwaggerWcf.Configuration;
using SwaggerWcf.Models;
using SettingElement = SwaggerWcf.Configuration.SettingElement;

namespace SwaggerWcf.Support
{
    internal class ServiceBuilder
    {
        public static Service Build(string path)
        {
            return BuildServiceCommon(path, BuildPaths);
        }

        public static Service Build<TBusiness>(string path)
        {
            return BuildServiceCommon(path, BuildPaths<TBusiness>);
        }

        private static Service BuildServiceCommon(string path, Action<Service, IList<string>, List<TagElement>, IList<Type>> buildPaths)
        {
            const string sectionName = "swaggerwcf";
            SwaggerWcfSection config =
                (SwaggerWcfSection)(ConfigurationManager.GetSection(sectionName) ?? new SwaggerWcfSection());

            List<Type> definitionsTypesList = new List<Type>();
            Service service = new Service();
            var hiddenTags = GetHiddenTags(config);
            var visibleTags = GetVisibleTags(config);
            IReadOnlyDictionary<string, string> settings = GetSettings(config);

            ProcessSettings(service, settings);

            buildPaths(service, hiddenTags, visibleTags, definitionsTypesList);

            service.Definitions = DefinitionsBuilder.Process(hiddenTags, visibleTags, definitionsTypesList);

            var distinctTags = service.Paths.SelectMany(p => p.Actions).SelectMany(act => act.Tags)
                .Distinct();


            var tagsToOrder = distinctTags.Select(tagName =>
            {
                // If tag listed as visible, try to take Description and SortOrder from there
                var existingVisibleDef = visibleTags.FirstOrDefault(vt => vt.Name == tagName);
                return new TagDeffinition
                {
                    Name = tagName,
                    Description = existingVisibleDef?.Description ?? string.Empty,
                    SortOrder = existingVisibleDef?.SortOrder ?? 0
                };
            });

            service.Tags = tagsToOrder.OrderBy(t => t.SortOrder)
                .ThenBy(t => t.Name)
                .ToList();

            return service;
        }


        private static List<string> GetHiddenTags(SwaggerWcfSection config)
        {
            return config.Tags?.OfType<TagElement>()
                       .Where(t => t.Visibile.Equals(false))
                       .Select(t => t.Name)
                       .ToList() ?? new List<string>();
        }

        private static List<TagElement> GetVisibleTags(SwaggerWcfSection config)
        {
            return config.Tags?.OfType<TagElement>()
                       .Where(t => t.Visibile)
                       .ToList();
        }

        private static IReadOnlyDictionary<string, string> GetSettings(SwaggerWcfSection config)
        {
            return config.Settings?.OfType<SettingElement>().ToDictionary(se => se.Name, se => se.Value)
                ?? new Dictionary<string, string>();
        }

        private static void ProcessSettings(Service service, IReadOnlyDictionary<string, string> settings)
        {
            if (settings.ContainsKey("BasePath"))
                service.BasePath = settings["BasePath"];
            if (settings.ContainsKey("Host"))
                service.Host = settings["Host"];
            if (settings.ContainsKey("Schemes"))
                service.Schemes = settings["Schemes"].Split(';').ToList();

            if (settings.Keys.Any(k => k.StartsWith("Info")))
                service.Info = new Info();
            if (settings.ContainsKey("InfoDescription"))
                service.Info.Description = settings["InfoDescription"];
            if (settings.ContainsKey("InfoVersion"))
                service.Info.Version = settings["InfoVersion"];
            if (settings.ContainsKey("InfoTermsOfService"))
                service.Info.TermsOfService = settings["InfoTermsOfService"];
            if (settings.ContainsKey("InfoTitle"))
                service.Info.Title = settings["InfoTitle"];

            if (settings.Keys.Any(k => k.StartsWith("InfoContact")))
                service.Info.Contact = new InfoContact();
            if (settings.ContainsKey("InfoContactName"))
                service.Info.Contact.Name = settings["InfoContactName"];
            if (settings.ContainsKey("InfoContactUrl"))
                service.Info.Contact.Url = settings["InfoContactUrl"];
            if (settings.ContainsKey("InfoContactEmail"))
                service.Info.Contact.Email = settings["InfoContactEmail"];

            if (settings.Keys.Any(k => k.StartsWith("InfoLicense")))
                service.Info.License = new InfoLicense();
            if (settings.ContainsKey("InfoLicenseUrl"))
                service.Info.License.Url = settings["InfoLicenseUrl"];
            if (settings.ContainsKey("InfoLicenseName"))
                service.Info.License.Name = settings["InfoLicenseName"];
        }

        private static void BuildPaths(Service service, IList<string> hiddenTags, List<TagElement> visibleTags, IList<Type> definitionsTypesList)
        {
            service.Paths = new List<Path>();

            IEnumerable<(SwaggerWcfAttribute, TypeInfo)> types = GetAssemblyTypes(hiddenTags);

            var mapper = new Mapper(hiddenTags);

            foreach (var (da, ti) in types)
            {
                if (service.Info is null)
                    service.Info = ti.GetServiceInfo();

                if (!string.IsNullOrWhiteSpace(service.BasePath))
                    service.BasePath.TrimEnd('/');

                string servicePath = string.IsNullOrWhiteSpace(da.ServicePath) ? string.Empty
                    : da.ServicePath.TrimEnd('/');

                if(!string.IsNullOrWhiteSpace(servicePath) && !servicePath.StartsWith("/"))
                    servicePath = "/" + servicePath;

                var paths = mapper.FindMethods(ti.AsType(), definitionsTypesList, servicePath);
                service.Paths.AddRange(paths);
            }
        }

        private static IEnumerable<(SwaggerWcfAttribute, TypeInfo)> GetAssemblyTypes(IList<string> hiddenTags)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                IEnumerable<TypeInfo> types;
                try
                {
                    types = assembly.DefinedTypes;
                }
                catch (Exception)
                {
                    // ignore assembly and continue
                    continue;
                }

                foreach (TypeInfo ti in types)
                {
                    var da = ti.GetCustomAttribute<SwaggerWcfAttribute>();
                    if (da == null || hiddenTags.Any(ht => ht == ti.AsType().Name))
                        continue;

                    yield return (da, ti);
                }
            }
        }

        private static void BuildPaths<TBusiness>(Service service, IList<string> hiddenTags, List<TagElement> visibleTags, IList<Type> definitionsTypesList)
        {
            var type = typeof(TBusiness);
            service.Paths = new List<Path>();

            var da = type.GetCustomAttribute<SwaggerWcfAttribute>();
            if (da == null || hiddenTags.Any(ht => ht == type.Name))
                return;

            var mapper = new Mapper(hiddenTags);

            if (string.IsNullOrWhiteSpace(service.BasePath))
                service.BasePath = da.ServicePath;

            if (service.BasePath.EndsWith("/"))
                service.BasePath = service.BasePath.Substring(0, service.BasePath.Length - 1);

            var paths = mapper.FindMethods(type, definitionsTypesList);
            service.Paths.AddRange(paths);
        }
    }
}
