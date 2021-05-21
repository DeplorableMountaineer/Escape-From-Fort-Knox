using System;
using System.Collections.Generic;
using Deplorable_Mountaineer.Code_Library.Queues;
using UnityEngine;

namespace Deplorable_Mountaineer.Code_Library.Searching {
    /// <summary>
    /// Code is adapted from algorithms given in Russell and Norvig:
    /// <i>Artificial Intelligence: A Modern Approach</i>, fourth ed.
    ///
    /// Algorithms to search a state graph for a path from the initial state to a goal state.
    /// Iterative Deepening Search is best in most cases where no good heuristic
    /// or evaluation function is available, but still can perform poorly on large state graphs. 
    /// </summary>
    public static class Searching {
        /// <summary>
        /// The most commonly-used search algorithm; works well if heuristic is good.
        ///
        /// It is complete if the state graph is finite or if both the following hold: there
        /// is a solution and there is a positive lower bound on action costs.  If
        /// heuristic is admissible (never overestimates cost from this node to goal), it is optimal.  If the heuristic
        /// is consistent (satisfies the triangle inequality, which implies admissibility),
        /// it is also optimally efficient in terms of number of nodes expanded.  If heuristic
        /// is an accurate estimate of cost from this node to goal without being admissible,
        /// it is a good "satisficing search" ("satisficing" basically means "good enough");
        /// in particular, if there is a weight factor w>1 where the
        /// heuristic is never more than w times the actual cost, the solution found will
        /// not have more than w times the optimal solution cost.
        ///
        /// The time and space complexity depend on the heuristic function; good heuristics
        /// can give very good results.  If the heuristic is not so good, memory is usually
        /// a limitation before time is. 
        /// </summary>
        /// <param name="problem">The state graph representing the problem</param>
        /// <param name="heuristic">An estimate of the best possible path
        /// cost from a given node to a goal node</param>
        /// <typeparam name="TS">The type of a state in the state graph</typeparam>
        /// <typeparam name="TA">The type of an action in the state graph</typeparam>
        /// <returns>Solution node or null on failure</returns>
        public static Node<TS, TA> AStar<TS, TA>(IStateGraph<TS, TA> problem,
            Func<Node<TS, TA>, float> heuristic){
            return BestFirstSearch(problem, node => heuristic(node) + node.PathCost);
        }

        /// <summary>
        /// Use evaluation function to expand the best nodes first.  Performance depends on choice
        /// of evaluation function.
        /// </summary>
        /// <param name="problem">The state graph representing the problem</param>
        /// <param name="evaluationFunction">popping a node from the frontier queue retrieves
        /// the highest value of this function</param>
        /// <typeparam name="TS">The type of a state in the state graph</typeparam>
        /// <typeparam name="TA">The type of an action in the state graph</typeparam>
        /// <returns>Solution node or null on failure</returns>
        public static Node<TS, TA> BestFirstSearch<TS, TA>(IStateGraph<TS, TA> problem,
            Func<Node<TS, TA>, float> evaluationFunction){
            Node<TS, TA> node = new Node<TS, TA>() {
                TieBreaker = Time.time,
                State = problem.InitialState,
                Parent = null,
                Action = default,
                PathCost = 0,
                EvaluationFunction = evaluationFunction
            };
            IQueue<Node<TS, TA>> frontier = new PriorityQueue<Node<TS, TA>>();
            frontier.Enqueue(node);
            Dictionary<TS, Node<TS, TA>> reached =
                new Dictionary<TS, Node<TS, TA>> {[problem.InitialState] = node};
            while(frontier.Count > 0){
                node = frontier.Dequeue();
                if(problem.IsGoal(node.State)) return node;
                foreach(Node<TS, TA> child in Expand(problem, node)){
                    TS s = child.State;
                    if(reached.ContainsKey(s) && child.PathCost >= reached[s].PathCost)
                        continue;
                    reached[s] = child;
                    frontier.Enqueue(child);
                }
            }

            return null;
        }

        /// <summary>
        /// expand shallowest nodes first. Complete (if b is finite and either
        /// there is a solution or state space is finite), optimal cost, and time and space
        /// complexity is O(b^d) where b is branching factor and d is solution depth  
        /// </summary>
        /// <param name="problem">The state graph representing the problem</param>
        /// <typeparam name="TS">The type of a state in the state graph</typeparam>
        /// <typeparam name="TA">The type of an action in the state graph</typeparam>
        /// <returns>Solution node or null on failure</returns>
        public static Node<TS, TA> BreadthFirstSearch<TS, TA>(IStateGraph<TS, TA> problem){
            Node<TS, TA> node = new Node<TS, TA>() {
                TieBreaker = Time.time,
                State = problem.InitialState,
                Parent = null,
                Action = default,
                PathCost = 0,
            };
            if(problem.IsGoal(node.State)) return node;
            IQueue<Node<TS, TA>> frontier = new FifoQueue<Node<TS, TA>>();
            frontier.Enqueue(node);
            HashSet<TS> reached = new HashSet<TS> {problem.InitialState};

            while(frontier.Count > 0){
                node = frontier.Dequeue();
                foreach(Node<TS, TA> child in Expand(problem, node)){
                    TS s = child.State;
                    if(problem.IsGoal(s)) return child;
                    if(reached.Contains(s)) continue;
                    reached.Add(s);
                    frontier.Enqueue(child);
                }
            }

            return null;
        }

        /// <summary>
        /// expand nodes of lowest cost first. Complete (if b is finite and either the state
        /// graph is finite or both the following hold: there
        /// is a positive lower bound on action costs and there is a solution), optimal cost,
        /// and time and space
        /// complexity is O(b^(1+c/e)) where b is branching factor, c is the optimal cost, and
        /// e is a lower bound on cost. 
        /// </summary>
        /// <param name="problem">The state graph representing the problem</param>
        /// <typeparam name="TS">The type of a state in the state graph</typeparam>
        /// <typeparam name="TA">The type of an action in the state graph</typeparam>
        /// <returns>Solution node or null on failure</returns>
        public static Node<TS, TA> UniformCostSearch<TS, TA>(IStateGraph<TS, TA> problem){
            return BestFirstSearch(problem, node => node.PathCost);
        }

        /// <summary>
        /// Depth first with depth limit (or cost limit) that increases with each iteration.
        /// Complete (if b is finite and either
        /// there is a solution or state space is finite) and optimal (if all action costs
        /// are equal).  Time complexity is O(b^d) and space complexity is O(bd) where
        /// b is branching factor and d is solution depth.
        /// Set problem action costs to 1 to use actual node depth instead of cost
        /// </summary>
        /// <param name="problem">The state graph representing the problem</param>
        /// <typeparam name="TS">The type of a state in the state graph</typeparam>
        /// <typeparam name="TA">The type of an action in the state graph</typeparam>
        /// <returns>Solution node or null on failure</returns>
        public static Node<TS, TA> IterativeDeepeningSearch<TS, TA>(
            IStateGraph<TS, TA> problem){
            float depth = 0;
            while(true){
                Node<TS, TA> result =
                    DepthLimitedSearch(problem, depth, out float depthAchieved);
                if(result != null || depthAchieved <= depth) return result;
                depth = depthAchieved;
            }
        }

        /// <summary>
        /// Depth first with depth limit (or cost limit).
        /// By itself, not generally complete or optimal.
        /// Time complexity is O(b^l) and space complexity is O(bl) where
        /// b is branching factor and l is the depth limit.
        /// Set problem action costs to 1 to use actual node depth instead of cost; otherwise
        /// depth limit given will be cost limit, but time/space complexity still depends on
        /// true depth achieved.
        /// </summary>
        /// <param name="problem">The state graph representing the problem</param>
        /// <param name="maxDepth">Maximum cost (or node depth) to search before failing</param>
        /// <param name="depthAchieved">Actual cost (or node depth) achieved.  If null
        /// is returned and this is less than or equal to max depth, there is no solution.</param>
        /// <typeparam name="TS">The type of a state in the state graph</typeparam>
        /// <typeparam name="TA">The type of an action in the state graph</typeparam>
        /// <returns>Solution node or null on failure or max depth exceeded</returns>
        public static Node<TS, TA> DepthLimitedSearch<TS, TA>(IStateGraph<TS, TA> problem,
            float maxDepth, out float depthAchieved){
            Node<TS, TA> node = new Node<TS, TA>() {
                TieBreaker = Time.time,
                State = problem.InitialState,
                Parent = null,
                Action = default,
                PathCost = 0,
            };
            depthAchieved = 0;
            if(problem.IsGoal(node.State)) return node;
            IQueue<Node<TS, TA>> frontier = new LifoQueue<Node<TS, TA>>();
            frontier.Enqueue(node);

            while(frontier.Count > 0){
                node = frontier.Dequeue();
                if(problem.IsGoal(node.State)) return node;
                if(node.PathCost > maxDepth) continue;
                foreach(Node<TS, TA> child in Expand(problem, node)){
                    depthAchieved = Mathf.Max(depthAchieved, child.PathCost);
                    frontier.Enqueue(child);
                }
            }

            return null;
        }

        /// <summary>
        /// Expand a node; utility function for other search algorithms
        /// </summary>
        /// <param name="problem">The state graph representing the problem</param>
        /// <param name="node"></param>
        /// <typeparam name="TS">The type of a state in the state graph</typeparam>
        /// <typeparam name="TA">The type of an action in the state graph</typeparam>
        /// <returns>Enumeration of child nodes in no particular order</returns>
        private static IEnumerable<Node<TS, TA>> Expand<TS, TA>(IStateGraph<TS, TA> problem,
            Node<TS, TA> node){
            TS s = node.State;
            foreach(TA action in problem.Actions(s)){
                TS s1 = problem.Result(s, action);
                float cost = node.PathCost + problem.ActionCost(s, action);
                Node<TS, TA> newNode = new Node<TS, TA>() {
                    TieBreaker = Time.time,
                    State = s1,
                    Parent = node,
                    Action = action,
                    PathCost = cost,
                    EvaluationFunction = node.EvaluationFunction
                };
                yield return newNode;
            }
        }
    }
}