using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Deplorable_Mountaineer.Code_Library {
    [PublicAPI]
    public class Rng {
        public static readonly Rng[] Rngs = {
            new Rng(), new Rng(), new Rng(), new Rng(), new Rng(),
            new Rng(), new Rng(), new Rng(), new Rng(), new Rng(),
            new Rng(), new Rng(), new Rng(), new Rng(), new Rng(),
            new Rng(), new Rng(), new Rng(), new Rng(), new Rng(),
            new Rng(), new Rng(), new Rng(), new Rng(), new Rng()
        };

        public static readonly Rng Random = new Rng();

        private const long Modulus = (long) 1 << 48;
        private const long Multiplier = 25214903917;
        private const long Increment = 11;
        private const long Mask = (1L << 48) - 1 - ((1L << 16) - 1);
        private static long _numInstances = 0;
        private long _state;
        private int _haltonIndex = 100;

        public Rng(long? seed = null){
            _numInstances++;
            _state = seed.HasValue
                ? (seed == 0 ? DateTime.Now.Ticks : seed.Value)
                : 314159 + 13*_numInstances*_numInstances;
        }

        /// <summary>
        /// Set the stateData of the random number generator
        /// </summary>
        /// <param name="seed">The stateData to set</param>
        public void SetSeed(long seed = 0){
            _state = seed == 0 ? DateTime.Now.Ticks : seed;
        }

        /// <summary>
        /// Get an integer, by default from 0 to int.MaxValue
        /// </summary>
        /// <param name="minInclusive">Lower bound</param>
        /// <param name="maxExclusive">One more than upper bound</param>
        /// <returns>A random integer</returns>
        public int NextInt(int minInclusive = 0, int maxExclusive = int.MaxValue){
            return Next(ref _state, minInclusive, maxExclusive);
        }

        /// <summary>
        /// Get a floating point number from 0 to 1 inclusive
        /// </summary>
        /// <returns>A random float</returns>
        public float NextFloat(){
            return (float) (NextInt()/(double) int.MaxValue);
        }

        /// <summary>
        /// Get a random floating point number from a uniform distribution
        /// </summary>
        /// <param name="min">Lower bound</param>
        /// <param name="max">Upper bound</param>
        /// <returns>A random float</returns>
        public float NextFloat(float min, float max){
            return NextFloat()*(max - min) + min;
        }

        /// <summary>
        /// Get a random floating point number from a standard
        /// normal (Gaussian) distribution
        /// </summary>
        /// <returns>A random standard normal float</returns>
        public float NextGaussian(){
            float u1 = 1.0f - NextFloat();
            float u2 = 1.0f - NextFloat();
            return Mathf.Sqrt(-2.0f*Mathf.Log(u1))*
                   Mathf.Sin(2.0f*Mathf.PI*u2);
        }

        /// <summary>
        /// Get a random float from -1 to 1 following a binomial distribution (peaks at 0)
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

        /// <summary>
        /// Return a random 2D vector of magnitude 1
        /// </summary>
        /// <returns>A random direction</returns>
        public Vector2 UnitNormal2(){
            float angle = NextFloat(0, 2*Mathf.PI);
            return new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
        }

        /// <summary>
        /// Return a random 2D vector of magnitude 1 no more than the specified
        /// number of degrees from the positive Y direction
        /// </summary>
        /// <param name="maxHalfAngle">Radius of direction limits</param>
        /// <returns>A random direction</returns>
        public Vector2 UnitNormal2(float maxHalfAngle){
            float angle = NextFloat(-maxHalfAngle*Mathf.Deg2Rad, maxHalfAngle*Mathf.Deg2Rad);
            return new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
        }

        /// <summary>
        /// Return a random 3D vector of magnitude 1
        /// </summary>
        /// <returns>A random direction</returns>
        public Vector3 UnitNormal3(){
            float azimuth = NextFloat(0, 2*Mathf.PI);
            float altitude = NextFloat(-Mathf.PI/2, Mathf.PI/2);
            float c = Mathf.Cos(altitude);
            return new Vector3(c*Mathf.Sin(azimuth), Mathf.Sin(altitude),
                c*Mathf.Cos(azimuth));
        }

        /// <summary>
        /// Return a random 3D vector of magnitude 1 no more than the specified
        /// number of degrees from the positive Z direction
        /// </summary>
        /// <param name="maxHalfAngle">Radius of direction limits</param>
        /// <returns>A random direction</returns>
        public Vector3 UnitNormal3(float maxHalfAngle){
            float m = maxHalfAngle*Mathf.Deg2Rad;
            float azimuth = NextFloat(-m, m);
            float altitude = NextFloat(-m, m);
            float c = Mathf.Cos(altitude);
            return new Vector3(c*Mathf.Sin(azimuth), Mathf.Sin(altitude),
                c*Mathf.Cos(azimuth));
        }

        /// <summary>
        /// Return a random 3D vector of magnitude 1 no more than the specified
        /// number of degrees from the specified axis direction
        /// </summary>
        /// <param name="maxHalfAngle">Radius of direction limits</param>
        /// <returns>A random direction</returns>
        public Vector3 UnitNormal3(Vector3 axis, float maxHalfAngle){
            return Quaternion.FromToRotation(Vector3.forward, axis)*UnitNormal3(maxHalfAngle);
        }

        private static int Next(ref long state, int minInclusive = 0,
            int maxExclusive = int.MaxValue){
            state = (state*Multiplier + Increment)%Modulus;
            long result = (state*Multiplier + Increment)%Modulus;
            result = (result*Multiplier + Increment)%Modulus;
            result &= Mask;
            result >>= 16;
            result %= maxExclusive - minInclusive;
            if(result < 0) result += maxExclusive - minInclusive;
            result += minInclusive;
            return (int) result;
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