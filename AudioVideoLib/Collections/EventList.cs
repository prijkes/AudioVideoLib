/*
 * Date: 2012-12-21
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AudioVideoLib.Collections
{
    /// <summary>
    /// Represents a strongly typed list of objects that can be accessed by index, manipulate lists, and add events to observe changes to the list.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public class EventList<T> : IList<T>
    {
        private readonly List<T> _list;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="EventList{T}"/> class that is empty and has the default initial capacity.
        /// </summary>
        public EventList()
        {
          _list = new List<T>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventList{T}"/> class that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0. </exception>
        public EventList(int capacity)
        {
          _list = new List<T>(capacity);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Occurs before an item is added to the list
        /// </summary>
        public event EventHandler<ListItemAddEventArgs<T>> ItemAdd;

        /// <summary>
        /// Occurs after an item has been added to the list.
        /// </summary>
        public event EventHandler<ListItemAddedEventArgs<T>> ItemAdded;

        /// <summary>
        /// Occurs before an item is replaced in the list.
        /// </summary>
        public event EventHandler<ListItemReplaceEventArgs<T>> ItemReplace;

        /// <summary>
        /// Occurs after an item has been replaced in the list.
        /// </summary>
        public event EventHandler<ListItemReplacedEventArgs<T>> ItemReplaced;

        /// <summary>
        /// Occurs before an item is removed from the list.
        /// </summary>
        public event EventHandler<ListItemRemoveEventArgs<T>> ItemRemove;

        /// <summary>
        /// Occurs after an item has been removed from the list.
        /// </summary>
        /// <remarks>
        /// This event will only be called when an item has been successfully removed from the list.
        /// </remarks>
        public event EventHandler<ListItemRemovedEventArgs<T>> ItemRemoved;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
        /// <returns>
        /// The number of elements that the <see cref="EventList{T}"/> can contain before resizing is required.
        /// </returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><see cref="EventList{T}.Capacity"/> is set to a value that is less than <see cref="EventList{T}.Count"/>.</exception>
        /// <exception cref="T:System.OutOfMemoryException">There is not enough memory available on the system.</exception>
        public int Capacity
        {
            get
            {
                return _list.Capacity;
            }

            set
            {
                _list.Capacity = value;
            }
        }

        /// <inheritdoc />
        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        /// <inheritdoc />
        public virtual bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc />
        public T this[int index]
        {
            get
            {
                return _list[index];
            }

            set
            {
                T oldItem = _list[index];
                ListItemReplaceEventArgs<T> editEventArgs = new ListItemReplaceEventArgs<T>(index, oldItem, value);
                OnItemReplace(editEventArgs);

                if (!editEventArgs.Cancel)
                {
                    _list[index] = value;
                    ListItemReplacedEventArgs<T> editedEventArgs = new ListItemReplacedEventArgs<T>(index, oldItem, value);
                    OnItemReplaced(editedEventArgs);
                }
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public virtual void Add(T item)
        {
            AddItem(item);
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the <see cref="EventList{T}"/>.
        /// </summary>
        /// <param name="collection">The collection whose elements should be added to the end of the <see cref="EventList{T}"/>. 
        /// The collection itself cannot be null, but it can contain elements that are null, if type <typeparamref name="T"/> is a reference type.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="collection"/> is null.</exception>
        public virtual void AddRange(IEnumerable<T> collection)
        {
            AddRangeItems(collection);
        }

        /// <summary>
        /// Returns a read-only <see cref="T:System.Collections.Generic.IList`1"/> wrapper for the current collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1"/> that acts as a read-only wrapper around the current <see cref="EventList{T}"/>.
        /// </returns>
        public ReadOnlyCollection<T> AsReadOnly()
        {
            return _list.AsReadOnly();
        }

        /// <inheritdoc />
        public virtual void Clear()
        {
            ClearItems();
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        /// <inheritdoc />
        public virtual bool Remove(T item)
        {
            return RemoveItem(item);
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        /// <inheritdoc />
        public virtual void Insert(int index, T item)
        {
            InsertItem(index, item);
        }

        /// <inheritdoc />
        public virtual void RemoveAt(int index)
        {
            RemoveItem(index);
        }

        /// <summary>
        /// Removes the all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <returns>
        /// The number of elements removed from the <see cref="EventList{T}"/> .
        /// </returns>
        /// <param name="match">The <see cref="T:System.Predicate`1"/> delegate that defines the conditions of the elements to remove.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="match"/> is null.</exception>
        public virtual int RemoveAll(Predicate<T> match)
        {
            return RemoveItems(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, 
        /// and returns the zero-based index of the first occurrence within the entire <see cref="EventList{T}"/>.
        /// </summary>
        /// <returns>
        /// The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.
        /// </returns>
        /// <param name="match">The <see cref="T:System.Predicate`1"/> delegate that defines the conditions of the element to search for.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="match"/> is null.</exception>
        public int FindIndex(Predicate<T> match)
        {
            return _list.FindIndex(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, 
        /// and returns the zero-based index of the first occurrence within the range of elements in the <see cref="EventList{T}"/> that extends from the specified index to the last element.
        /// </summary>
        /// <returns>
        /// The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.
        /// </returns>
        /// <param name="startIndex">The zero-based starting index of the search.</param>
        /// <param name="match">The <see cref="T:System.Predicate`1"/> delegate that defines the conditions of the element to search for.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="match"/> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="startIndex"/> is outside the range of valid indexes for the <see cref="EventList{T}"/>.</exception>
        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return _list.FindIndex(startIndex, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, 
        /// and returns the zero-based index of the first occurrence within the range of elements in the <see cref="EventList{T}"/> that starts at the specified index and contains the specified number of elements.
        /// </summary>
        /// <returns>
        /// The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.
        /// </returns>
        /// <param name="startIndex">The zero-based starting index of the search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <param name="match">The <see cref="T:System.Predicate`1"/> delegate that defines the conditions of the element to search for.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="match"/> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="startIndex"/> is outside the range of valid indexes for the <see cref="EventList{T}"/>.
        /// -or-<paramref name="count"/> is less than 0.
        /// -or-<paramref name="startIndex"/> and <paramref name="count"/> do not specify a valid section in the <see cref="EventList{T}"/>.</exception>
        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            return _list.FindIndex(startIndex, count, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of the last occurrence within the entire <see cref="EventList{T}"/>.
        /// </summary>
        /// <returns>
        /// The zero-based index of the last occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.
        /// </returns>
        /// <param name="match">The <see cref="T:System.Predicate`1"/> delegate that defines the conditions of the element to search for.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="match"/> is null.</exception>
        public int FindLastIndex(Predicate<T> match)
        {
            return _list.FindLastIndex(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, 
        /// and returns the zero-based index of the last occurrence within the range of elements in the <see cref="EventList{T}"/> that extends from the first element to the specified index.
        /// </summary>
        /// <returns>
        /// The zero-based index of the last occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.
        /// </returns>
        /// <param name="startIndex">The zero-based starting index of the backward search.</param>
        /// <param name="match">The <see cref="T:System.Predicate`1"/> delegate that defines the conditions of the element to search for.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="match"/> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="startIndex"/> is outside the range of valid indexes for the <see cref="EventList{T}"/>.</exception>
        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return _list.FindLastIndex(startIndex, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, 
        /// and returns the zero-based index of the last occurrence within the range of elements in the <see cref="EventList{T}"/> that contains the specified number of elements and ends at the specified index.
        /// </summary>
        /// <returns>
        /// The zero-based index of the last occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.
        /// </returns>
        /// <param name="startIndex">The zero-based starting index of the backward search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <param name="match">The <see cref="T:System.Predicate`1"/> delegate that defines the conditions of the element to search for.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="match"/> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="startIndex"/> is outside the range of valid indexes for the <see cref="EventList{T}"/>.
        /// -or-<paramref name="count"/> is less than 0.
        /// -or-<paramref name="startIndex"/> and <paramref name="count"/> do not specify a valid section in the <see cref="EventList{T}"/>.</exception>
        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            return _list.FindLastIndex(startIndex, count, match);
        }

        /// <summary>
        /// Copies the elements of the <see cref="EventList{T}"/> to a new array.
        /// </summary>
        /// <returns>
        /// An array containing copies of the elements of the <see cref="EventList{T}"/>.
        /// </returns>
        public T[] ToArray()
        {
            return _list.ToArray();
        }
        
        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Called when before an item is added.
        /// </summary>
        /// <param name="e">The <see cref="ListItemAddEventArgs{T}"/>.</param>
        protected virtual void OnItemAdd(ListItemAddEventArgs<T> e)
        {
            EventHandler<ListItemAddEventArgs<T>> eventHandlers = ItemAdd;
            if (eventHandlers == null)
                return;

            foreach (EventHandler<ListItemAddEventArgs<T>> eventHandler in eventHandlers.GetInvocationList())
            {
                eventHandler(this, e);
                if (e.Cancel)
                    break;
            }
        }

        /// <summary>
        /// Called when an item has been added.
        /// </summary>
        /// <param name="e">The <see cref="ListItemAddedEventArgs{T}"/>.</param>
        protected virtual void OnItemAdded(ListItemAddedEventArgs<T> e)
        {
            EventHandler<ListItemAddedEventArgs<T>> eventHandlers = ItemAdded;
            if (eventHandlers != null)
                eventHandlers(this, e);
        }

        /// <summary>
        /// Called when before an item is replaced.
        /// </summary>
        /// <param name="e">The <see cref="ListItemReplaceEventArgs{T}"/>.</param>
        protected virtual void OnItemReplace(ListItemReplaceEventArgs<T> e)
        {
            EventHandler<ListItemReplaceEventArgs<T>> eventHandlers = ItemReplace;
            if (eventHandlers == null)
                return;

            foreach (EventHandler<ListItemReplaceEventArgs<T>> eventHandler in eventHandlers.GetInvocationList())
            {
                eventHandler(this, e);
                if (e.Cancel)
                    break;
            }
        }

        /// <summary>
        /// Called when an item has been replaced.
        /// </summary>
        /// <param name="e">The <see cref="ListItemReplacedEventArgs{T}"/>.</param>
        protected virtual void OnItemReplaced(ListItemReplacedEventArgs<T> e)
        {
            EventHandler<ListItemReplacedEventArgs<T>> eventHandlers = ItemReplaced;
            if (eventHandlers != null)
                eventHandlers(this, e);
        }

        /// <summary>
        /// Called when before an item is removed.
        /// </summary>
        /// <param name="e">The <see cref="ListItemRemoveEventArgs{T}"/>.</param>
        protected virtual void OnItemRemove(ListItemRemoveEventArgs<T> e)
        {
            EventHandler<ListItemRemoveEventArgs<T>> eventHandlers = ItemRemove;
            if (eventHandlers == null)
                return;

            foreach (EventHandler<ListItemRemoveEventArgs<T>> eventHandler in eventHandlers.GetInvocationList())
            {
                eventHandler(this, e);
                if (e.Cancel)
                    break;
            }
        }

        /// <summary>
        /// Called when an item has been removed.
        /// </summary>
        /// <param name="e">The <see cref="ListItemRemovedEventArgs{T}"/>.</param>
        protected virtual void OnItemRemoved(ListItemRemovedEventArgs<T> e)
        {
            EventHandler<ListItemRemovedEventArgs<T>> eventHandlers = ItemRemoved;
            if (eventHandlers != null)
                eventHandlers(this, e);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private void AddItem(T item)
        {
            InsertItem(-1, item);
        }

        private void AddRangeItems(IEnumerable<T> collection)
        {
            foreach (T item in collection)
                InsertItem(-1, item);
        }

        private int InsertItem(int index, T item)
        {
            ListItemAddEventArgs<T> addEventArgs = new ListItemAddEventArgs<T>(index, item);
            OnItemAdd(addEventArgs);

            int insertIndex = -1;
            if (!addEventArgs.Cancel)
            {
                if (addEventArgs.Index == -1)
                {
                    _list.Add(addEventArgs.Item);
                    insertIndex = _list.Count - 1;
                }
                else
                {
                    _list.Insert(addEventArgs.Index, addEventArgs.Item);
                    insertIndex = addEventArgs.Index;
                }
                ListItemAddedEventArgs<T> addedEventArgs = new ListItemAddedEventArgs<T>(insertIndex, item);
                OnItemAdded(addedEventArgs);
            }
            return (insertIndex == -1) ? 0 : insertIndex;
        }

        private void InsertRangeItems(int index, IEnumerable<T> collection)
        {
            IEnumerator<T> enumerator = collection.GetEnumerator();
            bool moveNext = enumerator.MoveNext();
            T item = enumerator.Current;
            for (int i = 0, insertedIndex; moveNext; moveNext = enumerator.MoveNext(), item = enumerator.Current, i += (insertedIndex == 0) ? 0 : 1)
                insertedIndex = InsertItem(index + i, item);
        }

        private void ClearItems()
        {
            // .ToList() is important here; we can't remove items and iterate the original list at the same time.
            foreach (T item in _list.ToList())
                RemoveItem(item);
        }

        private bool RemoveItem(T item)
        {
            int itemIndex = _list.IndexOf(item);
            return RemoveItem(itemIndex);
        }

        private bool RemoveItem(int index)
        {
            T item = _list.ElementAtOrDefault(index);
            ListItemRemoveEventArgs<T> removeEventArgs = new ListItemRemoveEventArgs<T>(index, item);
            OnItemRemove(removeEventArgs);

            if (!removeEventArgs.Cancel)
            {
                if (_list.Remove(item))
                {
                    ListItemRemovedEventArgs<T> removedEventArgs = new ListItemRemovedEventArgs<T>(index, item);
                    OnItemRemoved(removedEventArgs);
                    return true;
                }
            }
            return false;
        }

        private int RemoveItems(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException("match");

            // .ToList() is important here; we can't remove items and iterate the original list at the same time.
            foreach (T item in _list.Where(i => match(i)).ToList())
                RemoveItem(item);

            return _list.Count;
        }
    }
}
