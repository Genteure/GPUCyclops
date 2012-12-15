using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Media {
	public struct Trace {
		public int callerClassNameIndex;
		public int calleeClassDotMethodIndex;
		public int messageIndex;
		public int thread;
	}

	public class FastTrace {
		static int BUFSIZE = 1024;
		static int currBufIndex = 0;
		static Trace[] TraceBuf = new Trace[BUFSIZE];
		static List<string> ClassNames = new List<string>();
		static List<string> MethodNames = new List<string>();
		static List<string> Messages = new List<string>();

		static int FindIndex(List<string> list, string str) {
			for (int i = 0; i < list.Count; i++) {
				if (list[i].Equals(str))
					return i;
			}
			throw new Exception("Contains says it's there, but it isn't!");
		}

		public static void AddTrace(string className, string calleeDetails, string message) {
			try {
				Trace trace = new Trace();
				trace.thread = Thread.CurrentThread.ManagedThreadId;
				if (!ClassNames.Contains(className))
					ClassNames.Add(className);
				trace.callerClassNameIndex = FindIndex(ClassNames, className);
				if (!MethodNames.Contains(calleeDetails))
					MethodNames.Add(calleeDetails);
				trace.calleeClassDotMethodIndex = FindIndex(MethodNames, calleeDetails);
				if (!Messages.Contains(message))
					Messages.Add(message);
				trace.messageIndex = FindIndex(Messages, message);
				TraceBuf[currBufIndex] = trace;
				currBufIndex = (currBufIndex + 1) % BUFSIZE;
			} catch (Exception) {
				PrintTrace();
				Debug.Assert(false);
			}
		}

		static int maxClassNameLen;
		static int maxMethodNameLen;
		static int maxMessageLen;
		public static string GetOneTrace(int index) {
			int filler;
			Trace trace = TraceBuf[index];
			StringBuilder sb = new StringBuilder();

			string str = trace.thread.ToString();
			sb.AppendFormat(@"T: {0}, ", str);

			str = ClassNames[trace.callerClassNameIndex];
			sb.AppendFormat(@"C: {0}, ", str);
			filler = maxClassNameLen - str.Length;
			sb.Append(' ', filler);

			str = MethodNames[trace.calleeClassDotMethodIndex];
			sb.AppendFormat(@"M: {0}, ", str);
			filler = maxMethodNameLen - str.Length;
			sb.Append(' ', filler);

			str = Messages[trace.messageIndex];
			sb.Append(str);

			return sb.ToString();
		}

		static int MaxStringLen(List<string> list) {
			// can't use Linq, so we look for Max ourselves here
			int curMax = 0;
			foreach (string s in list)
				if (s.Length > curMax)
					curMax = s.Length;
			return curMax;
		}

		public static void PrintTrace() {
			maxClassNameLen = MaxStringLen(ClassNames);
			maxMethodNameLen = MaxStringLen(MethodNames);
			maxMessageLen = MaxStringLen(Messages);
			for (int i = 0; i < TraceBuf.Length; i++) {
				Debug.WriteLine(GetOneTrace(i));
			}
		}
	}
}
