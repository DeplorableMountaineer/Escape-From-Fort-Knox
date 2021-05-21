using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deplorable_Mountaineer.Code_Library.Queues {
    /// <summary>
    /// A queue whose elements are retrieved in priority order
    /// </summary>
    /// <typeparam name="T">Data type for queue elements</typeparam>
    public class PriorityQueue<T> : IQueue<T> where T : IComparable<T> {
        private readonly List<T> _data = new List<T>();

        /// <summary>
        /// Create a new empty queue
        /// </summary>
        public PriorityQueue(){
        }

        /// <summary>
        /// Create a new queue and fill it with the specified data
        /// </summary>
        /// <param name="data">Initial data fill</param>
        public PriorityQueue(IEnumerable<T> data){
            _data.AddRange(data);
            BuildHeap();
        }

        /// <summary>
        /// Number of items in the queue
        /// </summary>
        public int Count => _data.Count;

        /// <summary>
        /// Get (without removing) an item from the queue;
        /// Item 0 will be the max priority, but remaining items are in a more complex order
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
            _data.Insert(Count, item);
            int i = Count;
            while(i > 1 && Parent(i - 1).CompareTo(item) < 0){
                _data[i - 1] = Parent(i - 1);
                i = ParentIndex(i - 1) + 1;
            }

            _data[i] = item;
        }

        /// <summary>
        /// Add a collection of data to the queue
        /// </summary>
        /// <param name="data">The collection of data</param>
        public void AddRange(IEnumerable<T> data){
            _data.AddRange(data);
            BuildHeap();
        }

        /// <summary>
        /// Return a list of queue elements.  The first item of the list will
        /// be the max, but remaining items are in a more complex order.
        /// </summary>
        /// <returns>A newly created list</returns>
        public List<T> ToList(){
            return new List<T>(_data);
        }

        /// <summary>
        /// Remove and return the maximum priority item of the queue
        /// </summary>
        /// <returns>The item</returns>
        public T Dequeue(){
            T max = Peek();
            _data[0] = this[Count - 1];
            _data.RemoveAt(Count - 1);
            Heapify(0);
            return max;
        }

        /// <summary>
        /// Get without removing the maximum priority item of the queue
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
        /// Item 0 will be the max priority, but remaining items are in a more complex order
        /// </summary>
        /// <param name="i">The index of the item</param>
        public T GetElement(int i){
            return this[i];
        }

        private T Parent(int i){
            Debug.Assert(i >= 0 && i < Count, "i >= 0 && i < Count");
            return _data[ParentIndex(i)];
        }

        private void BuildHeap(){
            int i = Mathf.FloorToInt(Count/2f);
            while(i > 0){
                i--;
                Heapify(i);
            }
        }

        private void Heapify(int i){
            while(true){
                int l = LeftIndex(i);
                int r = RightIndex(i);
                int largest = i;
                if(l < Count && this[l].CompareTo(this[i]) > 0) largest = l;
                if(r < Count && this[r].CompareTo(this[largest]) > 0) largest = r;
                if(largest == i) return;
                T tmp = this[i];
                _data[i] = this[largest];
                _data[largest] = tmp;
                i = largest;
            }
        }

        private static int ParentIndex(int i){
            return Mathf.FloorToInt((i + 1f)/2f) - 1;
        }

        private static int LeftIndex(int i){
            return 2*(i + 1) - 1;
        }

        private static int RightIndex(int i){
            return 2*(i + 1);
        }
    }
}