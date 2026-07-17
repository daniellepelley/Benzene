using System.Linq.Expressions;
using System.Reflection;

namespace Benzene.Core.Versioning.CasterBuilder;

public class CasterFuncBuilder
{
    private readonly SchemaTypeMatcher _schemaTypeMatcher;
    private readonly CasterMapperSettings _settings;
    private readonly Dictionary<(Type, Type), Delegate> _funcs;

    public CasterFuncBuilder(CasterMapperSettings settings)
    {
        _settings = settings;
        _funcs = _settings.Funcs.ToDictionary();
        _schemaTypeMatcher = new SchemaTypeMatcher(_settings.TypeMapping);
    }

    public Func<TFrom, TTo> CreateCasterFunc<TFrom, TTo>()
    {
        var fromType = typeof(TFrom);
        var toType = typeof(TTo);

        if (_funcs.ContainsKey((fromType, toType)))
        {
            return (Func<TFrom, TTo>)_funcs[(fromType, toType)];
        }

        var srcParam = Expression.Parameter(fromType, "src");
        var body = BuildMappingExpression(srcParam, fromType, toType);

        var lambda = Expression.Lambda<Func<TFrom, TTo>>(body, srcParam);
        var func = lambda.Compile();

        _funcs.Add((fromType, toType), func);

        return func;
    }

    private Expression BuildMappingExpression(Expression fromExpression, Type fromType, Type toType)
    {
        if (IsList(fromType, toType))
        {
            return CreateListExpression(fromExpression, fromType, toType);
        }

        if (IsBaseType(fromType))
        {
            return CreateBaseTypeExpression(fromExpression, fromType, toType);
        }

        return BuildClassMappingExpression(fromExpression, fromType, toType);
    }

    private MemberInitExpression BuildClassMappingExpression(Expression fromExpression, Type fromType, Type toType)
    {
        var derivedType = _schemaTypeMatcher.TryGetType(fromType, toType);

        var toInstance = Expression.New(derivedType);

        var bindings = CreatePropertyExpressions(fromExpression, fromType, derivedType)
            .Cast<MemberBinding>();

        return Expression.MemberInit(toInstance, bindings);
    }

    private MemberAssignment[] CreatePropertyExpressions(Expression fromExpression, Type fromType, Type toType)
    {
        _ = _settings.InitValues.TryGetValue(toType, out var initValueDictionary);

        return GetProperties(fromType, toType)
            .Select(properties =>
            {
                var fromProperty = properties.Item1;
                var toProperty = properties.Item2;

                if (initValueDictionary != null && initValueDictionary.TryGetValue(toProperty.Name, out var func))
                {
                    var invokeExpression = Expression.Invoke(Expression.Constant(func));
                    var convertedValue = Expression.Convert(invokeExpression, toProperty.PropertyType);
                    return Expression.Bind(toProperty, convertedValue);
                }

                if (fromProperty.PropertyType == toProperty.PropertyType)
                {
                    if (IsNullable(fromProperty, toProperty))
                    {
                        var fromValue = Expression.Property(fromExpression, fromProperty);
                        var toValue = Expression.Convert(fromValue, toProperty.PropertyType);
                        return Expression.Bind(toProperty, toValue);
                    }
                    else
                    {
                        var fromValue = Expression.Property(fromExpression, fromProperty);
                        return Expression.Bind(toProperty, fromValue);
                    }
                }

                if (IsEnumerable(fromProperty, toProperty))
                {
                    var enumerableExpression = CreateEnumerableExpression(fromExpression, fromProperty, toProperty);
                    return Expression.Bind(toProperty, enumerableExpression);
                }

                if (IsEnum(fromProperty, toProperty))
                {
                    return CreateEnumExpression(fromExpression, fromProperty, toProperty);
                }

                if (IsBaseType(fromProperty.PropertyType))
                {
                    var expression = CreateBaseTypeExpression(
                        Expression.Property(fromExpression, fromProperty), fromProperty.PropertyType,
                        toProperty.PropertyType);
                    return Expression.Bind(toProperty, expression);
                }

                var classExpression = CreateClassExpression(fromExpression, fromProperty, toProperty);
                return Expression.Bind(toProperty, classExpression);
            }).ToArray();
    }

    private ConditionalExpression CreateClassExpression(Expression fromExpression, PropertyInfo fromProperty,
        PropertyInfo toProperty)
    {
        var mapDelegate = MapDelegate(fromProperty.PropertyType, toProperty.PropertyType);
        var fromValue = Expression.Property(fromExpression, fromProperty);
        var isNull = Expression.Equal(fromValue,
            Expression.Constant(null, fromProperty.PropertyType));
        var defaultValue = Expression.Default(toProperty.PropertyType);
        var mapCall = Expression.Invoke(Expression.Constant(mapDelegate), fromValue);
        var toValue = Expression.Condition(isNull, defaultValue, mapCall);
        return toValue;
    }

    private ConditionalExpression CreateEnumerableExpression(Expression fromExpression, PropertyInfo fromProperty,
        PropertyInfo toProperty)
    {
        var fromElementType = fromProperty.PropertyType.IsGenericType
            ? fromProperty.PropertyType.GetGenericArguments()[0]
            : typeof(object);
        var toElementType = toProperty.PropertyType.IsGenericType
            ? toProperty.PropertyType.GetGenericArguments()[0]
            : typeof(object);
        var mapDelegate = MapDelegate(fromElementType, toElementType);
        var fromValue = Expression.Property(fromExpression, fromProperty);
        var isNull = Expression.Equal(fromValue, Expression.Constant(null, fromProperty.PropertyType));
        var defaultValue = Expression.Constant(null, toProperty.PropertyType);
        var mapCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Select),
            [fromElementType, toElementType],
            fromValue,
            Expression.Constant(mapDelegate, typeof(Func<,>).MakeGenericType(fromElementType, toElementType))
        );
        var convertedCollection = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.ToList),
            [toElementType],
            mapCall
        );
        var toValue = Expression.Condition(isNull, defaultValue, convertedCollection);
        return toValue;
    }

    private static (PropertyInfo, PropertyInfo)[] GetProperties(Type fromType, Type toType)
    {
        return toType.GetProperties()
            .Where(prop => prop.CanWrite)
            .Select(toProp => (FromProperty: fromType.GetProperty(toProp.Name), ToProperty: toProp))
            .Where(props => props.FromProperty != null && props.FromProperty.CanRead)
            .Select(tuple => (tuple.FromProperty!, tuple.ToProperty))
            .ToArray();
    }

    private static bool IsEnumerable(PropertyInfo fromProp, PropertyInfo toProp) =>
        typeof(IEnumerable<>).IsAssignableFrom(fromProp.PropertyType) &&
        typeof(IEnumerable<>).IsAssignableFrom(toProp.PropertyType);

    private static MemberAssignment CreateEnumExpression(Expression fromExpression, PropertyInfo fromProp,
        PropertyInfo toProp)
    {
        var fromValue = Expression.Property(fromExpression, fromProp);
        var toValue = Expression.Convert(fromValue, toProp.PropertyType);
        return Expression.Bind(toProp, toValue);
    }

    private static bool IsEnum(PropertyInfo fromProp, PropertyInfo toProp) =>
        fromProp.PropertyType.IsEnum && toProp.PropertyType.IsEnum ||
        Nullable.GetUnderlyingType(fromProp.PropertyType)?.IsEnum == true &&
         Nullable.GetUnderlyingType(toProp.PropertyType)?.IsEnum == true;

    private static bool IsNullable(PropertyInfo fromProp, PropertyInfo toProp) =>
        Nullable.GetUnderlyingType(fromProp.PropertyType) != null &&
        Nullable.GetUnderlyingType(toProp.PropertyType) != null;

    private Expression CreateBaseTypeExpression(Expression fromExpr, Type fromType, Type toType)
    {
        var derivedTypes = GetDerivedTypes(fromType).ToArray();
        if (derivedTypes.Length == 0)
        {
            throw new InvalidOperationException($"No derived types found for abstract type {fromType.FullName}.");
        }
        Expression? conditionalExpr = null;
        foreach (var derivedType in derivedTypes)
        {
            var toDerivedType = _schemaTypeMatcher.GetType(derivedType, toType);

            if (toDerivedType == null)
            {
                continue;
            }

            var typeCheck = Expression.TypeIs(fromExpr, derivedType);

            var derivedMappingExpr = BuildClassMappingExpression(
                Expression.Convert(fromExpr, derivedType), derivedType, toDerivedType);

            var convertedDerivedExpr = Expression.Convert(derivedMappingExpr, toType);

            conditionalExpr = conditionalExpr == null
                ? Expression.Condition(typeCheck, convertedDerivedExpr, Expression.Default(toType))
                : Expression.Condition(typeCheck, convertedDerivedExpr, conditionalExpr);
        }
        return conditionalExpr!;
    }

    private MethodCallExpression CreateListExpression(Expression fromExpr, Type fromType, Type toType)
    {
        var fromElementType = fromType.GetGenericArguments()[0];
        var toElementType = toType.GetGenericArguments()[0];
        var mapDelegate = MapDelegate(fromElementType, toElementType);
        var selectMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
            .MakeGenericMethod(fromElementType, toElementType);
        var mapCall = Expression.Call(
            selectMethod,
            fromExpr,
            Expression.Constant(mapDelegate)
        );
        var toListMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "ToList" && m.IsGenericMethod)
            .MakeGenericMethod(toElementType);
        var convertedCollection = Expression.Call(toListMethod, mapCall);
        return convertedCollection;
    }

    private static bool IsList(Type fromType, Type toType) =>
        fromType.IsGenericType && fromType.GetGenericTypeDefinition() == typeof(List<>) &&
        toType.IsGenericType && toType.GetGenericTypeDefinition() == typeof(List<>);

    private static bool IsBaseType(Type toType)
    {
        if (toType.IsAbstract)
        {
            return true;
        }
        var inheritors = toType.Assembly.GetTypes()
            .Where(type => type != toType && type.IsClass && !type.IsAbstract && toType.IsAssignableFrom(type))
            .ToArray();
        return inheritors.Length > 0;
    }

    private object? MapDelegate(Type fromType, Type toType)
    {
        var createMapDelegateMethod =
            typeof(CasterFuncBuilder)
                .GetMethod(nameof(CreateCasterFunc))!
                .MakeGenericMethod(fromType, toType);
        var mapDelegate = createMapDelegateMethod.Invoke(this, null);
        return mapDelegate;
    }

    private static IEnumerable<Type> GetDerivedTypes(Type baseType)
    {
        var types = baseType.Assembly.GetTypes()
            .Where(t => t != baseType && t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t));

        if (!baseType.IsAbstract)
        {
            // Prepend the base type so it is processed first (innermost check).
            // The loop builds expressions with each iteration becoming the outermost
            // conditional, so less-specific types must be processed before more-specific
            // ones. Appending the base type would make "is ValuationGrouping" match
            // before "is AggregateValuationGrouping", swallowing all subtype instances.
            return
            [
                baseType,
                ..types,
            ];
        }

        return types;
    }
}
