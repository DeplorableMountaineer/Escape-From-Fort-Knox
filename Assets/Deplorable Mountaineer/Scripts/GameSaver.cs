using System;
using System.Collections.Generic;
using System.IO;
using Deplorable_Mountaineer.Code_Library.Mover;
using Deplorable_Mountaineer.Movers;
using Deplorable_Mountaineer.Singleton;
using Deplorable_Mountaineer.Switches;
using Standard_Assets.Characters.FirstPersonCharacter;
using UnityEngine;

namespace Deplorable_Mountaineer {
    public class GameSaver : PersistentSingleton<GameSaver> {
        [SerializeField] private string filename = "SavedGame";
        [SerializeField] private KeyCode loadKey = KeyCode.F2;
        [SerializeField] private KeyCode saveKey = KeyCode.F6;

        private void Update(){
            if(Input.GetKeyDown(loadKey)) RestoreGame();
            else if(Input.GetKeyDown(saveKey)) SaveGame();
        }

        public void SaveGame(string overrideSaveName = null){
            GameState state = GetGameState();
            string stateString = SerializeGameState(state);
            SaveState(overrideSaveName ?? filename, stateString);
            Debug.Log("Game saved.");
        }

        public void RestoreGame(string overrideSaveName = null){
            string stateString = LoadState(overrideSaveName ?? filename);
            if(string.IsNullOrWhiteSpace(stateString)){
                Debug.LogWarning("No game to restore");
                return;
            }

            GameState state = DeserializeGameState(stateString);
            SetGameState(state);
            Debug.Log("Game restored.");
        }

        private GameState DeserializeGameState(string stateString){
            return GameState.Deserialize(stateString);
        }

        private void SetGameState(GameState state){
            SetPlayerState(state);
            SetPhysicsBodyStates(state);
            SetDroneStates(state);
            SetInitialSwitchState(state);
            state.gameTime = GameEvents.Instance.GameTimeAddend + Time.time;
        }

        private void SetDroneStates(GameState state){
            Dictionary<string, Drone.Drone> drones =
                new Dictionary<string, Drone.Drone>();
            foreach(Drone.Drone d in FindObjectsOfType<Drone.Drone>()){
                drones[d.stateData.id] = d;
            }

            foreach(Drone.Drone.StateData dData in state.droneStates){
                if(drones.ContainsKey(dData.id)){
                    drones[dData.id].stateData = dData;
                    drones[dData.id].SetState();
                }
            }
        }

        private void SetPhysicsBodyStates(GameState state){
            Dictionary<string, PhysicsBodyState> physicsBodyStates =
                new Dictionary<string, PhysicsBodyState>();
            foreach(PhysicsBodyState pbs in FindObjectsOfType<PhysicsBodyState>()){
                physicsBodyStates[pbs.State.id] = pbs;
            }

            foreach(PhysicsBodyState.StateData pbData in state.physicsBodyStates){
                if(physicsBodyStates.ContainsKey(pbData.id)){
                    physicsBodyStates[pbData.id].State = pbData;
                    physicsBodyStates[pbData.id].SetState();
                }
            }
        }

        private void SetPlayerState(GameState state){
            GameObject player = GetPlayer();
            Transform t = player.transform;
            CharacterController cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            t.position = state.playerPosition;
            //setting rotation of Unity standard assets first person character
            //requires more than just changing the transform
            FirstPersonController fpc = player.GetComponent<FirstPersonController>();
            fpc.mouseLook.m_CameraTargetRot = state.cameraTargetRot;
            fpc.mouseLook.m_CharacterTargetRot = state.characterTargetRot;
            // ReSharper disable once Unity.InefficientPropertyAccess
            cc.enabled = true;
        }

        private void SetInitialSwitchState(GameState state){
            FindObjectOfType<Trigger>().numActivations = 1;
            GameObject.FindObjectOfType<SwitchedMover>().Activated =
                state.initialSwitchActivated;
        }

        private string LoadState(string saveName){
            string dir = Application.persistentDataPath +
                         Path.DirectorySeparatorChar + "SavedGame";
            string file = dir + Path.DirectorySeparatorChar + saveName;
            if(!File.Exists(file)) return null;
            StreamReader reader = new StreamReader(file);
            string stateString = reader.ReadToEnd();
            reader.Close();
            return stateString;
        }

        private void SaveState(string saveName, string state){
            string dir = Application.persistentDataPath +
                         Path.DirectorySeparatorChar + "SavedGame";
            string file = dir + Path.DirectorySeparatorChar + saveName;
            if(!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            StreamWriter writer = new StreamWriter(file, false);
            writer.Write(state);
            writer.Close();
        }

        private string SerializeGameState(GameState gameState){
            return gameState.Serialize();
        }

        private GameState GetGameState(){
            GameState state = new GameState();
            GetPlayerState(state);
            GetPhysicsBodyStates(state);
            GetDroneStates(state);
            GetInitialSwitchState(state);
            GameEvents.Instance.GameTimeAddend = state.gameTime - Time.time;
            return state;
        }

        private void GetDroneStates(GameState state){
            state.droneStates.Clear();
            foreach(Drone.Drone d in FindObjectsOfType<Drone.Drone>()){
                GetDroneState(state, d);
            }
        }

        private void GetDroneState(GameState state, Drone.Drone drone){
            drone.GetState();
            state.droneStates.Add(drone.stateData);
        }

        private void GetInitialSwitchState(GameState state){
            state.initialSwitchActivated =
                GameObject.FindObjectOfType<SwitchedMover>().Activated;
        }

        private void GetPhysicsBodyStates(GameState state){
            state.physicsBodyStates.Clear();
            foreach(PhysicsBodyState pbs in FindObjectsOfType<PhysicsBodyState>()){
                GetPhysicsBodyState(state, pbs);
            }
        }

        private void GetPhysicsBodyState(GameState state, PhysicsBodyState pbs){
            pbs.GetState();
            state.physicsBodyStates.Add(pbs.State);
        }

        private void GetPlayerState(GameState state){
            GameObject player = GetPlayer();
            Transform t = player.transform;
            state.playerPosition = t.position;
            //setting rotation of Unity standard assets first person character
            //requires more than just changing the transform
            FirstPersonController fpc = player.GetComponent<FirstPersonController>();
            state.cameraTargetRot = fpc.mouseLook.m_CameraTargetRot;
            state.characterTargetRot = fpc.mouseLook.m_CharacterTargetRot;
        }

        private GameObject GetPlayer(){
            return GameObject.FindGameObjectWithTag("Player");
        }

        [Serializable]
        public class GameState {
            public Vector3 playerPosition;
            public Quaternion characterTargetRot;
            public Quaternion cameraTargetRot;
            public bool initialSwitchActivated;
            public float gameTime;

            public List<PhysicsBodyState.StateData> physicsBodyStates =
                new List<PhysicsBodyState.StateData>();

            public List<Drone.Drone.StateData> droneStates =
                new List<Drone.Drone.StateData>();

            public string Serialize(){
                return JsonUtility.ToJson(this);
            }

            public static GameState Deserialize(string stateString){
                return JsonUtility.FromJson<GameState>(stateString);
            }
        }
    }
}