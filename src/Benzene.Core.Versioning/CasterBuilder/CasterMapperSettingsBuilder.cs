using System.Linq.Expressions;

namespace Benzene.Core.Versioning.CasterBuilder;

public class CasterMapperSettingsBuilder
{
    private readonly Dictionary<Type, Type> _typeMapping = new();
    private readonly Dictionary<(Type, Type), Delegate> _funcs = new();
    private readonly Dictionary<Type, Dictionary<string, Func<object?>>> _initValues = new();

    public CasterMapperSettings Build()
    {
        return new CasterMapperSettings
        {
            InitValues = _initValues,
            TypeMapping = _typeMapping,
            Funcs = _funcs
        };
    }

    public CasterMapperSettingsBuilder RegisterTypeMapping<TFrom, TTo>()
    {
        _typeMapping.Add(typeof(TFrom), typeof(TTo));
        return this;
    }

    public CasterMapperSettingsBuilder RegisterInitValue<TTo, TProp>(Expression<Func<TTo, TProp>> memberSelector, Func<TProp> func)
    {
        if (memberSelector.Body is not MemberExpression mex)
        {
            throw new ArgumentException("memberSelector must be a member access", nameof(memberSelector));
        }

        var memberName = mex.Member.Name;
        var toType = typeof(TTo);

        if (!_initValues.TryGetValue(toType, out var dict))
        {
            dict = new Dictionary<string, Func<object?>>();
            _initValues[toType] = dict;
        }

        dict[memberName] = () => func();
        return this;
    }

    public CasterMapperSettingsBuilder RegisterInitValue<TTo, TProp>(Expression<Func<TTo, TProp>> memberSelector, TProp value)
        => RegisterInitValue(memberSelector, () => value);
}
