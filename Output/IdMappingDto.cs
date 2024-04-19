using System;
using System.Diagnostics.CodeAnalysis;

namespace Platform.Pedal.Core.Client.PlatformPedalCore
{
    [ExcludeFromCodeCoverage]
    public class IdMappingDto
    {
        public string Id { get; set; }
        public string EntityName { get; set; }
        public string Target { get; set; }
        public string TargetId { get; set; }
        public string TenantId { get; set; }
    }
}
