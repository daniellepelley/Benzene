using System;

namespace Benzene.Clients.Aws.Lambda
{
    /// <summary>
    /// Thrown when a <see cref="Amazon.Lambda.InvocationType.RequestResponse"/> invocation returns with
    /// <c>InvokeResponse.FunctionError</c> set - i.e. the target function threw. AWS returns HTTP 200 in
    /// that case with an error object (<c>errorMessage</c>/<c>errorType</c>/<c>stackTrace</c>) as the
    /// payload rather than the function's normal output, so treating the payload as the expected response
    /// would silently mis-deserialize a failure into a success. Raising this instead lets the caller
    /// surface it as a failure result.
    /// </summary>
    public class AwsLambdaFunctionErrorException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AwsLambdaFunctionErrorException"/> class.
        /// </summary>
        /// <param name="functionName">The invoked function's name.</param>
        /// <param name="functionError">The <c>FunctionError</c> value (<c>"Handled"</c>/<c>"Unhandled"</c>).</param>
        /// <param name="errorPayload">The raw error payload the function returned.</param>
        public AwsLambdaFunctionErrorException(string functionName, string functionError, string errorPayload)
            : base($"Lambda function {functionName} reported a {functionError} error: {errorPayload}")
        {
            FunctionName = functionName;
            FunctionError = functionError;
            ErrorPayload = errorPayload;
        }

        /// <summary>Gets the invoked function's name.</summary>
        public string FunctionName { get; }

        /// <summary>Gets the <c>FunctionError</c> value (<c>"Handled"</c> or <c>"Unhandled"</c>).</summary>
        public string FunctionError { get; }

        /// <summary>Gets the raw error payload the function returned.</summary>
        public string ErrorPayload { get; }
    }
}
