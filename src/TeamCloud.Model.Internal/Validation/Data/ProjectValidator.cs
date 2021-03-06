/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using FluentValidation;
using TeamCloud.Model.Internal.Data;

namespace TeamCloud.Model.Internal.Validation.Data
{
    public sealed class ProjectValidator : AbstractValidator<Project>
    {
        public ProjectValidator()
        {
            RuleFor(obj => obj.Name)
                .NotEmpty();

            RuleFor(obj => obj.Type)
                .NotEmpty();

            // RuleFor(obj => obj.Users)
            //     .NotEmpty();
        }
    }
}
