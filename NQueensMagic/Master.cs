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

using NQueensMagic.Search;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace NQueensMagic
{
    /// <summary>
    /// Determines how the program should behave and what result it should yield.
    /// </summary>
    public enum OperatingMode
    {
        /// <summary> Will search for all solutions. </summary>
        All,
        /// <summary> Will try to complete a given preset. </summary>
        Completion
    }

    /// <summary>
    /// The main class of NQueensMagic.
    /// </summary>
    /// <remarks>
    /// Combines the following activities: control over the N value,
    /// operating mode control, search initialization, log file production.
    /// </remarks>
    public static class Master
    {
        #region Constants

        /// <summary> The default value of N. </summary>
        public const int N_Default = 8;
        /// <summary> The lower limit of N. </summary>
        public const int N_Min = 4;
        /// <summary> The maximum value of N in 'All' mode. </summary>
        public const int N_Max_All = 16;
        /// <summary> The maximum value of N in 'Completion' mode. </summary>
        public const int N_Max_Completion = 1024;
        /// <summary> The default number of concurrent threads. </summary>
        public const int DefaultNumberOfThreads = 4;
        /// <summary> The maximum number of concurrent threads. </summary>
        public const int MaxNumberOfThreads = 32768;

        #endregion

        #region Properties

        /// <summary> The current operating mode. </summary>
        public static OperatingMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                Events.OnVariableChanged("Mode", value);

                // As soon as the mode changes, the max N should change accordingly:
                UpdateN_Max(value);

                // This should yield true only in the first instance (when N isn't yet initialized):
                if (_n == 0) return;

                switch (value)
                {
                    case OperatingMode.All:
                        PresetMaker.Close();
                        break;
                    case OperatingMode.Completion:
                        PresetMaker.Initialize();
                        break;
                }
            }
        }

        /// <summary>
        /// The longitudinal size of the board in squares and also the number of queens to place.
        /// </summary>
        public static int N
        {
            get => _n;
            set
            {
                if (value < N_Min || value > N_Max)
                {
                    Events.OnVariableOutOfRange("N", N_Min, N_Max);
                    return;
                }

                var oldN = _n;
                _n = value;
                Events.OnVariableChanged("N", value);

                // As N has been changed, it is absolutely necessary to reevaluate the board right away:
                BitBase.Update();

                if (_mode == OperatingMode.Completion)
                {
                    // Update the preset to match the new N value:
                    PresetMaker.UpdateProportionally(oldN);
                }
            }
        }

        /// <summary> The maximum number of concurrent threads. </summary>
        public static int NumberOfThreads
        {
            get => ThreadingOptions.MaxDegreeOfParallelism;
            set
            {
                if (value < 0 || value > MaxNumberOfThreads)
                {
                    Events.OnVariableOutOfRange("Threads", 1, MaxNumberOfThreads);
                    return;
                }

                ThreadingOptions.MaxDegreeOfParallelism = value;
            }
        }

        /// <summary> The upper limit of N (defined by the operating mode). </summary>
        public static int N_Max { get; private set; }

        /// <summary> Global options for the program's parallel behaviour. </summary>
        public static ParallelOptions ThreadingOptions { get; } = new ParallelOptions();

        /// <summary> The stopwatch for measuring performance of each critical process. </summary>
        public static Stopwatch SW { get; } = new Stopwatch();

        /// <summary> Product information (name, version, authors, etc). </summary>
        public static FileVersionInfo ProductInfo { get; } = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

        #endregion

        #region Private Fields

        /// <summary> Indicates whether the program has been initialized. </summary>
        private static bool _initialized;

        private static int _n;
        private static OperatingMode _mode;

        private static readonly string _programPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        #endregion

        #region Public Methods

        /// <summary> Initializes the program. </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            BitBase.Initialize();
            Mode = OperatingMode.All;
            N = N_Default;
            NumberOfThreads = DefaultNumberOfThreads;
            _initialized = true;
        }

        /// <summary> Performs the main cycle: search, serialization and log file saving. </summary>
        public static void Solve()
        {
            switch (_mode)
            {
                case OperatingMode.All:
                    var totalSolver = new TotalSolver();
                    totalSolver.Run();
                    Serializer.SerializeSolutions(totalSolver.Solutions);
                    SaveLog();
                    break;
                case OperatingMode.Completion:
                    var complSolver = new CompletionSolver();
                    complSolver.Run();
                    if (complSolver.Solution != null)
                    {
                        Serializer.SerializeSolution(complSolver.Solution);
                        SaveLog();
                    }
                    else // Failed to find a solution.
                    {
                        Events.OnNoSolutionFound();
                        // Generate a new preset right away:
                        PresetMaker.GenerateRandomPreset();
                    }
                    break;
            }
        }

        /// <summary> Creates an output directory. </summary>
        /// <param name="path"> The directory's path. </param>
        public static void CreateOutputDirectory(out string path)
        {
            path = $@"{_programPath}\Output\{_mode}\N{_n}";
            Directory.CreateDirectory(path);
        }

        #endregion

        #region Private Methods

        /// <summary> Updates the upper limit of N according to the mode selected. </summary>
        /// <param name="mode"> A new mode. </param>
        private static void UpdateN_Max(OperatingMode mode)
        {
            switch (mode)
            {
                case OperatingMode.All:
                    N_Max = N_Max_All;
                    break;
                case OperatingMode.Completion:
                    N_Max = N_Max_Completion;
                    break;
            }

            // Reevaluate N as it might be out of bounds now:
            if (_n > N_Max) N = N_Default;
        }

        /// <summary> Creates a log file containing performance results and other data. </summary>
        private static void SaveLog()
        {
            CreateOutputDirectory(out var dirPath);

            var timeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var filePath = $@"{dirPath}\Log_{_n}_{_mode}_{timeStamp}.txt";
            var contents = LogContents();

            if (_mode == OperatingMode.Completion)
            {
                contents.InsertRange(4, LogSetupInfo());
                // Remove the "Serialization ET" part as it's not relevant when there's only 1 solution:
                contents.RemoveAt(8);
                // Put the solution into the log file (no separate file is needed):
                contents.Add($"Solution: {Serializer.Solution}");
            }

            File.WriteAllLines(filePath, contents);
            Events.OnLogSaved();
        }

        /// <summary> Gathers common log data. </summary>
        private static List<string> LogContents()
        {
            return new List<string>()
            {
                $"Time of completion: {DateTime.Now.ToString()}",
                $"N: {_n}",
                $"Mode: {_mode}",
                $"Pre-calculation ET: {BitBase.Elapsed}",
                $"Search ET: {Seeker.Elapsed}",
                $"Serialization ET: {Serializer.Elapsed}"
            };
        }

        /// <summary> Gathers log data that is relevant to the Completion mode only. </summary>
        private static IEnumerable<string> LogSetupInfo()
        {
            return new[]
            {
                $"Preset size: {PresetMaker.Size}",
                $"Preset: {Serializer.Preset}",
                $"Preset generation ET: {PresetMaker.Elapsed}"
            };
        }

        #endregion
    }
}