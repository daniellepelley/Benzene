using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Clients;
using Benzene.Clients.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Results;
using System.Diagnostics.CodeAnalysis;

namespace Platform.Pedal.Core.Client.PlatformPedalCore
{
    [ExcludeFromCodeCoverage]
    public class PlatformPedalCoreServiceClient : IPlatformPedalCoreServiceClient
    {
        private readonly IBenzeneMessageClientFactory _clientFactory;

        public PlatformPedalCoreServiceClient(IBenzeneMessageClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public string HashCode => "7bf877f2cd3fcf63678ed48d368a524cbb5498eb8acc779c6a39434827d67c96";

        public Task<IClientResult<IdMappingDto>> CreateIdmappingPedalAsync(CreateIdMappingMessage message)
        {
            return CreateIdmappingPedalAsync(message, null);
        }

        public async Task<IClientResult<IdMappingDto>> CreateIdmappingPedalAsync(CreateIdMappingMessage message, IDictionary<string, string> headers)
        {
            using (var client = _clientFactory.Create("PlatformPedalCore", "pedal:idmapping:create"))
            {
                return await client.SendMessageAsync<CreateIdMappingMessage, IdMappingDto>("pedal:idmapping:create", message, headers);
            }
        }

        public Task<IClientResult<IdMappingDto>> DeleteIdmappingPedalAsync(DeleteIdMappingMessage message)
        {
            return DeleteIdmappingPedalAsync(message, null);
        }

        public async Task<IClientResult<IdMappingDto>> DeleteIdmappingPedalAsync(DeleteIdMappingMessage message, IDictionary<string, string> headers)
        {
            using (var client = _clientFactory.Create("PlatformPedalCore", "pedal:idmapping:delete"))
            {
                return await client.SendMessageAsync<DeleteIdMappingMessage, IdMappingDto>("pedal:idmapping:delete", message, headers);
            }
        }

        public Task<IClientResult<IdMappingDto>> GetIdmappingPedalAsync(GetIdMappingMessage message)
        {
            return GetIdmappingPedalAsync(message, null);
        }

        public async Task<IClientResult<IdMappingDto>> GetIdmappingPedalAsync(GetIdMappingMessage message, IDictionary<string, string> headers)
        {
            using (var client = _clientFactory.Create("PlatformPedalCore", "pedal:idmapping:get"))
            {
                return await client.SendMessageAsync<GetIdMappingMessage, IdMappingDto>("pedal:idmapping:get", message, headers);
            }
        }

        public Task<IClientResult<IdMappingDto[]>> GetallIdmappingPedalAsync(GetAllIdMappingsMessage message)
        {
            return GetallIdmappingPedalAsync(message, null);
        }

        public async Task<IClientResult<IdMappingDto[]>> GetallIdmappingPedalAsync(GetAllIdMappingsMessage message, IDictionary<string, string> headers)
        {
            using (var client = _clientFactory.Create("PlatformPedalCore", "pedal:idmapping:getall"))
            {
                return await client.SendMessageAsync<GetAllIdMappingsMessage, IdMappingDto[]>("pedal:idmapping:getall", message, headers);
            }
        }

        public Task<IClientResult<IdMappingDto>> UpdateIdmappingPedalAsync(UpdateIdMappingMessage message)
        {
            return UpdateIdmappingPedalAsync(message, null);
        }

        public async Task<IClientResult<IdMappingDto>> UpdateIdmappingPedalAsync(UpdateIdMappingMessage message, IDictionary<string, string> headers)
        {
            using (var client = _clientFactory.Create("PlatformPedalCore", "pedal:idmapping:update"))
            {
                return await client.SendMessageAsync<UpdateIdMappingMessage, IdMappingDto>("pedal:idmapping:update", message, headers);
            }
        }

        public async Task<IClientResult<HealthCheckResponse>> HealthCheckAsync()
        {
            using (var client = _clientFactory.Create("PlatformPedalCore", "healthcheck"))
            {
                var result = await client.SendMessageAsync<NullPayload, HealthCheckResponse>("healthcheck", new NullPayload(), null);
                return result.Status != ClientResultStatus.Ok
                    ? result
                    : ClientResult.Ok(ClientHealthCheckProcessor.Process(result.Payload, HashCode) as HealthCheckResponse);
            }
        }
    }
}
