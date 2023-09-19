using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Base class for the Stimulus Presenting Objects (SPOs)

namespace BCIEssentials.StimulusObjects
{
   /// <summary>
    /// Base class for the Stimulus Presenting Objects (SPOs)
    /// </summary>
    public class SPO : MonoBehaviour
    {
        [Space(20)]
        [Tooltip("Invoked when the SPO Controller requests this stimulus to start.")]
        public UnityEvent StartStimulusEvent = new();

        [Tooltip("Invoked when the SPO Controller requests this stimulus to stop.")]
        public UnityEvent StopStimulusEvent = new();

        [Tooltip("Invoked when the SPO Controller selects this SPO")]
        public UnityEvent OnSelectedEvent = new();


        /// <summary>
        /// Determines if this object is available to be selected
        /// by the <see cref="Controller"/>;
        /// </summary>
        public bool Selectable = true;

        /// <summary>
        /// Assigned by the SPO Controller, this represents the
        /// index of this SPO in the controllers pool of selectables. 
        /// </summary>
        public int SelectablePoolIndex;

        /// <summary>
        /// Request this SPO stimulus to begin.
        /// </summary>
        /// <returns>The time at the beginning of this frame using <see cref="Time.time"/></returns>
        public virtual float StartStimulus()
        {
            StartStimulusEvent?.Invoke();

            //Stimulus request time
            return Time.time;
        }

        /// <summary>
        /// Request this SPO stimulus to end.
        /// </summary>
        public virtual void StopStimulus()
        {
            StopStimulusEvent?.Invoke();
        }

        /// <summary>
        /// When this SPO has been selected.
        /// </summary>
        public virtual void Select()
        {
            OnSelectedEvent?.Invoke();
        }

        //TODO: Remove when refactored training out
        // What to do when targeted for training selection
        public virtual void OnTrainTarget()
        {
            float scaleValue = 1.4f;
            Vector3 objectScale = transform.localScale;
            transform.localScale = new Vector3(objectScale.x * scaleValue, objectScale.y * scaleValue,
                objectScale.z * scaleValue);
        }

        // What to do when untargeted
        public virtual void OffTrainTarget()
        {
            float scaleValue = 1.4f;
            Vector3 objectScale = transform.localScale;
            transform.localScale = new Vector3(objectScale.x / scaleValue, objectScale.y / scaleValue,
                objectScale.z / scaleValue);
        }
    }
}
