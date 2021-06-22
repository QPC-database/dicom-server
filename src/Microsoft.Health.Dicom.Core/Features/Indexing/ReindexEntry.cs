﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    /// <summary>
    /// Entry of ReindexStateStore.
    /// </summary>
    public class ReindexStateEntry
    {
        /// <summary>
        /// The store entry.
        /// </summary>
        public ExtendedQueryTagStoreEntry StoreEntry { get; set; }

        /// <summary>
        /// The operation id.
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// The reindex status.
        /// </summary>
        public IndexStatus Status { get; set; }

        /// <summary>
        /// The start watermark.
        /// </summary>
        public long? StartWatermark { get; set; }

        /// <summary>
        /// The end watermark.
        /// </summary>
        public long? EndWatermark { get; set; }
    }
}
