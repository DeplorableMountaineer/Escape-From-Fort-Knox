using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#if UNITY_EDITOR

#endif
namespace Deplorable_Mountaineer.Code_Library {
    /// <summary>
    /// Various and sundry functions useful for many game activities
    /// </summary>
    [PublicAPI]
    public static class GameUtils {
        /// <summary>
        /// Name to use when the player's name is unknown 
        /// </summary>
        public const string GenericPlayerName = "A. Generic Player";

        /// <summary>
        /// Try to guess the player name from the environment
        /// </summary>
        /// <returns>The guessed name</returns>
        public static string GuessPlayerName(){
            string result = Environment.UserName;
            result = result.Replace("/", "").Replace("|", "").Replace("*", "");
            return !string.IsNullOrWhiteSpace(result) ? result : GenericPlayerName;
        }

        /// <summary>
        /// Convert a variable name to human friendly text
        /// </summary>
        /// <param name="name">The variable name</param>
        /// <returns>The nicer text</returns>
        public static string Nicify(string name){
            string result = name;
            while(result.StartsWith("_")){
                result = result.Substring(1);
            }

            if(result.StartsWith("m_") && result.Length > 2){
                result = char.ToUpper(result[2]) + result.Substring(3);
            }

            if(result.StartsWith("k") && result.Length > 1 && char.IsUpper(result[1])){
                result = result.Substring(1);
            }

            string temp = result;
            result = "";
            foreach(char c in temp){
                if(char.IsLower(c) && result.Length == 0){
                    result += char.ToUpper(c);
                    continue;
                }

                if(char.IsUpper(c) && result.Length > 0){
                    result += " ";
                }

                result += c == '_' ? ' ' : c;
            }

            return result;
        }

        /// <summary>
        /// Determine if object is part of a prefab
        /// </summary>
        /// <param name="obj">The object to query</param>
        /// <returns>true or false</returns>
        public static bool IsInPrefab(Object obj){
#if UNITY_EDITOR
            return PrefabUtility.IsPartOfAnyPrefab(obj);
#else
            return false;
#endif
        }

        /// <summary>
        /// Given a collection of paths, try to find one with the smallest
        /// number extracted from the filename.  Intended for filenames following a pattern
        /// like obj_1, obj_2, obj_3, etc.
        /// </summary>
        /// <param name="paths">The collection of paths</param>
        /// <returns>The selected path</returns>
        public static string FindMinFileNameNumber(IEnumerable<string> paths){
            long minVal = long.MaxValue;
            string result = null;
            foreach(string path in paths){
                long l = GetFileNameNumber(path);
                if(l == 0) continue;
                if(l >= minVal) continue;
                minVal = l;
                result = path;
            }

            return result;
        }

        /// <summary>
        /// Extract a number from a filename.  Used for getting the index from a file with a name like "obj_7".
        /// </summary>
        /// <param name="path">The path to query</param>
        /// <returns>The number</returns>
        public static long GetFileNameNumber(string path){
            string[] s = path.Split(Path.DirectorySeparatorChar);
            if(s.Length < 1) return 0;
            string f = s[s.Length - 1];
            while(f.Length > 0 && !char.IsDigit(f[f.Length - 1]))
                f = f.Substring(0, f.Length - 1);

            int i = f.Length;
            while(i > 0 && char.IsDigit(f[i - 1])) i--;

            f = f.Substring(i);
            return long.TryParse(f, out long l) ? l : 0;
        }

        /// <summary>
        /// Return a list of files in reverse numeric order.
        /// </summary>
        /// <param name="path">Directory to query</param>
        /// <returns>Ordered collection of files</returns>
        public static IEnumerable<string> ListFilesInReverseNumericOrder(string path){
            SortedList<long, string> l = new SortedList<long, string>();
            foreach(string file in Directory.EnumerateFiles(path)){
                long ticks = GetFileNameNumber(file);
                l[ticks] = file;
            }

            List<string> ll = new List<string>(l.Values);
            ll.Reverse();
            return ll;
        }

        /// <summary>
        /// Return a list of directories in reverse numeric order.
        /// </summary>
        /// <param name="path">Parent directory to query</param>
        /// <returns>Ordered collection of directories</returns>
        public static IEnumerable<string> ListDirectoriesInReverseNumericOrder(string path){
            SortedList<long, string> l = new SortedList<long, string>();
            foreach(string file in Directory.EnumerateDirectories(path)){
                long ticks = GetFileNameNumber(file);
                l[ticks] = file;
            }

            List<string> ll = new List<string>(l.Values);
            ll.Reverse();
            return ll;
        }

        /// <summary>
        /// Find all nearby objects having specified tag
        /// </summary>
        /// <param name="position">Position to query</param>
        /// <param name="tag">Tag to search for</param>
        /// <param name="maxDistance">How rangeFar is "nearby"</param>
        /// <returns>Collection of GameObjects</returns>
        public static IEnumerable<GameObject> FindNearbyWithTag(Vector3 position,
            string tag, float maxDistance){
            foreach(GameObject go in GameObject.FindGameObjectsWithTag(tag)){
                if(Vector3.Distance(position, go.transform.position) <= maxDistance)
                    yield return go;
            }
        }

        /// <summary>
        /// Find all object in front of position and having specified tag
        /// </summary>
        /// <param name="position">Position to query</param>
        /// <param name="forward">Direction of "front"</param>
        /// <param name="tag">Tag to search for</param>
        /// <param name="maxDistance">how rangeFar is "nearby"</param>
        /// <param name="maxHalfAngle">how many degrees from directly in front are allowed</param>
        /// <returns>Collection of GameObjects</returns>
        public static IEnumerable<GameObject> FindInFrontWithTag(Vector3 position,
            Vector3 forward, string tag, float maxDistance, float maxHalfAngle){
            foreach(GameObject go in FindNearbyWithTag(position, tag, maxDistance)){
                Vector3 offset = go.transform.position - position;
                if(Vector3.Angle(offset, forward) > maxHalfAngle) continue;
                yield return go;
            }
        }

        /// <summary>
        /// Find all object in front of position and having specified tag and which has
        /// nothing blocking view from position to object position
        /// </summary>
        /// <param name="position">Position to query</param>
        /// <param name="forward">Direction of "front"</param>
        /// <param name="tag">Tag to search for</param>
        /// <param name="maxDistance">how rangeFar is "nearby"</param>
        /// <param name="maxHalfAngle">how many degrees from directly in front are allowed</param>
        /// <returns>Collection of GameObjects</returns>
        public static IEnumerable<GameObject> FindVisibleWithTag(Vector3 position,
            Vector3 forward, string tag, float maxDistance, float maxHalfAngle){
            foreach(GameObject go in FindInFrontWithTag(position, forward, tag, maxDistance,
                maxHalfAngle)){
                if(!Physics.Raycast(position, go.transform.position - position,
                    out RaycastHit hit, maxDistance)) yield return go;
                if(hit.collider == go.GetComponent<Collider>()) yield return go;
            }
        }

        /// <summary>
        /// Return true if allowed to quit on the current architecture.
        /// Quitting is always ok from the editor.
        /// </summary>
        /// <returns>True or False</returns>
        public static bool IsQuittingSupported(){
#if UNITY_EDITOR
            return true;
#else
            switch(Application.platform){
                // platforms that should have quit button
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.LinuxPlayer:
                    return true;
                default:
                    return false;
            }
#endif
        }

        /// <summary>
        /// Quit game.
        /// </summary>
        /// <param name="force">Try to quit even if the architecture does not recommend it</param>
        public static void QuitGame(bool force = false){
            if(!IsQuittingSupported() && !force) return;
            Application.Quit();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
        }

        /// <summary>
        /// How fast does one need to move upward to jump a specific height?
        /// </summary>
        /// <param name="height">How high do we want to jump</param>
        /// <param name="gravityY">usually negative, acceleration of gravity downward</param>
        /// <returns>Speed, assuming no drag</returns>
        public static float VerticalSpeedForJump(float height, float gravityY = -9.81f){
            if(Mathf.Abs(gravityY) < Mathf.Epsilon) gravityY = Physics.gravity.y;
            return Mathf.Sqrt(height*Mathf.Abs(gravityY)*2);
        }

        /// <summary>
        /// Return the name of the scene at the specified build index
        /// </summary>
        /// <param name="buildIndex">Integer build index of scene</param>
        /// <returns>Scene name</returns>
        public static string GetSceneNameByBuildIndex(int buildIndex){
            string[] path = SceneUtility.GetScenePathByBuildIndex(buildIndex).Split('/');
            string result = path[path.Length - 1];
            if(result.EndsWith(".unity")) result = result.Substring(0, result.Length - 6);

            return result;
        }

        public static T
            FindComponentByTag<T>(string leaderboardRankText, Transform scorePanel = null){
            throw new NotImplementedException();
        }
    }
}