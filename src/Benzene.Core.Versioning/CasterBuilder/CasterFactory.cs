using System.Linq.Expressions;
using Benzene.Core.Versioning.Casters;

namespace Benzene.Core.Versioning.CasterBuilder;

public class CasterFactory<TFrom, TTo>
{
    private readonly CasterMapperSettingsBuilder _builder = new();

    public ICaster<TFrom, TTo> Build()
    {
        var mapper = new CasterFuncBuilder(_builder.Build());
        var func = mapper.CreateCasterFunc<TFrom, TTo>();
        return new FuncCaster<TFrom, TTo>(func);
    }

    public CasterFactory<TFrom, TTo> Register<TFromType, TToType>()
    {
        _ = _builder.Register<TFromType, TToType>();
        return this;
    }

    public CasterFactory<TFrom, TTo> RegisterInitValue<TProp>(Expression<Func<TTo, TProp>> memberSelector, TProp value)
    {
        _ = RegisterInitValue(memberSelector, () => value);
        return this;
    }

    public CasterFactory<TFrom, TTo> RegisterInitValue<TProp>(Expression<Func<TTo, TProp>> memberSelector, Func<TProp> func)
    {
        _ = _builder.RegisterInitValue(memberSelector, func);
        return this;
    }

    public CasterFactory<TFrom, TTo> RegisterSubTypeInitValue<TSubType, TProp>(Expression<Func<TSubType, TProp>> memberSelector, TProp value)
    {
        _ = _builder.RegisterInitValue(memberSelector, () => value);
        return this;
    }

    public CasterFactory<TFrom, TTo> RegisterSubTypeInitValue<TSubType, TProp>(Expression<Func<TSubType, TProp>> memberSelector, Func<TProp> func)
    {
        _ = _builder.RegisterInitValue(memberSelector, func);
        return this;
    }

    public CasterFactory<TFrom, TTo> RegisterTypeMapping<TFromType, TToType>()
    {
        _ = _builder.RegisterTypeMapping<TFromType, TToType>();
        return this;
    }
}
