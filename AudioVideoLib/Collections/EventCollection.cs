/*
 * Date: 2012-12-22
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AudioVideoLib.Collections
{
    /// <summary>
    /// Represents a strongly typed collection of objects which support events to observe changes to the collection.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public class EventCollection<T> : ICollection<T>
    {
        private readonly List<T> _list = new List<T>();

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Occurs before an item is added to the collection.
        /// </summary>
        public event EventHandler<CollectionItemAddEventArgs<T>> ItemAdd;

        /// <summary>
        /// Occurs after an item has been added to the collection.
        /// </summary>
        public event EventHandler<CollectionItemAddedEventArgs<T>> ItemAdded;

        /// <summary>
        /// Occurs before an item is removed from the collection.
        /// </summary>
        public event EventHandler<CollectionItemRemoveEventArgs<T>> ItemRemove;

        /// <summary>
        /// Occurs after an item has been removed from the collection.
        /// </summary>
        public event EventHandler<CollectionItemRemovedEventArgs<T>> ItemRemoved;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        /// <inheritdoc />
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the <see cref="T" /> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="T" />.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns>
        /// The item at the specified index.
        /// </returns>
        public T this[int index]
        {
            get
            {
                return _list[index];
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

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
        public virtual void Add(T item)
        {
            AddItem(item);
        }

        /// <inheritdoc />
        public virtual void AddRange(IEnumerable<T> collection)
        {
            AddRangeItems(collection);
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
        public virtual bool Remove(T item)
        {
            return RemoveItem(item);
        }

        /// <summary>
        /// Removes the all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <returns>
        /// The number of elements removed from the <see cref="EventCollection{T}"/>.
        /// </returns>
        /// <param name="match">The <see cref="T:System.Predicate`1"/> delegate that defines the conditions of the elements to remove.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="match"/> is null.</exception>
        public virtual int RemoveAll(Predicate<T> match)
        {
            return RemoveItems(match);
        }

        /// <summary>
        /// Copies the elements of the <see cref="EventCollection{T}"/> to a new array.
        /// </summary>
        /// <returns>
        /// An array containing copies of the elements of the <see cref="EventCollection{T}"/>.
        /// </returns>
        public T[] ToArray()
        {
            return _list.ToArray();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Called when before an item is added.
        /// </summary>
        /// <param name="e">The <see cref="CollectionItemAddEventArgs{T}"/>.</param>
        protected virtual void OnItemAdd(CollectionItemAddEventArgs<T> e)
        {
            EventHandler<CollectionItemAddEventArgs<T>> eventHandlers = ItemAdd;
            if (eventHandlers == null)
                return;

            foreach (EventHandler<CollectionItemAddEventArgs<T>> eventHandler in eventHandlers.GetInvocationList())
            {
                eventHandler(this, e);
                if (e.Cancel)
                    break;
            }
        }

        /// <summary>
        /// Called when an item has been added.
        /// </summary>
        /// <param name="e">The <see cref="CollectionItemAddedEventArgs{T}"/>.</param>
        protected virtual void OnItemAdded(CollectionItemAddedEventArgs<T> e)
        {
            EventHandler<CollectionItemAddedEventArgs<T>> eventHandlers = ItemAdded;
            if (eventHandlers != null)
                eventHandlers(this, e);
        }

        /// <summary>
        /// Called when before an item is removed.
        /// </summary>
        /// <param name="e">The <see cref="CollectionItemRemoveEventArgs{T}"/>.</param>
        protected virtual void OnItemRemove(CollectionItemRemoveEventArgs<T> e)
        {
            EventHandler<CollectionItemRemoveEventArgs<T>> eventHandlers = ItemRemove;
            if (eventHandlers == null)
                return;

            foreach (EventHandler<CollectionItemRemoveEventArgs<T>> eventHandler in eventHandlers.GetInvocationList())
            {
                eventHandler(this, e);
                if (e.Cancel)
                    break;
            }
        }

        /// <summary>
        /// Called when an item has been removed.
        /// </summary>
        /// <param name="e">The <see cref="CollectionItemRemovedEventArgs{T}"/>.</param>
        protected virtual void OnItemRemoved(CollectionItemRemovedEventArgs<T> e)
        {
            EventHandler<CollectionItemRemovedEventArgs<T>> eventHandlers = ItemRemoved;
            if (eventHandlers != null)
                eventHandlers(this, e);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private void AddItem(T item)
        {
            CollectionItemAddEventArgs<T> itemAddEventArgs = new CollectionItemAddEventArgs<T>(item);
            OnItemAdd(itemAddEventArgs);
            
            if (!itemAddEventArgs.Cancel)
            {
                _list.Add(itemAddEventArgs.Item);
                CollectionItemAddedEventArgs<T> itemAddedEventArgs = new CollectionItemAddedEventArgs<T>(item);
                OnItemAdded(itemAddedEventArgs);
            }
        }

        private void AddRangeItems(IEnumerable<T> collection)
        {
            foreach (T item in collection)
                Add(item);
        }

        private bool RemoveItem(T item)
        {
            CollectionItemRemoveEventArgs<T> removeEventArgs = new CollectionItemRemoveEventArgs<T>(item);
            OnItemRemove(removeEventArgs);

            if (!removeEventArgs.Cancel)
            {
                if (_list.Remove(item))
                {
                    CollectionItemRemovedEventArgs<T> removedEventArgs = new CollectionItemRemovedEventArgs<T>(item);
                    OnItemRemoved(removedEventArgs);
                    return true;
                }
            }
            return false;
        }

        private void ClearItems()
        {
            // .ToList() is important here; we can't remove items and iterate the original list at the same time.
            foreach (T item in _list.ToList())
                RemoveItem(item);
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
