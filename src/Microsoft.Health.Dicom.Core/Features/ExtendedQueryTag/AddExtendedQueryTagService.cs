﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class AddExtendedQueryTagService : IAddExtendedQueryTagService
    {
        private readonly IStoreFactory<IExtendedQueryTagStore> _extendedQueryTagStoreFactory;
        private readonly IExtendedQueryTagEntryValidator _extendedQueryTagEntryValidator;
        private readonly int _maxAllowedCount;

        public AddExtendedQueryTagService(
            IStoreFactory<IExtendedQueryTagStore> extendedQueryTagStoreFactory,
            IExtendedQueryTagEntryValidator extendedQueryTagEntryValidator,
            IOptions<ExtendedQueryTagConfiguration> extendedQueryTagConfiguration)
        {
            EnsureArg.IsNotNull(extendedQueryTagStoreFactory, nameof(extendedQueryTagStoreFactory));
            EnsureArg.IsNotNull(extendedQueryTagEntryValidator, nameof(extendedQueryTagEntryValidator));
            EnsureArg.IsNotNull(extendedQueryTagConfiguration?.Value, nameof(extendedQueryTagConfiguration));

            _extendedQueryTagStoreFactory = extendedQueryTagStoreFactory;
            _extendedQueryTagEntryValidator = extendedQueryTagEntryValidator;
            _maxAllowedCount = extendedQueryTagConfiguration.Value.MaxAllowedCount;
        }

        public async Task<AddExtendedQueryTagResponse> AddExtendedQueryTagAsync(IEnumerable<AddExtendedQueryTagEntry> extendedQueryTags, CancellationToken cancellationToken)
        {
            _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(extendedQueryTags);

            IEnumerable<AddExtendedQueryTagEntry> result = extendedQueryTags.Select(item => item.Normalize());

            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync(cancellationToken);
            await extendedQueryTagStore.AddExtendedQueryTagsAsync(result, ExtendedQueryTagStatus.Ready, _maxAllowedCount, cancellationToken);

            // Current solution is synchronous, no job uri is generated, so always return blank response.
            return new AddExtendedQueryTagResponse();
        }
    }
}
