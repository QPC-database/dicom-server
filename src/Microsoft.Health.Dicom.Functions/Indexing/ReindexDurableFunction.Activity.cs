// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Functions.Indexing.Models;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    public partial class ReindexDurableFunction
    {
        /// <summary>
        /// The activity to complete reindex.
        /// </summary>
        /// <param name="operationId">The operation id.</param>
        /// <param name="log">The log.</param>
        /// <returns>The task.</returns>
        [FunctionName(nameof(CompleteReindexingTagsAsync))]
        public Task CompleteReindexingTagsAsync([ActivityTrigger] string operationId, ILogger log)
        {
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Completing Reindex operation on {operationId}", operationId);
            return _reindexStore.CompleteReindexAsync(operationId);
        }

        /// <summary>
        ///  The activity to start reindex.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="log">The log.</param>
        /// <returns>The reindex operation.</returns>
        [FunctionName(nameof(PrepareReindexingTagsAsync))]
        public async Task<ReindexOperation> PrepareReindexingTagsAsync([ActivityTrigger] PrepareReindexingTagsInput input, ILogger log)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(log, nameof(log));
            log.LogInformation("Start reindex with {input}", input);
            return await _reindexStore.PrepareReindexingAsync(input.TagKeys, input.OperationId);
        }

        /// <summary>
        /// The activity to get processing query tags.
        /// </summary>
        /// <param name="operationId">The operation id.</param>
        /// <param name="log">The log.</param>
        /// <returns>Extended query tag store entries.</returns>
        [FunctionName(nameof(GetProcessingTagsAsync))]
        public async Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetProcessingTagsAsync([ActivityTrigger] string operationId, ILogger log)
        {
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Getting query tags which is being processed by operation {operationId}", operationId);
            var entries = await _reindexStore.GetReindexEntriesAsync(operationId);
            // only process tags which is on Processing
            var tagKeys = entries
                .Where(x => x.Status == IndexStatus.Processing)
                .Select(y => y.TagKey)
                .ToList();
            return await _extendedQueryTagStore.GetExtendedQueryTagsAsync(tagKeys);
        }

        /// <summary>
        /// The activity to reindex  Dicom instances.
        /// </summary>
        /// <param name="input">The input</param>
        /// <param name="logger">The log.</param>
        /// <returns>The task</returns>
        [FunctionName(nameof(ReindexInstancesAsync))]
        public async Task ReindexInstancesAsync([ActivityTrigger] ReindexInstanceInput input, ILogger logger)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(logger, nameof(logger));

            logger.LogInformation("Reindex instances with {input}", input);

            var instanceIdentifiers = await _instanceStore.GetInstanceIdentifiersAsync(input.WatermarkRange);

            foreach (var instanceIdentifier in instanceIdentifiers)
            {
                await _instanceReindexer.ReindexInstanceAsync(input.TagStoreEntries, instanceIdentifier.Version);
            }

        }
    }
}
