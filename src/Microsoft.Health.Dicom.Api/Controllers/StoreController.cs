﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Messages.Store;
using Microsoft.Health.Dicom.Core.Web;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ApiVersion("1.0-prerelease")]
    [QueryModelStateValidator]
    [ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
    public class StoreController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<StoreController> _logger;

        public StoreController(IMediator mediator, ILogger<StoreController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [ProducesResponseType(typeof(DicomDataset), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(DicomDataset), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType(typeof(DicomDataset), (int)HttpStatusCode.Conflict)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.UnsupportedMediaType)]
        [HttpPost]
        [VersionedRoute(KnownRoutes.StoreRoute)]
        [Route(KnownRoutes.StoreRoute)]
        [AuditEventType(AuditEventSubType.Store)]
        public async Task<IActionResult> PostAsync(string studyInstanceUid = null)
        {
            long fileSize = Request.ContentLength ?? 0;
            _logger.LogInformation("DICOM Web Store Transaction request received, with study instance UID {studyInstanceUid} and file size of {fileSize} bytes", studyInstanceUid, fileSize);

            StoreResponse storeResponse = await _mediator.StoreDicomResourcesAsync(
                Request.Body,
                Request.ContentType,
                studyInstanceUid,
                HttpContext.RequestAborted);

            return StatusCode(
                (int)storeResponse.Status.ToHttpStatusCode(),
                storeResponse.Dataset);
        }
    }
}
