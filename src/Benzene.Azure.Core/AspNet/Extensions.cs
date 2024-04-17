using Microsoft.AspNetCore.Mvc;

namespace Benzene.Azure.Core.AspNet;

public static class Extensions
{
    public static void EnsureResponseExists(this AspNetContext context)
    {
        context.ContentResult ??= new ContentResult();
    }
}
