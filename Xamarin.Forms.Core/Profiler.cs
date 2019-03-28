using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms.Internal
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public struct Profile : IDisposable
	{
		const int Capacity = 1000;

		public struct Datum
		{
			public string Name;
			public string Id;
			public long Ticks;
			public int Depth;
			public int Line;
		}
		public static List<Datum> Data = new List<Datum>(Capacity);

		static Stack<Profile> s_stack = new Stack<Profile>(Capacity);
		static int s_depth = 0;
		static bool s_stopped = false;
		static Stopwatch s_stopwatch = new Stopwatch();

		readonly long start;
		readonly string name;
		readonly int slot;

		public static void Stop()
		{
			// unwind stack
			s_stopped = true;
			while (s_stack.Count > 0)
				s_stack.Pop();
		}

		public static void Push(
			[CallerMemberName] string name = "",
			string id = null,
			[CallerLineNumber] int line = 0)
		{
			if (s_stopped)
				return;

			if (!s_stopwatch.IsRunning)
				s_stopwatch.Start();

			s_stack.Push(new Profile(name, id, line));
		}

		public static void Pop()
		{
			if (s_stopped)
				return;

			var profile = s_stack.Pop();
			profile.Dispose();
		}

		public static void PopPush(
			string id,
			[CallerLineNumber] int line = 0)
		{
			if (s_stopped)
				return;

			var profile = s_stack.Pop();
			var name = profile.name;
			profile.Dispose();

			Push(name, id, line);
		}

		private Profile(
			string name,
			string id = null,
			int line = 0)
		{
			this = default(Profile);
			this.start = s_stopwatch.ElapsedTicks;

			this.name = name;

			this.slot = Data.Count;
			Data.Add(new Datum()
			{
				Depth = s_depth,
				Name = name,
				Id = id,
				Ticks = -1,
				Line = line
			});

			s_depth++;
		}
		public void Dispose()
		{
			if (s_stopped && this.start == 0)
				return;

			var ticks = s_stopwatch.ElapsedTicks - this.start;
			--s_depth;

			var datum = Data[slot];
			datum.Ticks = ticks;
			Data[this.slot] = datum;
		}
	}
}