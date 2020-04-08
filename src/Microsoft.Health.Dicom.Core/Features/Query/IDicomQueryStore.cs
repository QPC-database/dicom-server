﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public interface IDicomQueryStore
    {
        Task<DicomQueryResult> QueryAsync(
            DicomQueryExpression query,
            CancellationToken cancellationToken = default);
    }
}