using Microsoft.Extensions.Hosting;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;

namespace Eng_FolderMetrics.HostedServices
{
    internal class WebexAPPIHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return CreateMSTeamsGroup();
        }

        private static Task CreateMSTeamsGroup()
        {
            var scopes = new[] { "User.Read" };

            // Multi-tenant apps can use "common",
            // single-tenant apps must use the tenant ID from the Azure portal
            var tenantId = "common";

            // Value from app registration
            var clientId = "YOUR_CLIENT_ID";

            // using Azure.Identity;
            var options = new DeviceCodeCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                ClientId = clientId,
                TenantId = tenantId,
                // Callback function that receives the user prompt
                // Prompt contains the generated device code that user must
                // enter during the auth process in the browser
                DeviceCodeCallback = (code, cancellation) =>
                {
                    Console.WriteLine(code.Message);
                    return Task.FromResult(0);
                },
            };

            // https://learn.microsoft.com/dotnet/api/azure.identity.devicecodecredential
            var deviceCodeCredential = new DeviceCodeCredential(options);

            var graphClient = new GraphServiceClient(deviceCodeCredential, scopes);

            var requestBody = new Team
            {
                MemberSettings = new TeamMemberSettings
                {
                    AllowCreatePrivateChannels = true,
                    AllowCreateUpdateChannels = true,
                },
                MessagingSettings = new TeamMessagingSettings
                {
                    AllowUserEditMessages = true,
                    AllowUserDeleteMessages = true,
                },
                FunSettings = new TeamFunSettings
                {
                    AllowGiphy = true,
                    GiphyContentRating = GiphyRatingType.Strict,
                },
            };

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=csharp
            var result = graphClient.Groups["{group-id}"].Team.PutAsync(requestBody);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
