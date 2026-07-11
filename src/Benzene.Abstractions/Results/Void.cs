namespace Benzene.Abstractions.Results;

/// <summary>
/// Represents the absence of a meaningful payload in a result.
/// Use this type for handlers that perform an action but do not return data (e.g., commands that acknowledge success without a response body).
/// </summary>
/// <remarks>
/// This is a class (not struct) to ensure it can be used with nullable reference types and generic constraints.
/// Similar to F#'s unit type or void in C, but usable in generic type parameters.
/// </remarks>
public class Void {}
