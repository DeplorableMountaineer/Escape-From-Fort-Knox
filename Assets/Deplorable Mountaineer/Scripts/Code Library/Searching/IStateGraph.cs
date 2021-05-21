﻿using System.Collections.Generic;

namespace Deplorable_Mountaineer.Code_Library.Searching {
    /// <summary>
    /// Abstract state graph
    /// </summary>
    /// <typeparam name="TS">State type</typeparam>
    /// <typeparam name="TA">Action type</typeparam>
    public interface IStateGraph<TS, TA> {
        /// <summary>
        /// Starting state in the problem
        /// </summary>
        TS InitialState { get; }

        /// <summary>
        /// Actions available from the specified state
        /// </summary>
        /// <param name="state">The state to query</param>
        /// <returns>Enumeration of actions</returns>
        IEnumerable<TA> Actions(TS state);

        /// <summary>
        /// Number of actions available from specified state;
        /// returns int.Max if infinite or of unknown bound
        /// </summary>
        /// <param name="state">The state to query</param>
        /// <returns>A nonnegative integer</returns>
        int CountActions(TS state);

        /// <summary>
        /// Returns the result of taking the specified action
        /// from the specified state.  Action must be a valid
        /// action for the state. 
        /// </summary>
        /// <param name="currentState">The state to query</param>
        /// <param name="action">The action to query</param>
        /// <returns>The new state</returns>
        TS Result(TS currentState, TA action);

        /// <summary>
        /// Returns the cost of performing the specified (valid) action on
        /// the specified state.
        /// </summary>
        /// <param name="currentState">The state to query</param>
        /// <param name="action">The action to query</param>
        /// <returns>The cost, which must be nonnegative and preferably not zero</returns>
        float ActionCost(TS currentState, TA action);

        /// <summary>
        /// Return true if the specified state is a goal state.  A graph can have many
        /// goal states.
        /// </summary>
        /// <param name="state">The state to query</param>
        /// <returns>True or false</returns>
        bool IsGoal(TS state);

        /// <summary>
        /// Return true if given a valid state in the graph.
        /// </summary>
        /// <param name="state">The state object to query</param>
        /// <returns>True or falue</returns>
        bool IsValidState(TS state);

        /// <summary>
        /// Return true if the specified state has the specified action as a valid action.
        /// </summary>
        /// <param name="currentState">The state to query</param>
        /// <param name="action">The action to query</param>
        /// <returns>True or false</returns>
        bool IsValidAction(TS currentState, TA action);

        /// <summary>
        /// Return true if performing the specified action on the specified state gives
        /// the specified result state.
        /// </summary>
        /// <param name="currentState">Current state to query</param>
        /// <param name="action">Action to query</param>
        /// <param name="resultState">Result state to test against</param>
        /// <returns></returns>
        bool IsValidTransition(TS currentState, TA action, TS resultState);
    }
}