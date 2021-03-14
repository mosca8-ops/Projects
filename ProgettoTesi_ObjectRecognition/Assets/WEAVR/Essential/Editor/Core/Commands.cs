using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Core
{
    public enum Command
    {
        None,
        Duplicate,
        Delete,
        ForceDelete,
        Reset,
        Cancel,
        Group,
        Mirror,

        Undo,
        Redo,

        Copy,
        Cut,
        Paste
    }

    public class Commands
    {
        private Dictionary<KeyValuePair<KeyCode, EventModifiers>, Command> _commands;
        private Dictionary<Command, string> _shortcuts;

        public Commands() {
            _commands = new Dictionary<KeyValuePair<KeyCode, EventModifiers>, Command>();
            _shortcuts = new Dictionary<Command, string>();

            // Default Commands
            this[EventModifiers.Control, KeyCode.D] = Command.Duplicate;
            this[EventModifiers.Control, KeyCode.R] = Command.Reset;
            this[EventModifiers.None, KeyCode.Delete] = Command.Delete;
            this[EventModifiers.Shift, KeyCode.Delete] = Command.ForceDelete;
            this[EventModifiers.None, KeyCode.Escape] = Command.Cancel;
            this[EventModifiers.Control, KeyCode.G] = Command.Group;
            this[EventModifiers.Control, KeyCode.M] = Command.Mirror;

            this[EventModifiers.Control, KeyCode.Z] = Command.Undo;
            this[EventModifiers.Control, KeyCode.Y] = Command.Redo;

            this[EventModifiers.Control, KeyCode.C] = Command.Copy;
            this[EventModifiers.Control, KeyCode.X] = Command.Cut;
            this[EventModifiers.Control, KeyCode.V] = Command.Paste;
        }

        public Command this[EventModifiers modifiers, KeyCode code] {
            get {
                return GetCommand(modifiers, code);
            }
            private set {
                _commands.Add(new KeyValuePair<KeyCode, EventModifiers>(code, modifiers), value);
                if(modifiers == EventModifiers.Control) {
                    _commands.Add(new KeyValuePair<KeyCode, EventModifiers>(code, EventModifiers.Command), value);
                }
                else if(modifiers == EventModifiers.None) {
                    _commands.Add(new KeyValuePair<KeyCode, EventModifiers>(code, EventModifiers.FunctionKey), value);
                }
                // Set shortcuts description
                string shortcutDesr = " ";
                if((modifiers & EventModifiers.Control) == EventModifiers.Control 
                    || (modifiers & EventModifiers.Command) == EventModifiers.Command) {
                    shortcutDesr += "%";
                }
                if((modifiers & EventModifiers.Shift) == EventModifiers.Shift) {
                    shortcutDesr += "#";
                }
                if ((modifiers & EventModifiers.Alt) == EventModifiers.Alt) {
                    shortcutDesr += "&";
                }
                if(modifiers == EventModifiers.None || modifiers == EventModifiers.FunctionKey) {
                    shortcutDesr = " _";
                }

                _shortcuts[value] = shortcutDesr + code.ToString();
            }
        }

        public Command GetCommand(EventModifiers modifiers, KeyCode code) {
            Command returnCommand = Command.None;
            // Remove function key from modifiers
            modifiers &= ~EventModifiers.FunctionKey;
            if (_commands.TryGetValue(new KeyValuePair<KeyCode, EventModifiers>(code, modifiers), out returnCommand)) {
                return returnCommand;
            }
            return returnCommand;
        }

        public string GetShortcutDescription(Command command) {
            if(command != Command.None) {
                return _shortcuts[command];
            }
            return "";
        }
    }
}