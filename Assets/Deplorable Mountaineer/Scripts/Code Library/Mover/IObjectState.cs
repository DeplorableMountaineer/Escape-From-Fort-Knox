namespace Deplorable_Mountaineer.Code_Library.Mover {
    /// <summary>
    /// Allow an object to be saved and loaded
    /// </summary>
    public interface IObjectState {
        /// <summary>
        /// Extract the state of the object into a state structure
        /// </summary>
        void GetState();

        /// <summary>
        /// Apply the state structure to the object
        /// </summary>
        void SetState();
    }
}