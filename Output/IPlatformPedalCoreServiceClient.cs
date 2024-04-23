using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Clients;
using Benzene.Clients.HealthChecks;
using Benzene.Results;

namespace Platform.Pedal.Core.Client.PlatformPedalCore
{
    public interface IPlatformPedalCoreServiceClient : IHasHealthCheck
    {
        Task<IClientResult<IdMappingDto>> CreateIdmappingPedalAsync(CreateIdMappingMessage message);
        Task<IClientResult<IdMappingDto>> CreateIdmappingPedalAsync(CreateIdMappingMessage message, IDictionary<string, string> headers);
        Task<IClientResult<IdMappingDto>> DeleteIdmappingPedalAsync(DeleteIdMappingMessage message);
        Task<IClientResult<IdMappingDto>> DeleteIdmappingPedalAsync(DeleteIdMappingMessage message, IDictionary<string, string> headers);
        Task<IClientResult<IdMappingDto>> GetIdmappingPedalAsync(GetIdMappingMessage message);
        Task<IClientResult<IdMappingDto>> GetIdmappingPedalAsync(GetIdMappingMessage message, IDictionary<string, string> headers);
        Task<IClientResult<IdMappingDto[]>> GetallIdmappingPedalAsync(GetAllIdMappingsMessage message);
        Task<IClientResult<IdMappingDto[]>> GetallIdmappingPedalAsync(GetAllIdMappingsMessage message, IDictionary<string, string> headers);
        Task<IClientResult<IdMappingDto>> UpdateIdmappingPedalAsync(UpdateIdMappingMessage message);
        Task<IClientResult<IdMappingDto>> UpdateIdmappingPedalAsync(UpdateIdMappingMessage message, IDictionary<string, string> headers);
    }
}
