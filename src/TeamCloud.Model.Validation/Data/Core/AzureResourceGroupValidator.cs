/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using FluentValidation;
using TeamCloud.Model.Data.Core;

namespace TeamCloud.Model.Validation.Data
{
    public sealed class AzureResourceGroupValidator : AbstractValidator<AzureResourceGroup>
    {
        public AzureResourceGroupValidator()
        {
            RuleFor(obj => obj.SubscriptionId).MustBeGuid();
            RuleFor(obj => obj.Name).NotEmpty();
            RuleFor(obj => obj.Region).MustBeAzureRegion();
        }
    }
}
