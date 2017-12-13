using System;

namespace NQueensMagic
{
    /// <summary>
    /// Contains a set of events that can be communicated to an external UI or library.
    /// </summary>
    public static class Events
    {
        /// <summary> Serves as a universal event handler. </summary>
        /// <param name="args"> Any set of arguments. </param>
        public delegate void CommonEventHandler(params object[] args);

        #region Notifications

        /// <summary> Any major global variable or property has been modified. </summary>
        public static event CommonEventHandler VariableChanged;

        /// <summary> No solution was found during the search stage in Completion mode. </summary>
        public static event CommonEventHandler NoSolutionFound;

        /// <summary> One of the crucial procedures has begun (precalculation, search, preset generation, etc.) </summary>
        public static event CommonEventHandler OperationStarted;

        /// <summary> Some crucial procedure (precalculation, search, preset generation, etc.) has finished. </summary>
        public static event CommonEventHandler OperationFinished;

        /// <summary> A log file has been generated. </summary>
        public static event CommonEventHandler LogSaved;

        /// <summary> A search sub-stage has passed. </summary>
        public static event CommonEventHandler Progress;

        /// <summary> The whole search stage has passed. </summary>
        public static event CommonEventHandler FinishedSearching;

        /// <summary> All solutions have just been found in All mode. </summary>
        public static event CommonEventHandler AllSolutionsFound;

        /// <summary> A single solution has been serialized. </summary>
        public static event CommonEventHandler SingleSolutionSerialized;

        #endregion

        #region Errors

        /// <summary> An attempt to set a global variable or property out of its range was made. </summary>
        public static event CommonEventHandler VariableOutOfRange;

        /// <summary> An attempt to set a global variable or property to a wrong value was made. </summary>
        public static event CommonEventHandler VariableNotEquals;

        /// <summary> A preset with no elements has been defined. </summary>
        public static event CommonEventHandler EmptyPresetDetected;

        /// <summary> A preset that cannot be solved has been detected. </summary>
        public static event CommonEventHandler UnsolvablePresetDetected;

        /// <summary> Some element in a preset has not been recognized as valid. </summary>
        public static event CommonEventHandler PresetElementUnrecognized;

        #endregion

        #region Notifications (Invoking Methods)

        /// <summary> Invokes the event VariableChanged. </summary>
        /// <param name="variableName"> A variable's name. </param>
        /// <param name="value"> A new value. </param>
        public static void OnVariableChanged(string variableName, object @value)
        {
            VariableChanged?.Invoke(variableName, @value);
        }

        /// <summary> Invokes the event NoSolutionFound. </summary>
        public static void OnNoSolutionFound()
        {
            NoSolutionFound?.Invoke();
        }

        /// <summary> Invokes the event OperationStarted. </summary>
        /// <param name="message"> A message describing the operation. </param>
        public static void OnOperationStarted(string message)
        {
            OperationStarted?.Invoke(message);
        }

        /// <summary> Invokes the event OperationFinished. </summary>
        /// <param name="args"> Any details about the finished operation. </param>
        public static void OnOperationFinished(params object[] args)
        {
            OperationFinished?.Invoke(args);
        }

        /// <summary> Invokes the event LogSaved. </summary>
        public static void OnLogSaved()
        {
            LogSaved?.Invoke();
        }

        /// <summary> Invokes the event Progress. </summary>
        public static void OnProgress()
        {
            Progress?.Invoke();
        }

        /// <summary> Invokes the event FinishedSearching. </summary>
        /// <param name="timeSpan"> An elapsed time. </param>
        public static void OnFinishedSearching(TimeSpan timeSpan)
        {
            FinishedSearching?.Invoke(timeSpan);
        }

        /// <summary> Invokes the event AllSolutionsFound. </summary>
        public static void OnAllSolutionsFound()
        {
            AllSolutionsFound?.Invoke();
        }

        /// <summary> Invokes the event SingleSolutionSerialized. </summary>
        public static void OnSingleSolutionSerialized()
        {
            SingleSolutionSerialized?.Invoke();
        }

        #endregion

        #region Errors (Invoking Methods)

        /// <summary> Invokes the event VariableOutOfRange. </summary>
        /// <param name="variableName"> A variable's name. </param>
        /// <param name="minValue"> A minimum value. </param>
        /// <param name="maxValue"> A maximum value. </param>
        public static void OnVariableOutOfRange(string variableName, object minValue, object maxValue)
        {
            VariableOutOfRange?.Invoke(variableName, minValue, maxValue);
        }

        /// <summary> Invokes the event VariableNotEquals. </summary>
        /// <param name="variableName"> A variable's name. </param>
        /// <param name="currentValue"> A variable's value. </param>
        /// <param name="desiredValue"> A variable's desired value. </param>
        public static void OnVariableNotEquals(string variableName, object currentValue, object desiredValue)
        {
            VariableNotEquals?.Invoke(variableName, currentValue, desiredValue);
        }

        /// <summary> Invokes the event EmptyPresetDetected. </summary>
        public static void OnEmptyPresetDetected()
        {
            EmptyPresetDetected?.Invoke();
        }

        /// <summary> Invokes the event UnsolvablePresetDetected. </summary>
        public static void OnUnsolvablePresetDetected()
        {
            UnsolvablePresetDetected?.Invoke();
        }

        /// <summary> Invokes the event PresetElementUnrecognized. </summary>
        /// <param name="index"> An element's index. </param>
        /// <param name="value"> An element's value. </param>
        public static void OnPresetElementUnrecognized(int index, string @value)
        {
            PresetElementUnrecognized?.Invoke(index, @value);
        }

        #endregion
    }
}