using System.Collections.Generic;

namespace Deplorable_Mountaineer.Code_Library.Searching {
    /// <summary>
    /// Abstract stateData graph
    /// </summary>
    /// <typeparam name="TS">StateData type</typeparam>
    /// <typeparam name="TA">Action type</typeparam>
    public interface IStateGraph<TS, TA> {
        /// <summary>
        /// Starting stateData in the problem
        /// </summary>
        TS InitialState { get; }

        /// <summary>
        /// Actions available from the specified stateData
        /// </summary>
        /// <param name="state">The stateData to query</param>
        /// <returns>Enumeration of actions</returns>
        IEnumerable<TA> Actions(TS state);

        /// <summary>
        /// Number of actions available from specified stateData;
        /// returns int.Max if infinite or of unknown bound
        /// </summary>
        /// <param name="state">The stateData to query</param>
        /// <returns>A nonnegative integer</returns>
        int CountActions(TS state);

        /// <summary>
        /// Returns the result of taking the specified action
        /// from the specified stateData.  Action must be a valid
        /// action for the stateData. 
        /// </summary>
        /// <param name="currentState">The stateData to query</param>
        /// <param name="action">The action to query</param>
        /// <returns>The new stateData</returns>
        TS Result(TS currentState, TA action);

        /// <summary>
        /// Returns the cost of performing the specified (valid) action on
        /// the specified stateData.
        /// </summary>
        /// <param name="currentState">The stateData to query</param>
        /// <param name="action">The action to query</param>
        /// <returns>The cost, which must be nonnegative and preferably not zero</returns>
        float ActionCost(TS currentState, TA action);

        /// <summary>
        /// Return true if the specified stateData is a goal stateData.  A graph can have many
        /// goal states.
        /// </summary>
        /// <param name="state">The stateData to query</param>
        /// <returns>True or false</returns>
        bool IsGoal(TS state);

        /// <summary>
        /// Return true if given a valid stateData in the graph.
        /// </summary>
        /// <param name="state">The stateData object to query</param>
        /// <returns>True or falue</returns>
        bool IsValidState(TS state);

        /// <summary>
        /// Return true if the specified stateData has the specified action as a valid action.
        /// </summary>
        /// <param name="currentState">The stateData to query</param>
        /// <param name="action">The action to query</param>
        /// <returns>True or false</returns>
        bool IsValidAction(TS currentState, TA action);

        /// <summary>
        /// Return true if performing the specified action on the specified stateData gives
        /// the specified result stateData.
        /// </summary>
        /// <param name="currentState">Current stateData to query</param>
        /// <param name="action">Action to query</param>
        /// <param name="resultState">Result stateData to test against</param>
        /// <returns></returns>
        bool IsValidTransition(TS currentState, TA action, TS resultState);
    }
}