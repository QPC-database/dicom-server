﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Health.Dicom.Api.Features.Routing
{
    public sealed class VersionedRouteAttribute : RouteAttribute
    {
        public VersionedRouteAttribute(string template)
            : base("v{version:apiVersion}/" + template)
        {
        }
    }
}
