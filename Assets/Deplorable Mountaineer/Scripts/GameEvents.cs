using System;
using System.Collections.Generic;
using Deplorable_Mountaineer.Code_Library.Queues;
using Deplorable_Mountaineer.Movers;
using Deplorable_Mountaineer.Singleton;
using Deplorable_Mountaineer.UI;
using UnityEngine;

namespace Deplorable_Mountaineer {
    public class GameEvents : PersistentSingleton<GameEvents> {
        private readonly List<MessageEvent> _messageEvents = new List<MessageEvent>();

        private readonly PriorityQueue<MessageEvent> _eventQueue =
            new PriorityQueue<MessageEvent>();

        private readonly Dictionary<string, MessageEvent> _triggeredMessages =
            new Dictionary<string, MessageEvent>();

        public float GameTimeAddend { get; set; } = 0;

        private void Start(){
            _messageEvents.Add(new MessageEvent() {
                Time = 5,
                Condition = InitialSwitchNotActivated,
                TextMessage = "Gold, gold everywhere, and not an ounce of worth..."
            });

            _messageEvents.Add(new MessageEvent() {
                Time = 15,
                Condition = InitialSwitchNotActivated,
                TextMessage = "I really need to find a way to open the gate."
            });

            _messageEvents.Add(new MessageEvent() {
                TriggerId = "Fire Trigger",
                TextMessage = "There must be a way around this..."
            });

            _messageEvents.Add(new MessageEvent() {
                TriggerId = "Outside Trigger",
                TextMessage = "Ah...outside...if I can only find an exit..."
            });

            _messageEvents.Add(new MessageEvent() {
                TriggerId = "Vent Trigger",
                CancelTriggerId = "Fire Trigger",
                TextMessage = "Hmmm...feels like \"Alien\""
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
            if(me.AudioMessage) Message(me.AudioMessage);
            else Message(me.TextMessage);
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

        public class MessageEvent : IComparable<MessageEvent>, IComparable {
            //use negative time for triggered events
            public float Time = -1;
            public string TriggerId;
            public string CancelTriggerId;
            public Func<bool> Condition;
            public string TextMessage;
            public AudioClip AudioMessage;

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