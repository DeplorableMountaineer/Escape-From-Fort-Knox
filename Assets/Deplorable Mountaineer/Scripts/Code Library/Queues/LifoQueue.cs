using System.Collections.Generic;

namespace Deplorable_Mountaineer.Code_Library.Queues {
    /// <summary>
    /// A queue whose elements are retrieved in the reverse order
    /// they are entered (a stack)
    /// </summary>
    /// <typeparam name="T">Data type for queue elements</typeparam>
    public class LifoQueue<T> : IQueue<T> {
        private readonly List<T> _data = new List<T>();

        /// <summary>
        /// Create a new empty queue
        /// </summary>
        public LifoQueue(){
        }

        /// <summary>
        /// Create a new queue and fill it with the specified data
        /// </summary>
        /// <param name="data">Initial data fill</param>
        public LifoQueue(IEnumerable<T> data){
            _data.AddRange(data);
        }

        /// <summary>
        /// Number of items in the queue
        /// </summary>
        public int Count => _data.Count;

        /// <summary>
        /// Get (without removing) an item from the queue;
        /// Item 0 will be the most recently added element, and so on.
        /// </summary>
        /// <param name="i">The index of the item</param>
        public T this[int i] => _data[i];

        /// <summary>
        /// Clear the queue, leaving it empty
        /// </summary>
        public void Clear(){
            _data.Clear();
        }

        /// <summary>
        /// Add a new item to the queue
        /// </summary>
        /// <param name="item">Item to insert</param>
        public void Enqueue(T item){
            _data.Insert(0, item);
        }

        /// <summary>
        /// Add a collection of data to the queue
        /// </summary>
        /// <param name="data">The collection of data</param>
        public void AddRange(IEnumerable<T> data){
            foreach(T item in data){
                Enqueue(item);
            }
        }

        /// <summary>
        /// Return a list of queue elements.  The first item of the list will
        /// be the most recently added element, and so on.
        /// </summary>
        /// <returns>A newly created list</returns>
        public List<T> ToList(){
            return new List<T>(_data);
        }

        /// <summary>
        /// Remove and return the most recently added element of the queue
        /// </summary>
        /// <returns>The item</returns>
        public T Dequeue(){
            T max = Peek();
            _data.RemoveAt(0);
            return max;
        }

        /// <summary>
        /// Get without removing the most recently added element of the queue
        /// </summary>
        /// <returns>The item</returns>
        public T Peek(){
            return this[0];
        }

        /// <summary>
        /// Return true if queue contains the item
        /// </summary>
        /// <param name="item">The item to search for</param>
        /// <returns>True or false</returns>
        public bool Contains(T item){
            return _data.Contains(item);
        }

        /// <summary>
        /// Get (without removing) an item from the queue;
        /// Item 0 will be the most recently added element, and so on.
        /// </summary>
        /// <param name="i">The index of the item</param>
        public T GetElement(int i){
            return this[i];
        }
    }
}