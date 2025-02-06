using System.Linq.Expressions;
using System.Reflection;

namespace Benzene.Extras.Patches;

public static class PatchExtensions
{
    public static bool HasField<TUpdateMessage, TValue>(this TUpdateMessage source, Expression<Func<TUpdateMessage, TValue>> getter)
        where TUpdateMessage : IPatchMessage
    {
        var memberExpression = getter.Body as MemberExpression;
        if (memberExpression == null)
        {
            throw new ArgumentException("The expression is not a member access expression.", nameof(getter));
        }

        var fieldName = memberExpression.Member.Name.ToLowerInvariant();

        return source.UpdatedFields.Contains(fieldName);
    }
    
    public static TValue TryGet<TUpdateMessage, TValue>(this TUpdateMessage source, Expression<Func<TUpdateMessage, TValue>> getter, TValue defaultValue)
        where TUpdateMessage : IPatchMessage
    {
        var memberExpression = getter.Body as MemberExpression;
        if (memberExpression == null)
        {
            throw new ArgumentException("The expression is not a member access expression.", nameof(getter));
        }

        var fieldName = memberExpression.Member.Name.ToLowerInvariant();

        if (source.UpdatedFields.Contains(fieldName))
        {
            return getter.Compile().Invoke(source);
        }

        return defaultValue;
    }
    
    

    public static void Set<TUpdateMessage, TValue>(this TUpdateMessage source, Expression<Func<TUpdateMessage, TValue>> setter, TValue value)
        where TUpdateMessage : IPatchMessage
    {
        var memberExpression = setter.Body as MemberExpression;
        if (memberExpression == null)
        {
            throw new ArgumentException("The expression is not a member access expression.", nameof(setter));
        }
        var property = memberExpression.Member as PropertyInfo;
        if (property == null)
        {
            throw new ArgumentException("The member access expression does not access a property.", nameof(setter));
        }

        source.GetType().GetProperty(property.Name)?.SetValue(source, value);
        source.UpdatedFields.Add(property.Name.ToLowerInvariant());
    }
}
