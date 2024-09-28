using System;

namespace HASS.Agent.Shared.Extensions;

public static class GenericExtensions
{
    /// <summary>
    /// Applies the provided function to the given source object if it is not null, 
    /// and returns the result. If the source is null, the default value of TResult is returned.
    /// </summary>
    /// <typeparam name="TSource">The type of the source object, constrained to reference types.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the function.</typeparam>
    /// <param name="source">The source object to which the function is applied, if not null.</param>
    /// <param name="func">A function that accepts a non-null source object and returns a result of type TResult.</param>
    /// <returns>
    /// The result of applying the function to the source object, or the default value of TResult if the source is null.
    /// </returns>
    public static TResult? Let<TSource, TResult>(
        this TSource? source, 
        Func<TSource, TResult> func)
        where TSource : class =>
        source != null ? func(source) : default;


    /// <summary>
    /// Executes the specified action if the given source object is not null.
    /// </summary>
    /// <typeparam name="TSource">The type of the source object, constrained to reference types.</typeparam>
    /// <param name="source">The source object on which the action is performed, if not null.</param>
    /// <param name="action">The action to execute on the source object.</param>
    /// <remarks>
    /// This method provides a safe way to perform an action on an object without explicitly checking for null.
    /// If the source is null, the action is not invoked.
    /// </remarks>
    public static void Let<TSource>(
        this TSource? source,
        Action<TSource> action)
        where TSource : class
    {
        if (source != null) action(source);
    }
        
}