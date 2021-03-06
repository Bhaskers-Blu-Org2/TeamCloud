﻿/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Orchestration;
using TeamCloud.Orchestration.Auditing;
using TeamCloud.Orchestrator.Activities;

namespace TeamCloud.Orchestrator.Orchestrations.Utilities
{
    public static class OrchestratorCommandOrchestration
    {
        [FunctionName(nameof(OrchestratorCommandOrchestration))]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var command = functionContext.GetInput<ICommand>();
            var commandResult = command.CreateResult();
            var commandLog = functionContext.CreateReplaySafeLogger(log ?? NullLogger.Instance);

            try
            {
                await functionContext
                    .AuditAsync(command, commandResult)
                    .ConfigureAwait(true);

                var commandOrchestration = await functionContext
                    .CallActivityWithRetryAsync<string>(nameof(OrchestratorCommandDispatchActivity), command)
                    .ConfigureAwait(true);

                commandResult = await functionContext
                    .CallSubOrchestratorWithRetryAsync<ICommandResult>(commandOrchestration, command.CommandId.ToString(), command)
                    .ConfigureAwait(true);
            }
            catch (Exception exc)
            {
                commandLog.LogError(exc, $"Processing command '{command.GetType().FullName}' ({command.CommandId}) Failed >>> {exc.Message}");

                commandResult ??= command.CreateResult();
                commandResult.Errors.Add(exc);
            }
            finally
            {
                if (commandResult?.RuntimeStatus.IsUnknown() ?? false)
                {
                    commandResult = await functionContext
                        .CallActivityWithRetryAsync<ICommandResult>(nameof(CommandResultAugmentActivity), commandResult)
                        .ConfigureAwait(true);
                }

                await functionContext
                    .AuditAsync(command, commandResult)
                    .ConfigureAwait(true);

                functionContext.SetOutput(commandResult);
            }
        }
    }
}
