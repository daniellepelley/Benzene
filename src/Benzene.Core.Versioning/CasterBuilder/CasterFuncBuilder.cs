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

        // Init values bind on every writable target property they were registered for - including
        // properties newly added in the target schema, which have no source counterpart to map from.
        var initValueBindings = toType.GetProperties()
            .Where(prop => prop.CanWrite && initValueDictionary != null && initValueDictionary.ContainsKey(prop.Name))
            .Select(toProperty =>
            {
                var func = initValueDictionary![toProperty.Name];
                var invokeExpression = Expression.Invoke(Expression.Constant(func));
                var convertedValue = Expression.Convert(invokeExpression, toProperty.PropertyType);
                return Expression.Bind(toProperty, convertedValue);
            });

        var mappedBindings = GetProperties(fromType, toType)
            .Where(properties => initValueDictionary == null || !initValueDictionary.ContainsKey(properties.Item2.Name))
            .Select(properties =>
            {
                var fromProperty = properties.Item1;
                var toProperty = properties.Item2;

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

                // Value type whose nullability changed between versions (int->int?, int?->int, and
                // non-null enum<->nullable enum) - the property types differ, so the equal-type block
                // above misses it, and without this it fell through to class-mapping and came back
                // `default`, silently dropping the value on the very common "make a field optional"
                // schema change. Gated on a matching underlying type, so a genuinely different enum
                // type (V1.Status vs V2.Status) still falls through to value-based enum casting below.
                var fromUnderlying = Nullable.GetUnderlyingType(fromProperty.PropertyType) ?? fromProperty.PropertyType;
                var toUnderlying = Nullable.GetUnderlyingType(toProperty.PropertyType) ?? toProperty.PropertyType;
                if (fromUnderlying == toUnderlying && fromUnderlying.IsValueType)
                {
                    var fromValue = Expression.Property(fromExpression, fromProperty);
                    var toValue = Nullable.GetUnderlyingType(toProperty.PropertyType) != null
                        ? (Expression)Expression.Convert(fromValue, toProperty.PropertyType)          // ->Nullable<T>: wrap
                        : Expression.Coalesce(fromValue, Expression.Default(toProperty.PropertyType)); // Nullable<T>->T: value or default, never throw
                    return Expression.Bind(toProperty, toValue);
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
            });

        return initValueBindings.Concat(mappedBindings).ToArray();
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
        var fromElementType = GetEnumerableElementType(fromProperty.PropertyType);
        var toElementType = GetEnumerableElementType(toProperty.PropertyType);
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
        // Materialize as an array when the target property is an array (T[] has no parameterless ctor,
        // so the old class-mapping fall-through threw at startup), otherwise a List (assignable to
        // List<T>/IEnumerable<T>/IReadOnlyList<T>).
        var convertedCollection = Expression.Call(
            typeof(Enumerable),
            toProperty.PropertyType.IsArray ? nameof(Enumerable.ToArray) : nameof(Enumerable.ToList),
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
        IsEnumerableType(fromProp.PropertyType) && IsEnumerableType(toProp.PropertyType);

    // An array (T[]) or a single-generic-argument type assignable to IEnumerable<T> (List<T>,
    // IEnumerable<T>, IReadOnlyList<T>, Collection<T>...). Arrays were previously excluded (they are
    // not IsGenericType), so an array property with a changed element type fell through to
    // class-mapping and threw Expression.New(T[]) at startup.
    private static bool IsEnumerableType(Type type) =>
        type.IsArray || IsGenericEnumerable(type);

    private static Type GetEnumerableElementType(Type type) =>
        type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];

    // A single-generic-argument type assignable to IEnumerable<T> (List<T>, IEnumerable<T>,
    // IReadOnlyList<T>, Collection<T>...). Excludes string and dictionaries.
    private static bool IsGenericEnumerable(Type type) =>
        type.IsGenericType &&
        type.GetGenericArguments().Length == 1 &&
        typeof(IEnumerable<>).MakeGenericType(type.GetGenericArguments()[0]).IsAssignableFrom(type);

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

        return conditionalExpr
            ?? throw new InvalidOperationException(
                $"No target types could be matched for any type derived from {fromType.FullName} when mapping to {toType.FullName}.");
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
            // ones. Appending the base type would make "is BaseType" match before
            // "is DerivedType", swallowing all subtype instances.
            return
            [
                baseType,
                ..types,
            ];
        }

        return types;
    }
}
