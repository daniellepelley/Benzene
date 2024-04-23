using System;
using System.Diagnostics.CodeAnalysis;

namespace Platform.Pedal.Core.Client.PlatformPedalCore
{
    [ExcludeFromCodeCoverage]
    public class GetAllIdMappingsMessage
    {
        public PaginationMessage Pagination { get; set; }
        public string Id { get; set; }
        public string EntityName { get; set; }
        public string Target { get; set; }
        public Guid? TenantId { get; set; }
    }
}
