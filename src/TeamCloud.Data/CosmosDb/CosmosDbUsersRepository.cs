/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using TeamCloud.Data.CosmosDb.Core;
using TeamCloud.Model.Data.Core;
using TeamCloud.Model.Internal.Data.Core;
using TeamCloud.Model.Validation;
using User = TeamCloud.Model.Internal.Data.User;

namespace TeamCloud.Data.CosmosDb
{

    public class CosmosDbUsersRepository : CosmosDbRepository<User>, IUsersRepository
    {
        public CosmosDbUsersRepository(ICosmosDbOptions cosmosOptions)
            : base(cosmosOptions)
        { }

        public async Task<User> AddAsync(User user)
        {
            if (user is null)
                throw new ArgumentNullException(nameof(user));

            await user
                .ValidateAsync(throwOnValidationError: true)
                .ConfigureAwait(false);

            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            var response = await container
                .CreateItemAsync(user, new PartitionKey(Options.TenantName))
                .ConfigureAwait(false);

            return response.Resource;
        }

        public async Task<User> GetAsync(string id)
        {
            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            try
            {
                var response = await container
                    .ReadItemAsync<User>(id, new PartitionKey(Options.TenantName))
                    .ConfigureAwait(false);

                return response.Resource;
            }
            catch (CosmosException cosmosEx) when (cosmosEx.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async IAsyncEnumerable<User> ListAsync()
        {
            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            var query = new QueryDefinition($"SELECT * FROM u");
            var queryIterator = container.GetItemQueryIterator<User>(query, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(Options.TenantName) });

            while (queryIterator.HasMoreResults)
            {
                var queryResponse = await queryIterator
                    .ReadNextAsync()
                    .ConfigureAwait(false);

                foreach (var user in queryResponse)
                    yield return user;
            }
        }

        public async IAsyncEnumerable<User> ListAsync(string projectId)
        {
            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            var query = new QueryDefinition($"SELECT VALUE u FROM u WHERE EXISTS(SELECT VALUE m FROM m IN u.projectMemberships WHERE m.projectId = '{projectId}')");

            var queryIterator = container.GetItemQueryIterator<User>(query, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(Options.TenantName) });

            while (queryIterator.HasMoreResults)
            {
                var queryResponse = await queryIterator
                    .ReadNextAsync()
                    .ConfigureAwait(false);

                foreach (var user in queryResponse)
                    yield return user;
            }
        }

        public async IAsyncEnumerable<User> ListOwnersAsync(string projectId)
        {
            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            var query = new QueryDefinition($"SELECT VALUE u FROM u WHERE EXISTS(SELECT VALUE m FROM m IN u.projectMemberships WHERE m.projectId = '{projectId}' AND m.role = '{ProjectUserRole.Owner}')");

            var queryIterator = container.GetItemQueryIterator<User>(query, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(Options.TenantName) });

            while (queryIterator.HasMoreResults)
            {
                var queryResponse = await queryIterator
                    .ReadNextAsync()
                    .ConfigureAwait(false);

                foreach (var user in queryResponse)
                    yield return user;
            }
        }

        public async IAsyncEnumerable<User> ListAdminsAsync()
        {
            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            var query = new QueryDefinition($"SELECT * FROM u WHERE u.role = '{TeamCloudUserRole.Admin}'");

            var queryIterator = container.GetItemQueryIterator<User>(query, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(Options.TenantName) });

            while (queryIterator.HasMoreResults)
            {
                var queryResponse = await queryIterator
                    .ReadNextAsync()
                    .ConfigureAwait(false);

                foreach (var user in queryResponse)
                    yield return user;
            }
        }

        public async Task<User> SetAsync(User user)
        {
            if (user is null)
                throw new ArgumentNullException(nameof(user));

            await user
                .ValidateAsync(throwOnValidationError: true)
                .ConfigureAwait(false);

            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            var response = await container
                .UpsertItemAsync<User>(user, new PartitionKey(Options.TenantName))
                .ConfigureAwait(false);

            return response.Resource;
        }

        public async Task<User> RemoveAsync(User user)
        {
            if (user is null)
                throw new ArgumentNullException(nameof(user));

            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            try
            {
                var response = await container
                    .DeleteItemAsync<User>(user.Id, new PartitionKey(Options.TenantName))
                    .ConfigureAwait(false);

                return response.Resource;
            }
            catch (CosmosException cosmosEx) when (cosmosEx.StatusCode == HttpStatusCode.NotFound)
            {
                return null; // already deleted
            }
        }

        public async Task RemoveProjectMembershipsAsync(string projectId)
        {
            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            var query = new QueryDefinition($"SELECT VALUE u FROM u WHERE EXISTS(SELECT VALUE m FROM m IN u.projectMemberships WHERE m.projectId = '{projectId}')");

            var queryIterator = container.GetItemQueryIterator<User>(query, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(Options.TenantName) });


            while (queryIterator.HasMoreResults)
            {
                var queryResponse = await queryIterator
                    .ReadNextAsync()
                    .ConfigureAwait(false);

                foreach (var user in queryResponse)
                    _ = await RemoveProjectMembershipAsync(user, projectId)
                        .ConfigureAwait(false);
            }
        }

        public async Task<User> RemoveProjectMembershipAsync(User user, string projectId)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));

            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(((IContainerDocument)user).ETag))
            {
                var existingUser = await GetAsync(user.Id)
                    .ConfigureAwait(false);

                // user no longer exists
                if (existingUser is null)
                    return null;

                user = existingUser;
            }

            return await RemoveProjectMembershipSafeAsync(container, user, projectId)
                .ConfigureAwait(false);

            async Task<User> RemoveProjectMembershipSafeAsync(Container container, User user, string projectId)
            {
                var membership = user.ProjectMemberships.FirstOrDefault(m => m.ProjectId == projectId);

                if (membership is null)
                    return user;

                while (true)
                {
                    try
                    {
                        user.ProjectMemberships.Remove(membership);

                        return await container.ReplaceItemAsync(
                                user,
                                user.Id,
                                new PartitionKey(Options.TenantName),
                                new ItemRequestOptions { IfMatchEtag = ((IContainerDocument)user).ETag }).ConfigureAwait(false);
                    }
                    catch (CosmosException exc) when (exc.StatusCode == HttpStatusCode.NotFound)
                    {
                        // the requested user does not exist anymore - continue
                        return null;
                    }
                    catch (CosmosException exc) when (exc.StatusCode == HttpStatusCode.PreconditionFailed)
                    {
                        // the requested user has changed, get it again before proceeding
                        user = await GetAsync(user.Id).ConfigureAwait(false);
                    }
                }
            }
        }

        public Task<User> AddProjectMembershipAsync(User user, string projectId, ProjectUserRole role, IDictionary<string, string> properties)
        {
            return AddProjectMembershipAsync(user, new ProjectMembership
            {
                ProjectId = projectId,
                Role = role,
                Properties = properties ?? new Dictionary<string, string>()
            });
        }

        // this method can only change project memberships
        // other changes to the user object will be overwitten
        public async Task<User> AddProjectMembershipAsync(User user, ProjectMembership membership)
        {
            if (user is null)
                throw new ArgumentNullException(nameof(user));

            if (membership is null)
                throw new ArgumentNullException(nameof(membership));

            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(((IContainerDocument)user).ETag))
            {
                var existingUser = await GetAsync(user.Id).ConfigureAwait(false)
                    ?? await AddAsync(user).ConfigureAwait(false);

                user = existingUser;
            }

            return await AddProjectMembershipSafeAsync(container, user, membership)
                .ConfigureAwait(false);

            async Task<User> AddProjectMembershipSafeAsync(Container container, User user, ProjectMembership membership)
            {
                while (true)
                {
                    try
                    {
                        user.EnsureProjectMembership(membership);

                        return await container.ReplaceItemAsync(
                            user,
                            user.Id,
                            new PartitionKey(Options.TenantName),
                            new ItemRequestOptions { IfMatchEtag = ((IContainerDocument)user).ETag }).ConfigureAwait(false);
                    }
                    catch (CosmosException exc) when (exc.StatusCode == HttpStatusCode.NotFound)
                    {
                        // the requested user does not exist anymore - continue
                        return null;
                    }
                    catch (CosmosException exc) when (exc.StatusCode == HttpStatusCode.PreconditionFailed)
                    {
                        // the requested user has changed, get it again before proceeding
                        user = await GetAsync(user.Id).ConfigureAwait(false);
                    }
                }
            }
        }

        public async Task<User> SetTeamCloudInfoAsync(User user)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));

            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(((IContainerDocument)user).ETag))
            {
                var existingUser = await GetAsync(user.Id).ConfigureAwait(false)
                    ?? await AddAsync(user).ConfigureAwait(false);

                user = existingUser;
            }

            return await SetTeamCloudInfoSafeAsync(container, user)
                .ConfigureAwait(false);


            async Task<User> SetTeamCloudInfoSafeAsync(Container container, User user)
            {
                while (true)
                {
                    try
                    {
                        return await container.ReplaceItemAsync(
                            user,
                            user.Id,
                            new PartitionKey(Options.TenantName),
                            new ItemRequestOptions { IfMatchEtag = ((IContainerDocument)user).ETag }).ConfigureAwait(false);
                    }
                    catch (CosmosException exc) when (exc.StatusCode == HttpStatusCode.NotFound)
                    {
                        // the requested user does not exist anymore - continue
                        return null;
                    }
                    catch (CosmosException exc) when (exc.StatusCode == HttpStatusCode.PreconditionFailed)
                    {
                        // the requested user has changed, get it again before proceeding
                        user = await GetAsync(user.Id).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
