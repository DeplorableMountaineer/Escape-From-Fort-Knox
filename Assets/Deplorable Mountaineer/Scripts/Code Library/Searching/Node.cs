using System;

namespace Deplorable_Mountaineer.Code_Library.Searching {
    /// <summary>
    /// Search tree node
    /// </summary>
    /// <typeparam name="TS">State type</typeparam>
    /// <typeparam name="TA">Action type</typeparam>
    public class Node<TS, TA> : IComparable<Node<TS, TA>>, IComparable {
        /// <summary>
        /// e.g. the current time from Time.time;  Used to break ties
        /// if the evaluation function is equal on two nodes
        /// </summary>
        public float TieBreaker { get; set; }

        /// <summary>
        /// Each node is associated with some state.  
        /// </summary>
        public TS State { get; set; }

        /// <summary>
        /// The parent node in the search tree
        /// </summary>
        public Node<TS, TA> Parent { get; set; }

        /// <summary>
        /// The action used to get from the parent node's state to this node's state
        /// </summary>
        public TA Action { get; set; }

        /// <summary>
        /// The cost so far from the initial node to this node
        /// </summary>
        public float PathCost { get; set; }

        /// <summary>
        /// The evaluation function, to determine which node is retrieved from a priority queue first 
        /// </summary>
        public Func<Node<TS, TA>, float> EvaluationFunction { get; set; }

        /// <summary>
        /// Compare two nodes, returning 0 if a tie, 1 if this is prioritized, -1 if
        /// other is prioritized.
        /// </summary>
        /// <param name="other">The other node</param>
        /// <returns>0, 1, or -1</returns>
        public int CompareTo(Node<TS, TA> other){
            float a = EvaluationFunction(this);
            float b = EvaluationFunction(other);
            int i = -a.CompareTo(b); //negated because queue returns max, but BFS wants min
            return i != 0 ? i : -TieBreaker.CompareTo(other.TieBreaker);
        }

        /// <summary>
        /// Compare two nodes, returning 0 if a tie, 1 if this is prioritized, -1 if
        /// other is prioritized; Null considered less than everything.  
        /// </summary>
        /// <param name="obj">The other node</param>
        /// <returns>0, 1, or -1</returns>
        /// <exception cref="ArgumentException">if obj is not null and not a node type</exception>
        public int CompareTo(object obj){
            if(ReferenceEquals(null, obj)) return 1;
            if(ReferenceEquals(this, obj)) return 0;
            return obj is Node<TS, TA> other
                ? CompareTo(other)
                : throw new ArgumentException(
                    $"Object must be of type {nameof(Node<TS, TA>)}");
        }
    }
}