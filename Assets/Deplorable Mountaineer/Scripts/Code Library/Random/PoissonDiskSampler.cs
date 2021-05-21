#region

using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library {
    /// <summary>
    ///     http://gregschlom.com/devlog/2014/06/29/
    ///     Poisson-disc-sampling-Unity.html
    ///     http://www.cs.ubc.ca/~rbridson/docs/
    ///     bridson-siggraph07-poissondisk.pdf
    /// </summary>
    [PublicAPI]
    public class PoissonDiskSampler {
        private const int K = 30;
        private readonly List<Vector2> _activeSamples = new List<Vector2>();
        private readonly float _cellSize;
        private readonly Vector2[,] _grid;
        private readonly float _radiusSquared;
        private Rect _rect;

        public PoissonDiskSampler(float width, float height, float radius){
            _rect = new Rect(0, 0, width, height);
            _radiusSquared = radius*radius;
            _cellSize = radius/Mathf.Sqrt(2);
            _grid = new Vector2[Mathf.CeilToInt(width/_cellSize),
                Mathf.CeilToInt(height/_cellSize)];
        }

        public IEnumerable<Vector2> Samples(){
            Vector2 firstSample = new Vector2(Random.value*_rect.width,
                Random.value*_rect.height);
            yield return AddSample(firstSample);
            while(_activeSamples.Count > 0){
                int i = (int) Random.value*_activeSamples.Count;
                Vector2 sample = _activeSamples[i];
                bool found = false;
                for(int j = 0; j < K; j++){
                    float angle = 2*Mathf.PI*Random.value;
                    float r = Mathf.Sqrt(Random.value*3*2*_radiusSquared);
                    Vector2 candidate =
                        sample + r*new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    if(!_rect.Contains(candidate) || !IsFarEnough(candidate)) continue;
                    found = true;
                    yield return AddSample(candidate);
                    break;
                }

                if(found) continue;
                _activeSamples[i] = _activeSamples[_activeSamples.Count - 1];
                _activeSamples.RemoveAt(_activeSamples.Count - 1);
            }
        }

        private bool IsFarEnough(Vector2 sample){
            GridPos pos = new GridPos(sample, _cellSize);
            int xMin = Mathf.Max(pos.x - 2, 0);
            int yMin = Mathf.Max(pos.y - 2, 0);
            int xMax = Mathf.Min(pos.x + 2, _grid.GetLength(0) - 1);
            int yMax = Mathf.Min(pos.y + 2, _grid.GetLength(1) - 1);
            for(int y = yMin; y <= yMax; y++)
            for(int x = xMin; x <= xMax; x++){
                Vector2 s = _grid[x, y];
                if(s == Vector2.zero) continue;
                if(Vector2.SqrMagnitude(s - sample) < _radiusSquared)
                    return false;
            }

            return true;
        }

        private Vector2 AddSample(Vector2 sample){
            _activeSamples.Add(sample);
            GridPos pos = new GridPos(sample, _cellSize);
            _grid[pos.x, pos.y] = sample;
            return sample;
        }

        private readonly struct GridPos {
            public readonly int x;
            public readonly int y;

            public GridPos(Vector2 sample, float cellSize){
                x = (int) (sample.x/cellSize);
                y = (int) (sample.y/cellSize);
            }
        }
    }
}