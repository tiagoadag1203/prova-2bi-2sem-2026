using System;
using System.Collections.Generic;
using UnityEngine;

namespace XequeDeTinta.Data
{
    [Serializable]
    public class EventOutcome
    {
        public EventOutcomeKind kind;
        public int value;
        public int value2;
    }

    [Serializable]
    public class EventChoice
    {
        public string text;
        public List<EventOutcome> outcomes = new List<EventOutcome>();
    }

    [CreateAssetMenu(menuName = "Xeque de Tinta/Evento", fileName = "Event_Novo")]
    public class EventData : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea] public string body;
        public EventChoice choiceA = new EventChoice();
        public EventChoice choiceB = new EventChoice();
    }
}
