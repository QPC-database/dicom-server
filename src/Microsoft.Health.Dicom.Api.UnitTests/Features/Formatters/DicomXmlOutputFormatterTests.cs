﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Dicom;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Health.Dicom.Api.Features.ContentTypes;
using Microsoft.Health.Dicom.Api.Features.Formatters;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Formatters
{
    public class DicomXmlOutputFormatterTests
    {
        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(DicomItem))]
        [InlineData(typeof(JObject))]
        [InlineData(null)]
        public void GivenAnInvalidDicomObjectAndXmlContentType_WhenCheckingCanWrite_ThenFalseShouldBeReturned(Type modelType)
        {
            bool result = CanWrite(modelType, KnownContentTypes.XmlContentType);
            Assert.False(result);
        }

        [Theory]
        [InlineData(typeof(DicomDataset))]
        [InlineData(typeof(IEnumerable<DicomDataset>))]
        [InlineData(typeof(IList<DicomDataset>))]
        [InlineData(typeof(IReadOnlyCollection<DicomDataset>))]
        public void GivenAValidDicomObjectAndXmlContentType_WhenCheckingCanWrite_ThenTrueShouldBeReturned(Type modelType)
        {
            bool result = CanWrite(modelType, KnownContentTypes.XmlContentType);
            Assert.True(result);
        }

        [Fact]
        public async Task GivenADicomDatasetAndXmlContentType_WhenSerializing_ThenTheObjectIsSerializedToTheResponseStream()
        {
            var formatter = new DicomXmlOutputFormatter();
            DicomDataset dataset = BuildSimpleDataset();
            var defaultHttpContext = new DefaultHttpContext();
            defaultHttpContext.Request.ContentType = KnownContentTypes.XmlContentType;

            using (var responseBody = new MemoryStream())
            {
                defaultHttpContext.Response.Body = responseBody;

                await formatter.WriteResponseBodyAsync(
                    new OutputFormatterWriteContext(
                        defaultHttpContext,
                        (stream, encoding) => new StreamWriter(stream, encoding),
                        typeof(DicomDataset),
                        dataset),
                    Encoding.UTF8);

                string expectedString;
                using (var stream = new MemoryStream())
                using (var streamWriter = new StreamWriter(stream, Encoding.UTF8))
                {
                    expectedString = Core.DicomXML.ConvertDicomToXML(dataset, Encoding.UTF8);
                }

                string actualString = Encoding.UTF8.GetString(responseBody.ToArray());
                Assert.Equal(expectedString, actualString.Substring(1));
            }
        }

        private DicomDataset BuildSimpleDataset()
        {
            var dataset = new DicomDataset();
            dataset.AddOrUpdate(DicomTag.StudyInstanceUID, "1.2.345");
            dataset.AddOrUpdate(DicomTag.PatientName, "Test^Name");
            return dataset;
        }

        private bool CanWrite(Type modelType, string contentType)
        {
            var formatter = new DicomXmlOutputFormatter();

            var defaultHttpContext = new DefaultHttpContext();
            defaultHttpContext.Request.ContentType = contentType;

            var result = formatter.CanWriteResult(
                new OutputFormatterWriteContext(
                    defaultHttpContext,
                    (stream, encoding) => null,
                    modelType,
                    new object()));

            return result;
        }
    }
}
