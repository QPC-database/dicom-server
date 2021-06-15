﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Operations.Functions.Configs
{
    /// <summary>
    /// Represents configuration settings related to the Dicom functions.
    /// </summary>
    public class DicomFunctionsConfiguration
    {
        public const string SectionName = "DicomFunctions";

        /// <summary>
        /// Gets or sets the settings for re-indexing DICOM instances
        /// </summary>
        [Required]
        public ReindexOperationConfiguration Reindex { get; set; }
    }
}