/*
    NQueensMagic, a program that solves the N queens puzzle
    Copyright (C) 2017 Aaron Alfer
    
    NQueensMagic is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    
    NQueensMagic is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using NQueensMagic;
using System;

namespace NQueensMagicConsole
{
    internal class ConsoleInterface
    {
        /// <summary> An assignment operator that is used in console commands. </summary>
        private const char _assignOpr = '=';

        private static void Main()
        {
            DisplayProductInfo();
            SubscribeToEvents();
            Master.Initialize();

            // Pending user input:
            while (true) ReadCommand(Console.ReadLine());
        }

        private static void SubscribeToEvents()
        {
            Events.VariableChanged += Notify_VariableChanged;
            Events.NoSolutionFound += Notify_NoSolutionFound;
            Events.LogSaved += Notify_LogSaved;
            Events.OperationStarted += Notify_OperationStarted;
            Events.OperationFinished += Notify_OperationFinished;
            Events.Progress += Notify_Progress;
            Events.FinishedSearching += Notify_FinishedSearching;
            Events.AllSolutionsFound += Notify_AllSolutionsFound;
            Events.SingleSolutionSerialized += Notify_SingleSolutionSerialized;

            Events.VariableOutOfRange += DisplayVariableOutOfRangeError;
            Events.VariableNotEquals += DisplayVariableNotEqualsError;
            Events.EmptyPresetDetected += DisplayEmptyPresetError;
            Events.UnsolvablePresetDetected += DisplayUnsolvablePresetError;
            Events.PresetElementUnrecognized += DisplayUnrecognizedPresetElementError;
        }

        #region Input

        /// <summary> Interprets user input and takes actions accordingly. </summary>
        /// <param name="cmd"> Some command entered by the user. </param>
        private static void ReadCommand(string cmd)
        {
            /* White-space and empty commands are ignored.
               Other values are considered valid by being recognized as either an assignment or a call. */

            if (!String.IsNullOrWhiteSpace(cmd) && !HandleAssignment(cmd) && !HandleCall(cmd))
                DisplayErrorMessage("Unrecognized character sequence");
        }

        /// <summary>
        /// Checks whether the provided command is an assignment, and if it is, performs the operation.
        /// </summary>
        /// <param name="cmd"> Some command entered by the user. </param>
        /// <returns> Is the command an assignment? </returns>
        private static bool HandleAssignment(string cmd)
        {
            /* First check for an operator. If it doesn't exist,
               we can safely say this is not an assignment. */

            var oprIdx = cmd.IndexOf(_assignOpr);
            if (oprIdx < 0) return false;

            /* From now on, the method will return true because it knows the command
               contains an assignment operator, hence the command is indeed
               an assignment operation attempt, successful or not. */

            // What goes before the operator is, logically, a variable name.
            var @var = cmd.Substring(0, oprIdx);

            // Handle empty variable case.
            if (String.IsNullOrWhiteSpace(@var))
            {
                DisplayErrorMessage("No variable has been provided");
                return true;
            }

            // All that goes after the operator should be a value the user wants to assign the variable to.
            var @value = cmd.Length > oprIdx + 1 ? cmd.Substring(oprIdx + 1) : string.Empty;

            // Handle empty value case.
            if (String.IsNullOrWhiteSpace(@value))
            {
                DisplayErrorMessage("No value has been provided");
                return true;
            }

            // Try to perform the operation.
            Assign(@var.Trim(), @value.Trim());
            return true;
        }

        /// <summary> Tries to perform an assignment operation ordered by the user. </summary>
        /// <param name="var"> Text representation of a variable name. </param>
        /// <param name="value"> Text representation of a value. </param>
        private static void Assign(string @var, string @value)
        {
            // Convert everything to lower case to increase flexibility and avoid confusion.
            switch (@var.ToLower())
            {
                // Here goes the list of all keywords representing variables.

                case "mode":
                    if (AssignMode(@value)) return;
                    break;
                case "n":
                    if (AssignN(@value)) return;
                    break;
                case "threads":
                    if (AssignNumberOfThreads(@value)) return;
                    break;
                case "psize" when Master.Mode == OperatingMode.Completion:
                    if (AssignPresetSize(@value)) return;
                    break;
                case "preset" when Master.Mode == OperatingMode.Completion:
                    /* Because manual preset adjustment is a very specific operation,
                       instead of a regular assignment method, we call a special one. */
                    Serializer.DeserializePreset(@value);
                    return;
                default:
                    // If control ends up here then there's something wrong with the variable name.
                    DisplayErrorMessage($"Unknown variable ({@var})");
                    return;
            }

            // If control made it to this point then we have some invalid value.
            DisplayErrorMessage($"Invalid value ({@value})");
        }

        /// <summary> Tries to perform a call ordered by the user. </summary>
        /// <param name="cmd"> Some command entered by the user. </param>
        /// <returns> Was an attempt successful? </returns>
        private static bool HandleCall(string cmd)
        {
            // Again, boost flexibility by removing empty spaces and converting all to lower case.
            switch (cmd.Trim().ToLower())
            {
                // Here goes the list of all keywords representing special calls.

                case "solve":
                    // Calls the main program function. That is, search for solutions to the puzzle.
                    Master.Solve();
                    return true;
                case "random" when Master.Mode == OperatingMode.Completion:
                    // Prompts the program to generate a random preset in the Completion mode.
                    PresetMaker.GenerateRandomPreset();
                    return true;
                case "meaning of life":
                case "the meaning of life":
                    // As a bonus, this wonderful piece of software can give the answer to the ultimate question of life.
                    Console.WriteLine(42);
                    // Yes, my sense of humour could be better than that...
                    return true;
                default:
                    // No valid keyword has been provided.
                    return false;
            }
        }

        #region Assignment Methods

        /// <summary> Attempts to assign the current operating mode to a given value. </summary>
        /// <param name="value"> Text representation of an operating mode. </param>
        /// <returns> Was an attempt successful? </returns>
        private static bool AssignMode(string @value)
        {
            // Safely parse to enum and perform boundaries check for further security.

            if (Enum.TryParse<OperatingMode>(@value, true, out var mode)
                && mode >= OperatingMode.All && mode <= OperatingMode.Completion)
            {
                Master.Mode = mode;
                return true;
            }
            return false;
        }

        /// <summary> Attempts to assign N to a given value. </summary>
        /// <param name="value"> Text representation of an integral value. </param>
        /// <returns> Was an attempt successful? </returns>
        private static bool AssignN(string @value)
        {
            if (int.TryParse(@value, out var x))
            {
                Master.N = x;
                return true;
            }
            return false;
        }

        /// <summary> Attempts to assign the number of threads to a given value. </summary>
        /// <param name="value"> Text representation of an integral value. </param>
        /// <returns> Was an attempt successful? </returns>
        private static bool AssignNumberOfThreads(string @value)
        {
            if (int.TryParse(@value, out var x))
            {
                Master.NumberOfThreads = x;
                return true;
            }
            return false;
        }

        /// <summary> Attempts to assign the preset size to a given value. </summary>
        /// <param name="value"> Text representation of an integral value. </param>
        /// <returns> Was an attempt successful? </returns>
        private static bool AssignPresetSize(string @value)
        {
            if (int.TryParse(@value, out var x))
            {
                PresetMaker.Size = x;
                return true;
            }
            return false;
        }

        #endregion

        #endregion

        #region Output

        /// <summary> Outputs the product information to the console window. </summary>
        private static void DisplayProductInfo()
        {
            var info = Master.ProductInfo;
            Console.WriteLine("{0} {1} by {2}", info.ProductName, info.ProductVersion, info.CompanyName);
        }

        #region Errors

        /// <summary> Displays an error message to the console window. </summary>
        private static void DisplayErrorMessage(params object[] args)
        {
            Console.WriteLine("Error: {0}", args[0]);
        }

        private static void DisplayVariableOutOfRangeError(params object[] args)
        {
            DisplayErrorMessage($"{args[0]} must be >= {args[1]} and <= {args[2]}");
        }

        private static void DisplayVariableNotEqualsError(params object[] args)
        {
            DisplayErrorMessage($"{args[0]} is {args[1]} but it must equal {args[2]}");
        }

        private static void DisplayUnrecognizedPresetElementError(params object[] args)
        {
            DisplayErrorMessage($"Element {args[0]} ({args[1]}) is neither an integer nor a skip sign");
        }

        private static void DisplayUnsolvablePresetError(params object[] args)
        {
            DisplayErrorMessage("Unsolvable preset");
        }

        private static void DisplayEmptyPresetError(params object[] args)
        {
            DisplayErrorMessage("Empty preset");
        }

        #endregion

        #region Notifications

        private static void Notify_VariableChanged(params object[] args)
        {
            Console.WriteLine("{0} has been set to {1}", args[0], args[1]);
        }

        private static void Notify_NoSolutionFound(params object[] args)
        {
            // Play a low-frequency signal informing the user that the program has failed to find any solution.
            Console.Beep(300, 200);

            Console.WriteLine("No solution");
        }

        private static void Notify_LogSaved(params object[] args)
        {
            // Play a high-frequency signal informing the user that the cycle has ended successfully.
            Console.Beep();
        }

        private static void Notify_OperationStarted(params object[] args)
        {
            Console.WriteLine(args[0]);
        }

        private static void Notify_OperationFinished(params object[] args)
        {
            Console.Write("Finished in ");

            foreach (var arg in args)
                Console.WriteLine(arg);
        }

        private static void Notify_Progress(params object[] args)
        {
            Console.Write('|');
        }

        private static void Notify_FinishedSearching(params object[] args)
        {
            Console.WriteLine();
            Notify_OperationFinished(args[0]);
        }

        private static void Notify_AllSolutionsFound(params object[] args)
        {
            Console.Write("\n{0} solutions have been found", Stats.AllSolutionsTotal[Master.N - 1]);
        }

        private static void Notify_SingleSolutionSerialized(params object[] args)
        {
            Console.WriteLine("Solution: {0}", Serializer.Solution);
        }

        #endregion

        #endregion
    }
}