using System.Collections;
using BCIEssentials.Controllers;
using BCIEssentials.Utilities;
using BCIEssentials.StimulusObjects;
using UnityEngine;

namespace BCIEssentials.ControllerBehaviors
{
    public class MIControllerBehavior : BCIControllerBehavior
    {
        public override BCIBehaviorType BehaviorType => BCIBehaviorType.MI;

        // Variables related to Iterative training
        public int numSelectionsBeforeTraining = 3; // How many selections to make before creating the classifier
        public int numSelectionsBetweenTraining = 3; // How many selections to make before updating the classifier

        protected int selectionCounter = 0;
        protected int updateCounter = 0;

        public override void PopulateObjectList(SpoPopulationMethod populationMethod = SpoPopulationMethod.Tag)
        {
            base.PopulateObjectList(populationMethod);

            // Warn about the number of objects to be selected from, if greater than 2
            if (_selectableSPOs.Count > 2)
            {
                Debug.LogWarning("Warning: Selecting between more than 2 objects!");
            }
        }

        protected override IEnumerator WhileDoIterativeTraining()
        {
            // Generate the target list
            PopulateObjectList();

            int numOptions = _selectableSPOs.Count;

            // Create a random non repeating array 
            int[] trainArray = new int[numTrainingSelections];
            trainArray = ArrayUtilities.GenerateRNRA(numTrainingSelections, 0, numOptions);
            LogArrayValues(trainArray);

            yield return 0;


            // Loop for each training target
            for (int i = 0; i < numTrainingSelections; i++)
            {

                if (selectionCounter >= numSelectionsBeforeTraining)
                {
                    if (updateCounter == 0)
                    {
                        // update the classifier
                        Debug.Log($"Updating the classifier after {selectionCounter} selections");
                        marker.Write("Update Classifier");
                        updateCounter++;
                    }
                    else if (selectionCounter >=
                             numSelectionsBeforeTraining + (updateCounter * numSelectionsBetweenTraining))
                    {
                        // update the classifier
                        Debug.Log($"Updating the classifier after {selectionCounter} selections");
                        marker.Write("Update Classifier");
                        updateCounter++;
                    }
                }

                // Get the target from the array
                trainTarget = trainArray[i];

                // 
                Debug.Log($"Running training selection {i} on option {trainTarget}");

                // Turn on train target
                _selectableSPOs[trainTarget].OnTrainTarget();

                // Go through the training sequence
                yield return new WaitForSecondsRealtime(pauseBeforeTraining);


                StartStimulusRun();
                for (int j = 0; j < (numTrainWindows - 1); j++)
                {
                    yield return new WaitForSecondsRealtime(windowLength + interWindowInterval);

                    if (shamFeedback)
                    {
                        _selectableSPOs[trainTarget].Select();
                    }
                }

                StopStimulusRun();

                // Take a break
                yield return new WaitForSecondsRealtime(trainBreak);

                // Turn off train target
                _selectableSPOs[trainTarget].OffTrainTarget();

                // Reset objects

                // Take a break
                yield return new WaitForSecondsRealtime(trainBreak);

                trainTarget = 99;
                selectionCounter++;
            }

            // Send marker
            marker.Write("Training Complete");

            yield return 0;

        }

        // Why is this public when the other training overrides are protected?
        //TODO: Figure out why protected here isn't working, but is for other training types
        public override IEnumerator WhileDoSingleTraining()
        {
            PopulateObjectList();

            // For the time being, only allow single training on a single object
            if (_selectableSPOs.Count > 1)
            {
                Debug.LogError("Single training only allowed on a single object");
                yield break;
            }

            SPO targetObject = _selectableSPOs[0];

            Debug.Log("Starting single training");
            // For a single, specified SPO, run a single training trial
            if (targetObject != null)
            {
                // Turn on train target - 
                targetObject.OnTrainTarget();
                Debug.Log($"Running single training on option {targetObject.name}");

                // Get the index of the target object
                int targetID = targetObject.ObjectID;
                Debug.Log($"Running single training on option {targetID}");

                // For each window in the trial
                for (int j = 0; j < (numTrainWindows); j++)
                {
                    // Send the marker for the window
                    marker.Write($"mi, 1, {targetID}, {windowLength}");

                    yield return new WaitForSecondsRealtime(windowLength);

                    if (shamFeedback)
                    {
                        targetObject.Select();
                    }
                }

                // Turn off train target
                targetObject.OffTrainTarget();
            }
            else
            {
                Debug.LogError("No target object specified for single training");
            }

            marker.Write("Trial Ends");

            yield return null;
        }

        /// <summary>
        /// Select an object from <see cref="SelectableSPOs"/> based on the SPO ObjectID,
        /// not the index in the list.
        /// </summary>
        /// <param name="objectIndex"></param>
        /// <param name="stopStimulusRun"></param>
        public override void SelectSPO(int objectID, bool stopStimulusRun = false)
        {
            //If the current training type is not single, then run the base method
            if (CurrentTrainingType != BCITrainingType.Single)
            {
                base.SelectSPO(objectID, stopStimulusRun);
                return;
            }
            
            // If the current training type is single, then make sure the SPO selection is
            // based on the object ID
            var objectCount = _selectableSPOs.Count;
            if (objectCount == 0)
            {
                Debug.LogWarning("No Objects to select");
                return;
            }

            //Not sure if I want to include this or not - leaving out for now, but can be added back in
            //If the SPO objects only have ObjectID == 0 then return
            // if (_selectableSPOs.TrueForAll(spo => spo.ObjectID == 0) && _selectableSPOs.Count > 1)
            // {
            //     Debug.LogWarning("All SPOs have ObjectID == 0");
            //     return;
            // }

            if(_selectableSPOs.TrueForAll(spo => spo.ObjectID != objectID))
            {
                Debug.LogWarning($"ObjectID {objectID} not found in the list of SPOs");
                return;       
            }

            // Select the SPO(s) with the matching ObjectID. Doesn't currently break so has to check all
            foreach (var spo in _selectableSPOs)
            {
                if (spo.ObjectID == objectID)
                {
                    spo.Select();
                    LastSelectedSPO = spo;
                    Debug.Log($"SPO '{spo.gameObject.name}' selected.");
                }
            }
            //I'm unsure if this is needed, but I'm mimicking how the base method handles this
            if (stopStimulusRun)
            {
                StopStimulusRun();
            }

        }

        public override void SelectSPOAtEndOfRun(int objectIndex)
        {
            base.SelectSPOAtEndOfRun(objectIndex);
        }

        public override void ReceiveMarkers()
        {
             //If the current training type is not single, then run the base method
            if (CurrentTrainingType != BCITrainingType.Single)
            {
                Debug.Log("Using base method for receive markers as single training was not done");
                base.ReceiveMarkers();
                return;
            }

            if (!response.Connected)
            {
                response.Connect();
            }

            if (response.Polling)
            {
                response.StopPolling();
            }

            //Ping count
            int pingCount = 0;
            response.StartPolling(responses =>
            {
                foreach (var response in responses)
                {
                    if (response.Equals("ping"))
                    {
                        pingCount++;
                        if (pingCount % 100 == 0)
                        {
                            Debug.Log($"Ping Count: {pingCount}");
                        }
                    }
                    else if (!response.Equals(""))
                    {
                        //Question: Why do we only get here if the first value is good, but are then concerned about all other values?
                        //Question: Do we get more than once response string?

                        //Trying to handle if the response is square brackets and other outputs from bessy python.
                        string myResponse = response.Replace("[", "").Replace("]", "").Trim();
                        string[] responseParts = myResponse.Split(new char[] { ' ', '.' }, System.StringSplitOptions.RemoveEmptyEntries);
                        int[] responseValueNumbers = new int[responseParts.Length];
                        Debug.Log("response : {response}");
                        for (int i = 0; i < responseParts.Length; i++)
                        {
                        if (int.TryParse(responseParts[i], out var objectID))
                            {
                                Debug.Log("Selecting SPO with ID: " + objectID);
                                SelectSPO(objectID, false);
                                responseValueNumbers[i] = objectID;
                            }
                        }

                        // for (int i = 0; i < responses.Length; i++)
                        // {
                        //     Debug.Log($"response : {response}");
                        //     // Deal with the weird response coming from python for the single training
                        //     string[] myResponses = response.Replace("[", "").Replace("]", "").Split(new char[] { ' ', '.' }, System.StringSplitOptions.RemoveEmptyEntries);
                            
                        //     if (int.TryParse(response, out var objectID))
                        //     {
                        //         Debug.Log("Selecting the SPO!");
                        //         SelectSPO(objectID, false);
                        //     }
                        // }
                    }
                }
            });
        }

        public override void UpdateClassifier()
        {
            Debug.Log("Updating the classifier");
            marker.Write("Training Complete");
        }

        protected override IEnumerator SendMarkers(int trainingIndex = 99)
        {
            // Make the marker string, this will change based on the paradigm
            while (StimulusRunning)
            {
                // Desired format is: [mi, number of options, training label (or -1 if n/a), window length] 
                string trainingString = trainingIndex <= _selectableSPOs.Count ? trainingIndex.ToString() : "-1";

                // Send the marker
                marker.Write($"mi, {_selectableSPOs.Count}, {trainingString}, {windowLength}");

                // Wait the window length + the inter-window interval
                yield return new WaitForSecondsRealtime(windowLength + interWindowInterval);


            }
        }
    }
}