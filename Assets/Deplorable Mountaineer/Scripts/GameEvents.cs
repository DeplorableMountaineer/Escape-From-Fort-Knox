using System;
using System.Collections;
using System.Collections.Generic;
using Deplorable_Mountaineer.Code_Library.Queues;
using Deplorable_Mountaineer.Movers;
using Deplorable_Mountaineer.Singleton;
using Deplorable_Mountaineer.UI;
using UnityEngine;

namespace Deplorable_Mountaineer {
    public class GameEvents : PersistentSingleton<GameEvents> {
        [SerializeField] private AudioMessage[] audioMessages;
        private readonly List<MessageEvent> _messageEvents = new List<MessageEvent>();

        private readonly PriorityQueue<MessageEvent> _eventQueue =
            new PriorityQueue<MessageEvent>();

        private readonly Dictionary<string, MessageEvent> _triggeredMessages =
            new Dictionary<string, MessageEvent>();


        public readonly Dictionary<string, AudioClip> AudioMessages =
            new Dictionary<string, AudioClip>();

        public float GameTimeAddend { get; set; } = 0;
        private bool _firstTimeDead = true;

        private void Start(){
            foreach(AudioMessage am in audioMessages){
                AudioMessages[am.title] = am.clip;
            }

            _messageEvents.Add(new MessageEvent() {
                Time = 5,
                Condition = InitialSwitchNotActivated,
                TextMessage = "Gold, gold everywhere, and not an ounce of worth...",
                AudioMessageClip = AudioMessages["Gold"],
            });

            _messageEvents.Add(new MessageEvent() {
                Time = 15,
                Condition = InitialSwitchNotActivated,
                TextMessage = "I really need to find a way to open the gate.",
                AudioMessageClip = AudioMessages["Open"],
            });

            _messageEvents.Add(new MessageEvent() {
                TriggerId = "Fire Trigger",
                TextMessage = "There must be a way around this...",
                AudioMessageClip = AudioMessages["Way"],
            });

            _messageEvents.Add(new MessageEvent() {
                TriggerId = "Outside Trigger",
                TextMessage = "Ah...outside...if I can only find an exit...",
                AudioMessageClip = AudioMessages["See"],
            });

            _messageEvents.Add(new MessageEvent() {
                TriggerId = "Vent Trigger",
                CancelTriggerId = "Fire Trigger",
                TextMessage = "Hmmm...feels like \"Alien\"",
                AudioMessageClip = AudioMessages["Alien"],
            });

            _messageEvents.Add(new MessageEvent() {
                TriggerId = "Gun Trigger",
                Condition = () => !FindObjectOfType<PlayerGun>().enabled,
                TextMessage = "I should have taken that gun when I had a chance...",
                AudioMessageClip = AudioMessages["Should"],
            });

            _messageEvents.Add(new MessageEvent() {
                TriggerId = "Made It Trigger",
                TextMessage = "Not the exit I expected, but I made it outside!",
                AudioMessageClip = AudioMessages["Made"],
            });

            foreach(MessageEvent me in _messageEvents){
                if(me.Time > 0){
                    _eventQueue.Enqueue(me);
                }
                else _triggeredMessages[me.TriggerId] = me;
            }

            foreach(TriggeredMessage tm in FindObjectsOfType<TriggeredMessage>()){
                if(_triggeredMessages.ContainsKey(tm.triggerId))
                    tm.MessageEvent = _triggeredMessages[tm.triggerId];
            }
        }

        private void Update(){
            if(_eventQueue.Count == 0){
                enabled = false;
                return;
            }

            if(_eventQueue.Peek().Time > GameTimeAddend + Time.time) return;
            MessageEvent me = _eventQueue.Dequeue();
            if(me.Condition != null && !me.Condition.Invoke()) return;
            if(me.AudioMessageClip) Message(me.AudioMessageClip);
            if(!me.AudioMessageClip || AudioListener.volume <= .1f) Message(me.TextMessage);
            if(string.IsNullOrWhiteSpace(me.CancelTriggerId)){
                return;
            }

            //getting one message makes another message obsolete; cancel it
            foreach(TriggeredMessage tm in FindObjectsOfType<TriggeredMessage>()){
                if(tm.triggerId == me.CancelTriggerId) tm.MessageEvent = null;
            }
        }

        public bool InitialSwitchNotActivated(){
            return !FindObjectOfType<SwitchedMover>().Activated;
        }

        public void Message(string text){
            HudMessage.Message(text);
        }

        public void Message(AudioClip clip){
            HudMessage.Message(clip);
        }


        public void OnPlayerDead(){
            if(_firstTimeDead){
                Message(AudioMessages["Save"]);
                _firstTimeDead = false;
            }

            Message("Player is dead.  Game reloading.");
            FindObjectOfType<CharacterController>().enabled = false;
            FindObjectOfType<FirstPersonController>().enabled = false;
            FindObjectOfType<PlayerGun>().enabled = false;
            StartCoroutine(ReloadGame());
        }

        private IEnumerator ReloadGame(){
            yield return new WaitForSeconds(5);
            FindObjectOfType<CharacterController>().enabled = true;
            FindObjectOfType<FirstPersonController>().enabled = true;
            GameSaver.Instance.ResetGame();
        }

        [Serializable]
        public class AudioMessage {
            public string title;
            public AudioClip clip;
        }

        public class MessageEvent : IComparable<MessageEvent>, IComparable {
            //use negative time for triggered events
            public float Time = -1;
            public string TriggerId;
            public string CancelTriggerId;
            public Func<bool> Condition;
            public string TextMessage;
            public AudioClip AudioMessageClip;

            public int CompareTo(MessageEvent other){
                if(ReferenceEquals(this, other)) return 0;
                if(ReferenceEquals(null, other)) return 1;
                return -Time.CompareTo(other.Time);
            }

            public int CompareTo(object obj){
                if(ReferenceEquals(null, obj)) return 1;
                if(ReferenceEquals(this, obj)) return 0;
                return obj is MessageEvent other
                    ? CompareTo(other)
                    : throw new ArgumentException(
                        $"Object must be of type {nameof(MessageEvent)}");
            }
        }
    }
}