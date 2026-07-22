using System;

namespace Benzene.Azure.Function.SourceGenerators
{
    /// <summary>
    /// The fully-resolved shape of one trigger function to emit, reduced to strings so the incremental
    /// pipeline can cache on value equality (an unchanged set of declarations re-emits nothing).
    /// </summary>
    internal sealed class TriggerInfo : IEquatable<TriggerInfo>
    {
        public TriggerInfo(
            string className,
            string functionNameLiteral,
            string bindingAttribute,
            string parameterType,
            string parameterName,
            string returnType,
            string dispatchExpression)
        {
            ClassName = className;
            FunctionNameLiteral = functionNameLiteral;
            BindingAttribute = bindingAttribute;
            ParameterType = parameterType;
            ParameterName = parameterName;
            ReturnType = returnType;
            DispatchExpression = dispatchExpression;
        }

        /// <summary>The generated class name (unique within the generated namespace).</summary>
        public string ClassName { get; }

        /// <summary>The quoted C# literal for the Azure Function name (unique across the app).</summary>
        public string FunctionNameLiteral { get; }

        /// <summary>The trigger binding attribute call, without the surrounding brackets, e.g. <c>global::….HttpTrigger(…)</c>.</summary>
        public string BindingAttribute { get; }

        /// <summary>The bound trigger parameter's type, fully qualified.</summary>
        public string ParameterType { get; }

        /// <summary>The bound trigger parameter's name.</summary>
        public string ParameterName { get; }

        /// <summary>The Run method's return type, fully qualified.</summary>
        public string ReturnType { get; }

        /// <summary>The body expression forwarding into the app, e.g. <c>global::….HandleHttpRequest(_app, req)</c>.</summary>
        public string DispatchExpression { get; }

        public bool Equals(TriggerInfo? other) =>
            other is not null
            && ClassName == other.ClassName
            && FunctionNameLiteral == other.FunctionNameLiteral
            && BindingAttribute == other.BindingAttribute
            && ParameterType == other.ParameterType
            && ParameterName == other.ParameterName
            && ReturnType == other.ReturnType
            && DispatchExpression == other.DispatchExpression;

        public override bool Equals(object? obj) => Equals(obj as TriggerInfo);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + ClassName.GetHashCode();
                hash = hash * 31 + FunctionNameLiteral.GetHashCode();
                hash = hash * 31 + BindingAttribute.GetHashCode();
                hash = hash * 31 + ParameterType.GetHashCode();
                hash = hash * 31 + ParameterName.GetHashCode();
                hash = hash * 31 + ReturnType.GetHashCode();
                hash = hash * 31 + DispatchExpression.GetHashCode();
                return hash;
            }
        }
    }
}
