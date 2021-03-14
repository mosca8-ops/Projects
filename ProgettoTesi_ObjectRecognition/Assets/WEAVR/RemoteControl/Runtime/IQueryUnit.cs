using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.RemoteControl
{
    /// <summary>
    /// Search type to narrow down the querying
    /// </summary>
    [Flags]
    public enum QuerySearchType
    {
        /// <summary>
        /// Zero out for flags
        /// </summary>
        None = 0,
        /// <summary>
        /// Should search in scene
        /// </summary>
        Scene = 1 << 0,
        /// <summary>
        /// Should search in procedure
        /// </summary>
        Procedure = 1 << 1,
        /// <summary>
        /// Should search in interactions
        /// </summary>
        Interaction = 1 << 2,
        /// <summary>
        /// Should search in resources
        /// </summary>
        Resource = 1 << 3,
        /// <summary>
        /// Should search by guid
        /// </summary>
        Guid = 1 << 4,

        /// <summary>
        /// Special searches (e.g. properties in objects or static values)
        /// </summary>
        Generic = 1 << 7,
    }

    /// <summary>
    /// Interface for handling querying by various units
    /// </summary>
    public interface IQueryUnit
    {
        /// <summary>
        /// The name of the unit, for special cases where a generic search is required
        /// </summary>
        string UnitName { get; }

        /// <summary>
        /// Whether it can handle the query search type
        /// </summary>
        /// <param name="searchType">The search type to be handled</param>
        /// <returns>If it can handle the search type or not</returns>
        bool CanHandleSearchType(QuerySearchType searchType);

        /// <summary>
        /// Search based on search string
        /// </summary>
        /// <typeparam name="T">The type of returned result</typeparam>
        /// <param name="searchType">The search type</param>
        /// <param name="searchString">The search string</param>
        /// <returns>A query wrapper for the results</returns>
        IQuery<T> Query<T>(QuerySearchType searchType, string searchString);

        /// <summary>
        /// Search based on search string
        /// </summary>
        /// <typeparam name="T">The type of returned result</typeparam>
        /// <param name="searchType">The search type</param>
        /// <param name="searchString">The search string</param>
        /// <param name="options">The string comparing options</param>
        /// <returns>A query wrapper for the results</returns>
        IQuery<T> Query<T>(QuerySearchType searchType, string searchString, CompareOptions options);

        /// <summary>
        /// Search based on search string
        /// </summary>
        /// <typeparam name="T">The type of returned result</typeparam>
        /// <param name="searchType">The search type</param>
        /// <param name="searchFunction">The search function for each element</param>
        /// <returns>A query wrapper for the results</returns>
        IQuery<T> Query<T>(QuerySearchType searchType, Func<T, bool> searchFunction);
    }

    /// <summary>
    /// The interface to handle query results
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IQuery<T>
    {
        /// <summary>
        /// The <see cref="IQueryUnit"/> unit which created this query
        /// </summary>
        IQueryUnit Creator { get; }

        /// <summary>
        /// Convert the query to a list
        /// </summary>
        /// <returns>The list with all the elements in the query</returns>
        IList<T> ToList();

        /// <summary>
        /// The first element in the query
        /// </summary>
        /// <returns></returns>
        T First();

        /// <summary>
        /// The first element in the query which satisfies the predicate
        /// </summary>
        /// <param name="predicate">The predicate to identify the element</param>
        /// <returns>The first element by the predicate</returns>
        T First(Func<T, bool> predicate);

        /// <summary>
        /// The last element in the query
        /// </summary>
        /// <returns>The last element</returns>
        T Last();

        /// <summary>
        /// The last element in the query which satisfies the predicate
        /// </summary>
        /// <param name="predicate">The predicate to identify the element</param>
        /// <returns>The last element by the predicate</returns>
        T Last(Func<T, bool> predicate);

        /// <summary>
        /// Get the element at the specified index.
        /// </summary>
        /// <remarks>Index can be negative, which means it can start from the end</remarks>
        /// <param name="index">The index of the element. Can be negative which means it start from the end</param>
        /// <returns>The element found at specified index, or default if not found</returns>
        T GetElementAt(int index);

        /// <summary>
        /// Whether it has any element in the query or not
        /// </summary>
        /// <returns>True if there are elements in the query</returns>
        bool HasAny();

        /// <summary>
        /// Gets the enumerator for this query. Useful for foreach loop
        /// </summary>
        /// <returns>The enumerator of this query</returns>
        IEnumerator<T> GetEnumerator();

        /// <summary>
        /// Whether this query is still valid or should be discarded. Used mainly for caching purposes
        /// </summary>
        bool IsStillValid { get; set; }
    }

    /// <summary>
    /// A query which represents an empty result
    /// </summary>
    /// <typeparam name="T">The type of the element</typeparam>
    public class EmptyQuery<T> : IQuery<T>
    {
        public static readonly EmptyQuery<T> Shared = new EmptyQuery<T>();

        public IQueryUnit Creator { get; set; }

        public T First() => default;

        public T First(Func<T, bool> predicate) => default;

        public IEnumerator<T> GetEnumerator() => default;

        public bool HasAny() => false;

        public bool IsStillValid { get; set; } = false;

        public T Last() => default;

        public T Last(Func<T, bool> predicate) => default;

        public IList<T> ToList() => new List<T>();

        public T GetElementAt(int index) => default;
    }
}
