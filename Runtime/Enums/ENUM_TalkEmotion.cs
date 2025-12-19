using UnityEngine;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Enum defining different emotional states for NPC speech animations
    /// Used to trigger appropriate facial expressions and body language during dialogue
    /// </summary>
    public enum ENUM_TalkEmotion
    {
        [Tooltip("Default idle animation - minimal movement")]
        Idle,

        [Tooltip("Happy, cheerful speaking animation")]
        Happy,

        [Tooltip("Angry, frustrated speaking animation")]
        Angry,

        [Tooltip("Annoyed, irritated speaking animation")]
        Annoyed,

        [Tooltip("Neutral, calm speaking animation")]
        Neutral,

        [Tooltip("Sad, melancholy speaking animation")]
        Sad,

        [Tooltip("Excited, energetic speaking animation")]
        Excited,

        [Tooltip("Concerned, worried speaking animation")]
        Concerned,

        [Tooltip("Professional, formal speaking animation")]
        Professional
    }
}
