﻿using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMovieSession
	{
		IMovie Movie { get; set; }
		IMovie QueuedMovie { get; }
		IMovieController MovieController { get; }
		MultitrackRecorder MultiTrack { get; }

		IController PreviousFrame { get; }
		IController CurrentInput { get; }

		void CreateMovieController(ControllerDefinition definition);

		IMovieController GenerateMovieController(ControllerDefinition definition);

		bool ReadOnly { get; set; }
		bool MovieIsQueued { get; }

		// TODO: this isn't sustainable
		bool? PreviousNesInQuickNES { get; set; }
		bool? PreviousSnesInSnes9x { get; set; }
		bool? PreviousGbaUsemGba { get; set; }
		bool? PreviousGbUseGbHawk { get; set; }

		void HandleMovieOnFrameLoop();
		void HandleMovieAfterFrameLoop();
		void HandleMovieSaveState(TextWriter writer);

		bool CheckSavestateTimeline(TextReader reader);
		bool HandleMovieLoadState(TextReader reader);

		ILogEntryGenerator LogGeneratorInstance(IController source);

		void QueueNewMovie(IMovie movie, bool record, IEmulator emulator);
		void RunQueuedMovie(bool recordMode);

		void ToggleMultitrack();

		void StopMovie(bool saveChanges = true);
	}
}
