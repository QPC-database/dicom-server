﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents;
using Xunit;

namespace Microsoft.Health.Dicom.CosmosDb.UnitTests.Features.Storage.Documents
{
    public class QuerySeriesDocumentTests
    {
        [Fact]
        public void GivenInvalidInstanceIdentifers_WhenCreatingQuerySeriesDocument_ExceptionsThrown()
        {
            Assert.Throws<ArgumentNullException>(() => new QuerySeriesDocument(null, Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => new QuerySeriesDocument(string.Empty, Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => new QuerySeriesDocument(new string('a', 65), Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => new QuerySeriesDocument("?...", Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentNullException>(() => new QuerySeriesDocument(Guid.NewGuid().ToString(), null));
            Assert.Throws<ArgumentException>(() => new QuerySeriesDocument(Guid.NewGuid().ToString(), string.Empty));
            Assert.Throws<ArgumentException>(() => new QuerySeriesDocument("sameid", "sameid"));

            Assert.Throws<ArgumentNullException>(() => QuerySeriesDocument.GetDocumentId(null, Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => QuerySeriesDocument.GetDocumentId(string.Empty, Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => new QuerySeriesDocument(Guid.NewGuid().ToString(), new string('a', 65)));
            Assert.Throws<ArgumentException>(() => new QuerySeriesDocument(Guid.NewGuid().ToString(), "?..."));
            Assert.Throws<ArgumentNullException>(() => QuerySeriesDocument.GetDocumentId(Guid.NewGuid().ToString(), null));
            Assert.Throws<ArgumentException>(() => QuerySeriesDocument.GetDocumentId(Guid.NewGuid().ToString(), string.Empty));
            Assert.Throws<ArgumentException>(() => QuerySeriesDocument.GetDocumentId("sameid", "sameid"));

            Assert.Throws<ArgumentNullException>(() => QuerySeriesDocument.GetPartitionKey(null));
            Assert.Throws<ArgumentException>(() => QuerySeriesDocument.GetPartitionKey(string.Empty));
        }

        [Fact]
        public void GivenExistingInstance_WhenAddingToInstancesHashSet_IsNotAdded()
        {
            var document = new QuerySeriesDocument(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            var dataset = new DicomDataset();
            var sopInstanceUID = Guid.NewGuid().ToString();
            var testPatientName = Guid.NewGuid().ToString();
            dataset.Add(DicomTag.SOPInstanceUID, sopInstanceUID);
            dataset.Add(DicomTag.PatientName, testPatientName);

            var instanceDocument1 = QueryInstance.Create(dataset, new[] { DicomTag.PatientName });
            var instanceDocument2 = QueryInstance.Create(dataset, new[] { DicomTag.PatientName });

            Assert.Throws<ArgumentNullException>(() => document.AddInstance(null));
            Assert.True(document.AddInstance(instanceDocument1));
            Assert.False(document.AddInstance(instanceDocument2));

            Assert.Equal(testPatientName, document.DistinctIndexedAttributes[DicomTag.PatientName].First());

            Assert.Throws<ArgumentNullException>(() => document.RemoveInstance(null));
            Assert.Throws<ArgumentException>(() => document.RemoveInstance(string.Empty));
            Assert.True(document.RemoveInstance(sopInstanceUID));
            Assert.False(document.RemoveInstance(sopInstanceUID));
        }
    }
}