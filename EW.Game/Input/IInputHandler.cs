﻿using System;
namespace EW
{
	[Flags]
	public enum MouseButton
	{
		None = 0,
		Left = 1,
		Right = 2,
		Middle = 4,
	}

	[Flags]
	public enum Modifiers
	{
		None = 0,
		Shift = 1,
		Alt = 2,
		Ctrl = 4,
		Meta = 8,
	}

	public enum KeyInputEvent { Down, Up }
	public enum MouseInputEvent { Down, Move, Up, Scroll }

	public struct KeyInput
	{
		public KeyInputEvent Event;

		public Modifiers Modifiers;
		public int MultiTapCount;
		public char UnicodeChar;
	}

	public struct MouseInput
	{
		public MouseInputEvent Event;
		public MouseButton Button;
		public int ScrollDelta;
		public Modifiers Modifiers;
		public int MultiTapCount;


	}
	public interface IInputHandler
	{
		void ModifierKeys(Modifiers mods);
		void OnKeyInput();


	}
}
