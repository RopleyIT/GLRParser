using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Parsing
{
    /// <summary>
    /// Simple interface for use with ?
    /// multiplicity, i.e. zero or one.
    /// </summary>
    /// <typeparam name="T">The type of item that is optional</typeparam>

    public interface IOptional<T>
    {
        /// <summary>
        /// Find out if the optional
        /// value is present
        /// </summary>

        bool HasValue
        {
            get;
        }

        /// <summary>
        /// Get the optional value
        /// if present. If not,
        /// return default(T).
        /// </summary>

        T Value
        {
            get;
        }
    }

    /// <summary>
    /// Wrap a List(Of Object) as a
    /// List(Of T), by implementing the
    /// IList(Of T) interface. This class
    /// is provided to make access to the value
    /// of a $N parameter in an action function
    /// straightforward if the parameter is for
    /// a multiplicity token (e.g. token*, 
    /// token+ or token?)
    /// </summary>
    /// <typeparam name="T">The type to assume
    /// each object in the list will have</typeparam>

    public class ListArg<T> : IList<T>, IOptional<T>
    {
        /// <summary>
        /// The list of object references being wrapped
        /// </summary>

        public IList<object> WrappedList
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructor. Used when the argument is passed
        /// as one of the $N parameters from the parser.
        /// Given a List(of Object), place a typesafe
        /// wrapper around it that assumes all
        /// elements are of type T.
        /// </summary>
        /// <param name="arg">The argument to a parser action
        /// function to be treated as a collection</param>

        public ListArg(object arg)
        {
            WrappedList = arg as IList<object>;
            if (WrappedList == null)
                throw new ArgumentException
                    ("ListArg needs a non-null List<object> to be wrapped");
        }

        /// <summary>
        /// Used when the WrappedList wraps an optional argument.
        /// True if there is (at least) one argument in the list.
        /// </summary>

        public bool HasValue => WrappedList.Count > 0;

        /// <summary>
        /// Return the value from the first slot in the
        /// list. If there is no value in the list,
        /// return the default value for type T.
        /// </summary>

        public T Value => (T)(WrappedList.FirstOrDefault());

        /// <summary>
        /// Find the index of the item with this
        /// object reference in the list
        /// </summary>
        /// <param name="item">The item whose index we want</param>
        /// <returns>The index of the item</returns>

        public int IndexOf(T item) => WrappedList.IndexOf(item);

        /// <summary>
        /// Insert the specified item at the
        /// selected offset into the list. All
        /// subsequent item's will have their
        /// indices increased by one, including
        /// the previous item at the selected offset.
        /// </summary>
        /// <param name="index">The offset at which to
        /// insert the item</param>
        /// <param name="item">The item to be inserted</param>

        public void Insert(int index, T item) => WrappedList.Insert(index, item);

        /// <summary>
        /// Remove the item at the selected index
        /// from the list. Items to the right
        /// move one index value down to remove
        /// the gap in the list.
        /// </summary>
        /// <param name="index">Index from which
        /// to remove an item</param>

        public void RemoveAt(int index) => WrappedList.RemoveAt(index);

        /// <summary>
        /// Indexer into the list
        /// </summary>
        /// <param name="index">Offset at which
        /// to inspect or overwrite an item</param>
        /// <returns>The item at the selected
        /// index, with the wrapper type.</returns>

        public T this[int index]
        {
            get
            {
                object o = WrappedList[index];
                return (T)o;
            }
            set => WrappedList[index] = value;
        }

        /// <summary>
        /// Append a new item of type T
        /// to the end of the list.
        /// </summary>
        /// <param name="item">The item to be appended</param>

        public void Add(T item) => WrappedList.Add(item);

        public void Clear() => WrappedList.Clear();

        /// <summary>
        /// Find out if the specified item
        /// is within the list.
        /// </summary>
        /// <param name="item">The item to seek</param>
        /// <returns>True if the item is in the list</returns>

        public bool Contains(T item) => WrappedList.Contains(item);

        /// <summary>
        /// Copy the entire contents of the list out
        /// to a previously allocated array of
        /// object references.
        /// </summary>
        /// <param name="array">The target array to copy to</param>
        /// <param name="arrayIndex">The starting position
        /// in the target array at which to begin the
        /// copy</param>

        public void CopyTo(T[] array, int arrayIndex)
        {
            // Implement the exceptions defined for IList<T>.CopyTo()

            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (WrappedList.Count > array.Length - arrayIndex)
                throw new ArgumentException("Not enough space in array to copy list");

            foreach (object o in WrappedList)
            {
                array[arrayIndex++] = (T)o;
            }
        }

        /// <summary>
        /// The number of items in the list.
        /// </summary>

        public int Count => WrappedList.Count;

        /// <summary>
        /// The wrapped list is not read only, so
        /// this property always returns false.
        /// </summary>

        public bool IsReadOnly => false;

        /// <summary>
        /// Remove the item from the list
        /// </summary>
        /// <param name="item">Reference to the
        /// item to be removed.</param>
        /// <returns>True if the removal was
        /// successful. False if the item
        /// was not in the list.</returns>

        public bool Remove(T item) => WrappedList.Remove(item);

        /// <summary>
        /// Implementation of the IEnumerable(Of T)
        /// interface"/>
        /// </summary>
        /// <returns>Typed enumerator</returns>

        public IEnumerator<T> GetEnumerator() => WrappedList.Select(o => (T)o).GetEnumerator();

        /// <summary>
        /// Implementation of the IEnumerable interface
        /// </summary>
        /// <returns>Non-generic enumerator</returns>

        IEnumerator IEnumerable.GetEnumerator() => WrappedList.GetEnumerator();
    }
}
