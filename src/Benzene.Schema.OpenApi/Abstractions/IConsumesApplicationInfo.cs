using Benzene.Abstractions.Info;

namespace Benzene.Schema.OpenApi.Abstractions;

public interface IConsumesApplicationInfo<out TBuilder>
{
    TBuilder AddApplicationInfo(IApplicationInfo applicationInfo);
}

public interface IProducesYaml
{
    string GenerateYaml();
}

public interface IProducesJson
{
    string GenerateJson();
}

