#region

using System;
using Deplorable_Mountaineer.Singleton;
using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library {
    /// <summary>
    ///     Persistant singleton random number manager.  Use seed of 0 for random
    ///     seed (based on clock time).  Otherwise, use fixed seeds for repeatability.
    ///     Should be created at the start (e.g. intro splash screen level)
    ///     If there are issues, use Script Execution Order to make it
    ///     start before the default time.
    /// </summary>
    [PublicAPI]
    public class RandomNumbers : PersistentSingleton<RandomNumbers> {
        [SerializeField] private long modulus = (long) 1 << 48;
        [SerializeField] private long multiplier = 25214903917;
        [SerializeField] private long increment = 11;
        [SerializeField] private long mask = (1L << 48) - 1 - ((1L << 16) - 1);
        [SerializeField] private long initialSeed = 314159;
        private int _haltonIndex = 100;

        private long _state;

        protected override void Awake(){
            base.Awake();
            _state = initialSeed;
        }

        /// <summary>
        ///     Get the current state of the random number generator
        /// </summary>
        /// <returns>The state</returns>
        public long GetState(){
            return _state;
        }

        /// <summary>
        ///     Set the state of the random number generator
        /// </summary>
        /// <param name="seed">The state to set</param>
        public void SetState(long seed){
            _state = seed == 0 ? DateTime.Now.Ticks : seed;
        }

        /// <summary>
        ///     Get an integer, by default from 0 to int.MaxValue
        /// </summary>
        /// <param name="minInclusive">Lower bound</param>
        /// <param name="maxExclusive">One more than upper bound</param>
        /// <returns>A random integer</returns>
        public int NextInt(int minInclusive = 0, int maxExclusive = int.MaxValue){
            _state = (_state*multiplier + increment)%modulus;
            long result = (_state*multiplier + increment)%modulus;
            result = (result*multiplier + increment)%modulus;
            result &= mask;
            result >>= 16;
            result %= maxExclusive - minInclusive;
            if(result < 0) result += maxExclusive - minInclusive;
            result += minInclusive;
            return (int) result;
        }

        /// <summary>
        ///     Get a floating point number from 0 to 1 inclusive
        /// </summary>
        /// <returns>A random float</returns>
        public float NextFloat(){
            return (float) (NextInt()/(double) int.MaxValue);
        }

        /// <summary>
        ///     Get a random floating point number from a uniform distribution
        /// </summary>
        /// <param name="min">Lower bound</param>
        /// <param name="max">Upper bound</param>
        /// <returns>A random float</returns>
        public float NextFloat(float min, float max){
            return NextFloat()*(max - min) + min;
        }

        /// <summary>
        ///     Get a random floating point number from a standard
        ///     normal (Gaussian) distribution
        /// </summary>
        /// <returns>A random standard normal float</returns>
        public float NextGaussian(){
            float u1 = 1.0f - NextFloat();
            float u2 = 1.0f - NextFloat();
            return Mathf.Sqrt(-2.0f*Mathf.Log(u1))*
                   Mathf.Sin(2.0f*Mathf.PI*u2);
        }

        /// <summary>
        ///     Get a random floating point number from the specified normal distribution
        /// </summary>
        /// <param name="mean">center of the distribution</param>
        /// <param name="stdDev">spread of the distribution</param>
        /// <returns>A random normal float</returns>
        public float NextGaussian(float mean, float stdDev){
            return mean + stdDev*NextGaussian();
        }

        /// <summary>
        ///     Get a random float from -1 to 1 following a binomial distribution (peaks at 0)
        /// </summary>
        /// <returns>A random binomial float</returns>
        public float NextBinomial(){
            return NextFloat() - NextFloat();
        }

        /// <summary>
        ///     Get the next integer element of the Halton sequence, good for random
        ///     placement without too much bunching
        /// </summary>
        /// <param name="minInclusive">Lower bound</param>
        /// <param name="maxExclusive">One less than upper bound</param>
        /// <returns>A random integer</returns>
        public int Halton1dInt(int minInclusive = 0, int maxExclusive = 1000){
            int dist = maxExclusive - minInclusive;
            if(dist >= 10000) return minInclusive + Mathf.FloorToInt(Halton1d()*dist - .01f);
            int result = Mathf.FloorToInt(Halton1d()*dist*100);
            result = minInclusive + result%dist;
            return result;
        }

        /// <summary>
        ///     Get the next floating point number of the Halton sequence, good for
        ///     random placement without too much bunching.
        /// </summary>
        /// <param name="min">Lower bound</param>
        /// <param name="max">Upper bound</param>
        /// <returns>A random float (scalar)</returns>
        public float Halton1d(float min = 0, float max = 1){
            _haltonIndex += 23;
            return Halton(11, _haltonIndex, min, max);
        }

        /// <summary>
        ///     Get the next 2-Vector of the Halton sequence, good for
        ///     random placement without too much bunching.
        /// </summary>
        /// <param name="min">Lower bound for each coordinate</param>
        /// <param name="max">Upper bound for each coordinate</param>
        /// <returns>A random vector</returns>
        public Vector2 Halton2d(float min = 0, float max = 1){
            _haltonIndex += 23;
            return new Vector2(Halton(11, _haltonIndex, min, max),
                Halton(13, _haltonIndex, min, max));
        }

        /// <summary>
        ///     Get the next 3-Vector of the Halton sequence, good for
        ///     random placement without too much bunching.
        /// </summary>
        /// <param name="min">Lower bound for each coordinate</param>
        /// <param name="max">Upper bound for each coordinate</param>
        /// <returns>A random vector</returns>
        public Vector3 Halton3d(float min = 0, float max = 1){
            _haltonIndex += 23;
            return new Vector3(Halton(11, _haltonIndex, min, max),
                Halton(13, _haltonIndex, min, max),
                Halton(17, _haltonIndex, min, max));
        }

        /// <summary>
        ///     Get the next 4-Vector of the Halton sequence, good for
        ///     random placement without too much bunching.
        /// </summary>
        /// <param name="min">Lower bound for each coordinate</param>
        /// <param name="max">Upper bound for each coordinate</param>
        /// <returns>A random vector</returns>
        public Vector4 Halton4d(float min = 0, float max = 1){
            _haltonIndex += 23;
            return new Vector4(Halton(11, _haltonIndex, min, max),
                Halton(13, _haltonIndex, min, max),
                Halton(17, _haltonIndex, min, max),
                Halton(19, _haltonIndex, min, max));
        }

        private static float Halton(int b, int index, float min = 0, float max = 1){
            float result = 0;
            float den = 1;
            int i = Mathf.Abs(index);
            while(i > 0){
                den *= b;
                result += i%b/den;
                i = Mathf.FloorToInt(i/(float) b);
            }

            return Mathf.Lerp(min, max, result);
        }
    }
}