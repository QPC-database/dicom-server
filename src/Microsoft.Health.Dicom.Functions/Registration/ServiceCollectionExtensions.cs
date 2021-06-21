﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Modules;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.Operations.Functions.Configs;
using Microsoft.Health.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;

namespace Microsoft.Health.Dicom.Operations.Functions.Registration
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add services for DICOM functions.
        /// </summary>
        /// <param name="services">The DICOM function builder instance.</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The DICOM function builder instance.</returns>
        public static IDicomServerBuilder AddDicomFunctions(this IServiceCollection services, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(services, nameof(services));
            DicomFunctionsConfiguration dicomOperationsConfig = new DicomFunctionsConfiguration();
            configuration?.GetSection(DicomFunctionsConfiguration.SectionName).Bind(dicomOperationsConfig);
            services.AddSingleton(Options.Create(dicomOperationsConfig));

            services.AddMvcCore()
                .AddNewtonsoftJson(x => x.SerializerSettings.Converters
                .Add(new StringEnumConverter()));
            System.Console.WriteLine("After AddMvcCore");
            foreach (var item in services)
            {
                if (item.ServiceType == typeof(IHostedService))
                {
                    System.Console.WriteLine(item);
                }
            }
            System.Console.WriteLine();
            services.RegisterAssemblyModules(typeof(ServiceModule).Assembly, new FeatureConfiguration() { EnableExtendedQueryTags = true }, new ServicesConfiguration());
            System.Console.WriteLine("After RegisterAssemblyModules");
            foreach (var item in services)
            {
                if (item.ServiceType == typeof(IHostedService))
                {
                    System.Console.WriteLine(item);
                }
            }
            System.Console.WriteLine();
            return new DicomServerBuilder(services);
        }
        private class DicomServerBuilder : IDicomServerBuilder
        {
            public DicomServerBuilder(IServiceCollection services)
            {
                EnsureArg.IsNotNull(services, nameof(services));
                Services = services;
            }

            public IServiceCollection Services { get; }
        }

    }
}
