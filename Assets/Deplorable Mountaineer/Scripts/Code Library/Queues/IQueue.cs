using System.Collections.Generic;

namespace Deplorable_Mountaineer.Code_Library.Queues {
    /// <summary>
    /// A general queue
    /// </summary>
    /// <typeparam name="T">The item type for queue elements</typeparam>
    public interface IQueue<T> {
        /// <summary>
        /// Number of items in the queue
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Get (without removing) an item from the queue;
        /// Item 0 will be the top item, but there are no guarantees about the order
        /// of other items
        /// </summary>
        /// <param name="i">The index of the item</param>
        T this[int i] { get; }

        /// <summary>
        /// Clear the queue, leaving it empty
        /// </summary>
        void Clear();

        /// <summary>
        /// Add a new item to the queue
        /// </summary>
        /// <param name="item">Item to insert</param>
        void Enqueue(T item);

        /// <summary>
        /// Remove and return the top item of the queue
        /// </summary>
        /// <returns>The item</returns>
        T Dequeue();

        /// <summary>
        /// Get without removing the top item of the queue
        /// </summary>
        /// <returns>The item</returns>
        T Peek();

        /// <summary>
        /// Return true if queue contains the item
        /// </summary>
        /// <param name="item">The item to search for</param>
        /// <returns>True or false</returns>
        bool Contains(T item);

        /// <summary>
        /// Get (without removing) an item from the queue;
        /// Item 0 will be the top item, but there are no guarantees about the order
        /// of other items
        /// </summary>
        /// <param name="i">The index of the item</param>
        T GetElement(int i);

        /// <summary>
        /// Add a collection of data to the queue
        /// </summary>
        /// <param name="data">The collection of data</param>
        void AddRange(IEnumerable<T> data);

        /// <summary>
        /// Return a list of queue elements.  The first item of the list will
        /// be the top item, but there are no guarantees about the order
        /// of other items
        /// </summary>
        /// <returns>A newly created list</returns>
        List<T> ToList();
    }
}