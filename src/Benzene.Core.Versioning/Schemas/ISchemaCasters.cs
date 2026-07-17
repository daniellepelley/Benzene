namespace Benzene.Core.Versioning.Schemas;

public interface ISchemaCasters
{
    ISchemaCaster[] GetAll();

    ISchemaCaster GetSchemaCaster(string fromSchema, string toSchema, string topic);
}

