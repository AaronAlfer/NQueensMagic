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

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NQueensMagic
{
    /// <summary> Handles serialization of solutions and presets, i.e. producing readable results. </summary>
    public static class Serializer
    {
        #region Constants

        /// <summary> The maximum number of lines in a single output file. </summary>
        public const int MaxLinesPerFile = 10000;

        /// <summary> The sign indicating an empty file (column) in a preset. </summary>
        public const string SkipSign = "_";

        #endregion

        #region Properties

        /// <summary> The current preset in a string format. </summary>
        public static string Preset { get; private set; }

        /// <summary> The lastly obtained single solution in a string format. </summary>
        public static string Solution { get; private set; }

        /// <summary> The time it took to serialize multiple solutions last time. </summary>
        public static TimeSpan Elapsed { get; private set; }

        #endregion

        #region Public Methods

        /// <summary> Serializes the current preset of queens. </summary>
        public static void SerializePreset()
        {
            Preset = PresetMaker.Preset.Occupancy.ToQueenString();
        }

        /// <summary> De-serializes a preset of queens. </summary>
        /// <param name="preset"> A preset to be de-serialized. </param>
        public static void DeserializePreset(string preset)
        {
            // Split a string into individual elements which are supposed to represent Y-coordinates.
            var elements = preset.Split(' ');

            // The number of elements should correspond to the current N.
            if (elements.Length != Master.N)
            {
                Events.OnVariableNotEquals("Number of elements", elements.Length, Master.N);
                return;
            }

            // Make sure the elements are valid integers, and get the details.
            if (!TryParsePreset(elements, out var info))
                return;

            // Empty and full presets do not pass. Finally, test the preset for validity i.e. make
            // sure it's solvable. If the preset passes, launch the solving algorithm right away
            // to reduce unnecessary wait.

            if (info.IsEmpty)
                Events.OnEmptyPresetDetected();
            else if (info.IsFull || !PresetMaker.TryManualPreset(info.Y_Coordinates))
                Events.OnUnsolvablePresetDetected();
            else
                Master.Solve();
        }

        /// <summary> Serializes a complete set of queens. </summary>
        /// <param name="solution"> A solution to be serialized. </param>
        public static void SerializeSolution(ulong[] solution)
        {
            if (solution == null)
                throw new Exception("An attempt to serialize a non-existing solution was made!");

            Solution = solution.ToQueenString();
            Events.OnSingleSolutionSerialized();
        }

        /// <summary> Serializes multiple solutions and saves them to text files. </summary>
        /// <param name="solutions"> A collection of solutions to be serialized. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SerializeSolutions(ulong[][] solutions)
        {
            Events.OnOperationStarted("Serializing...");
            Master.SW.Start();

            // The last solution's index:
            var lastIdx = Master.N - 1;
            // The total number of solutions to be produced in the output:
            var totalNumber = Stats.AllSolutionsTotal[lastIdx];
            // The number of text files to be created:
            var numberOfFiles = 1 + totalNumber / MaxLinesPerFile;

            // Each solution will be stored in a separate string. The array of
            // strings is further split down to chunks corresponding to each file.
            var result = new string[numberOfFiles][];

            for (var i = 0; i < numberOfFiles; i++)
                result[i] = new string[MaxLinesPerFile];

            PopulateTextResult(solutions, result, totalNumber, lastIdx);
            WriteResultToFiles(result);

            Elapsed = Master.SW.Elapsed;
            Master.SW.Reset();
            Events.OnOperationFinished(Elapsed);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Serializes multiple solutions and writes the result to an array of strings.
        /// </summary>
        /// <param name="solutions"> A collection of matrices representing solutions. </param>
        /// <param name="result"> An array of strings to be used as output. </param>
        /// <param name="totalNumber"> The target number of solutions (including the derived ones). </param>
        /// <param name="lastIdx"> The last solution's index. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PopulateTextResult(ulong[][] solutions, string[][] result, int totalNumber, int lastIdx)
        {
            // The number of 'unique' solutions aka solutions found during the search:
            var uniqueNumber = solutions.Length;

            // A container for integral arrays of Y-coordinates. Only the 'unique' solutions will be stored here.
            // This data is to be used in the 2nd loop where the derived solutions are generated.
            var uniqueItems = new int[uniqueNumber][];

            // Get the 'unique' solutions:
            Parallel.For(0, uniqueNumber, Master.ThreadingOptions, i =>
            {
                // Get an array of Y-coordinates:
                int[] arr = solutions[i].ToYCoordinates();
                // Transform the array into a string:
                var line = string.Join(" ", arr);
                // Put the string into the output array:
                WriteLine(result, line, i);
                // Put the integral array into the outer collection of 'unique' items:
                uniqueItems[i] = arr;
            });

            // Get the derived solutions:
            Parallel.For(uniqueNumber, totalNumber, Master.ThreadingOptions, i =>
            {
                // Get a unique array of Y-coordinates by the index:
                int[] arr = uniqueItems[i - uniqueNumber];

                var sb = new StringBuilder();

                /* Now we'll make a string out of the array by appending elements in the reverse order.
                   The result will be a derived solution: the same as a 'unique' one but the other way around.
                   The last element is added outside of the loop in order to avoid including a redundant empty space at the end. */

                for (var j = lastIdx; j > 0; j--)
                    sb.Append($"{arr[j]} ");

                var line = $"{sb.ToString()}{arr[0]}";
                WriteLine(result, line, i);
            });
        }

        /// <summary> Writes a line of text to a jagged string array. </summary>
        /// <param name="result"> A container. </param>
        /// <param name="line"> A line of text. </param>
        /// <param name="index"> A line's global index. </param>
        private static void WriteLine(string[][] result, string line, int index)
        {
            var fileIdx = index / MaxLinesPerFile;
            var lineIdx = index % MaxLinesPerFile;
            result[fileIdx][lineIdx] = $"{index + 1}. {line}";
        }

        /// <summary> Writes lines of text to files. </summary>
        /// <param name="result"> A source container. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteResultToFiles(string[][] result)
        {
            Master.CreateOutputDirectory(out var dirPath);

            var filePath = $@"{dirPath}\Solutions{Master.N}";
            var manyFiles = result.Length > 1;

            for (var i = 0; i < result.Length; i++)
            {
                // Index files if there are more than one.
                var fileNum = manyFiles ? String.Format("_{0:D3}", i + 1) : string.Empty;
                File.WriteAllLines($"{filePath}{fileNum}.txt", result[i]);
            }
        }

        /// <summary>
        /// Tries to parse a manually defined preset and stores the result in a specialized data structure.
        /// </summary>
        /// <param name="elements"> An array of Y-coordinates. </param>
        /// <param name="info"> Integral Y-coordinates, fullness flag and emptiness flag. </param>
        /// <returns> Is input valid? </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryParsePreset(string[] elements, out PresetInfo info)
        {
            info = new PresetInfo();

            for (var i = 0; i < Master.N; i++)
            {
                var e = elements[i];

                /* Each element should either be an integer representing a Y-coordinate
                   or a skip sign indicating a missing queen. */

                if (int.TryParse(e, out var y))
                {
                    // A Y-coordinate must not exceed the range.

                    if (y >= 0 && y < Master.N)
                    {
                        info.Y_Coordinates[i] = y;
                        // As soon as we find a valid coordinate, the set can't be empty:
                        info.IsEmpty = false;
                    }
                    else
                    {
                        Events.OnVariableOutOfRange($"Element {i} ({y})", 0, Master.N - 1);
                        return false;
                    }
                }
                else if (e == SkipSign)
                {
                    info.Y_Coordinates[i] = -1;
                    // If there's at least 1 skip sign then the set is definitely not full:
                    info.IsFull = false;
                }
                else
                {
                    Events.OnPresetElementUnrecognized(i, e);
                    return false;
                }
            }
            return true;
        }

        #endregion

        /// <summary> A set of variables describing a manually defined preset of queens. </summary>
        private class PresetInfo
        {
            public int[] Y_Coordinates = new int[Master.N];
            public bool IsEmpty = true;
            public bool IsFull = true;
        }
    }
}