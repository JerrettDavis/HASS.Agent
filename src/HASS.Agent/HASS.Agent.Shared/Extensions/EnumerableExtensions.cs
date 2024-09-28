using System;
using System.Collections.Generic;

namespace HASS.Agent.Shared.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    /// Performs the specified action on each element of the IEnumerable.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the IEnumerable.</typeparam>
    /// <param name="source">The IEnumerable on which to perform the action.</param>
    /// <param name="action">The Action to perform on each element of the IEnumerable.</param>
    public static void ForEach<T>(
        this IEnumerable<T>? source, 
        Action<T>? action)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);

        foreach (var item in source) action(item);
    }
}