using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.AI;
using OVGU.VAR.VRResist;


public class NPCController : MonoBehaviour
{
    private NavMeshAgent agent;

    public AudioSource audioSource;
    public Animator animator;
    public string nameForIdentification;
    public MessageHandler messageHandler;
    private EventLogger logger;

    [Header("Audio Configuration")]
    [SerializeField]
    [Tooltip("Audio clips available for this NPC")]
    public List<AudioClip> audioClips;

    [SerializeField]
    [Tooltip("User-friendly labels for audio clips - must match audioClips order")]
    public List<string> audioLabels;

    [SerializeField] private GameObject head;

    // Start is called before the first frame update
    void Start()
    {

        audioSource = head.GetComponent<AudioSource>();
        agent = gameObject.GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

    }

    // play audio clip and play animation
    public void Speak(string clipName, string animationTrigger)
    {
        Speak(clipName);
        StartCoroutine(TalkAnimationTrigger(animationTrigger));
    }

    //play audio clip without animation
    public void Speak(string clipName)
    {
        AudioClip audioClip = audioClips.Find(obj => obj.name == clipName);
        audioSource.PlayOneShot(audioClip);

        EventMessage msg = new EventMessage("speakStart", new string[]{
            nameForIdentification,
            audioClip.length.ToString("R",CultureInfo.InvariantCulture),
            clipName

        });
        messageHandler.SendEventMessageToClient(msg);
    }

    //stops speaking immediately
    public void stopSpeaking()
    {
        audioSource.Stop();

        EventMessage msg = new EventMessage("speakStop", new string[] { nameForIdentification });
        messageHandler.SendEventMessageToClient(msg);
    }

    // set animation bool for duraration of audio clip
    IEnumerator TalkAnimationTrigger(string animationTrigger)
    {
        animator.SetBool(animationTrigger, true);
        yield return new WaitWhile(() => audioSource.isPlaying);
        animator.SetBool(animationTrigger, false);
    }

    #region MessageHandler Integration Methods

    /// <summary>
    /// Get user-friendly label for a specific audio clip
    /// </summary>
    /// <param name="clipName">Name of the audio clip</param>
    /// <returns>User-friendly label or clip name as fallback</returns>
    public string GetLabelForClip(string clipName)
    {
        int index = audioClips.FindIndex(clip => clip.name == clipName);
        if (index >= 0 && index < audioLabels.Count)
            return audioLabels[index];
        return clipName; // Fallback to clip name
    }

    /// <summary>
    /// Get all audio clip names and labels for MessageHandler integration
    /// </summary>
    /// <returns>Tuple containing clip names and corresponding labels</returns>
    public (string[] clipNames, string[] labels) GetAudioData()
    {
        string[] clipNames = new string[audioClips.Count];
        for (int i = 0; i < audioClips.Count; i++)
        {
            clipNames[i] = audioClips[i].name;
        }

        string[] labels;
        if (audioLabels.Count == audioClips.Count)
        {
            labels = audioLabels.ToArray();
        }
        else
        {
            // Fallback: use clip names if labels are missing or mismatched
            Debug.LogWarning($"[NPCController] {nameForIdentification}: audioLabels count ({audioLabels.Count}) doesn't match audioClips count ({audioClips.Count}). Using clip names as fallback.");
            labels = clipNames;
        }

        return (clipNames, labels);
    }

    /// <summary>
    /// Validate that audio clips and labels are properly configured
    /// </summary>
    /// <returns>True if configuration is valid</returns>
    public bool ValidateAudioConfiguration()
    {
        if (audioClips.Count == 0)
        {
            Debug.LogWarning($"[NPCController] {nameForIdentification}: No audio clips configured!");
            return false;
        }

        if (audioLabels.Count != audioClips.Count)
        {
            Debug.LogWarning($"[NPCController] {nameForIdentification}: Audio labels count ({audioLabels.Count}) doesn't match clips count ({audioClips.Count})!");
            return false;
        }

        // Check for null clips
        for (int i = 0; i < audioClips.Count; i++)
        {
            if (audioClips[i] == null)
            {
                Debug.LogError($"[NPCController] {nameForIdentification}: Audio clip at index {i} is null!");
                return false;
            }
        }

        return true;
    }

    #endregion
}
