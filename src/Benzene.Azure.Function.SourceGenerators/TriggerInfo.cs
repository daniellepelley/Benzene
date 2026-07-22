using System;

namespace Benzene.Azure.Function.SourceGenerators
{
    /// <summary>
    /// The fully-resolved shape of one trigger function to emit, reduced to strings so the incremental
    /// pipeline can cache on value equality (an unchanged set of declarations re-emits nothing). Each
    /// transport builds its own <see cref="ParameterList"/> and <see cref="DispatchExpression"/>, so
    /// two-parameter (Blob) and generic (Cosmos) shapes fit the same model.
    /// </summary>
    internal sealed class TriggerInfo : IEquatable<TriggerInfo>
    {
        public TriggerInfo(
            string className,
            string functionNameLiteral,
            string parameterList,
            string returnType,
            string dispatchExpression)
        {
            ClassName = className;
            FunctionNameLiteral = functionNameLiteral;
            ParameterList = parameterList;
            ReturnType = returnType;
            DispatchExpression = dispatchExpression;
        }

        /// <summary>The generated class name (unique within the generated namespace).</summary>
        public string ClassName { get; }

        /// <summary>The quoted C# literal for the Azure Function name (unique across the app).</summary>
        public string FunctionNameLiteral { get; }

        /// <summary>The full parameter list of the generated <c>Run</c> method, incl. the binding attribute(s).</summary>
        public string ParameterList { get; }

        /// <summary>The <c>Run</c> method's return type, fully qualified.</summary>
        public string ReturnType { get; }

        /// <summary>The body expression forwarding into the app, e.g. <c>global::….HandleHttpRequest(_app, req)</c>.</summary>
        public string DispatchExpression { get; }

        public bool Equals(TriggerInfo? other) =>
            other is not null
            && ClassName == other.ClassName
            && FunctionNameLiteral == other.FunctionNameLiteral
            && ParameterList == other.ParameterList
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
                hash = hash * 31 + ParameterList.GetHashCode();
                hash = hash * 31 + ReturnType.GetHashCode();
                hash = hash * 31 + DispatchExpression.GetHashCode();
                return hash;
            }
        }
    }
}
