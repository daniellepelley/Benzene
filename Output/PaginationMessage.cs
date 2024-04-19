using System;
using System.Diagnostics.CodeAnalysis;

namespace Platform.Pedal.Core.Client.PlatformPedalCore
{
    [ExcludeFromCodeCoverage]
    public class PaginationMessage
    {
        public int? PageNumber { get; set; }
        public int? ItemsPerPage { get; set; }
    }
}
