﻿/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using TeamCloud.Model.Commands;
using TeamCloud.Model.Internal.Data;

namespace TeamCloud.Model.Internal.Commands
{
    public class OrchestratorProjectUpdateCommand : OrchestratorCommand<Project, OrchestratorProjectUpdateCommandResult, ProviderProjectUpdateCommand, Model.Data.Project>
    {
        public OrchestratorProjectUpdateCommand(User user, Project payload) : base(user, payload) { }
    }
}
