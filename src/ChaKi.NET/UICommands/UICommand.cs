using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;

namespace ChaKi.UICommands
{
    public class UICommand
    {
        private enum FieldNameTemplate
        {
            UIC__command__ToolStripButton,
            UIC__command__ToolStripMenuItem,
            UIC__command__PopupMenuItem,
        }

        private enum EventHandlerTemplate
        {
            On__command__,
            On__command__UpdateUI,
        }

        private string GetReplaceParameter(string template)
        {
            int startIndex = template.IndexOf("__");
            int endIndex = template.LastIndexOf("__");
            if ((startIndex == -1) || (endIndex == -1) || (startIndex == endIndex))
            {
                throw new InvalidOperationException("Invalid command template.");
            }
            return template.Substring(startIndex, endIndex - startIndex + 2);
        }

        private string GetFieldName(FieldNameTemplate template)
        {
            return template.ToString().Replace(GetReplaceParameter(template.ToString()), Name);
        }

        private string GetHandlerName(EventHandlerTemplate template)
        {
            return template.ToString().Replace(GetReplaceParameter(template.ToString()), Name);
        }

        private ToolStripButton FindButton(string buttonName)
        {
            FieldInfo buttonInfo = Target.GetType().GetField(
                buttonName,
                BindingFlags.IgnoreCase
                | BindingFlags.Instance
                | BindingFlags.NonPublic
                | BindingFlags.Public);
            if (buttonInfo != null)
            {
                return buttonInfo.GetValue(Target) as ToolStripButton;
            }
            return null;
        }

        private ToolStripMenuItem FindMenuItem(string menuName)
        {
            FieldInfo menuInfo = Target.GetType().GetField(
                menuName,
                BindingFlags.IgnoreCase
                | BindingFlags.Instance
                | BindingFlags.NonPublic
                | BindingFlags.Public);
            if (menuInfo != null)
            {
                return menuInfo.GetValue(Target) as ToolStripMenuItem;
            }
            return null;
        }

        private void CreateEventHandler(
            object target,
            object component,
            string eventName,
            string handlerName,
            bool add)
        {
            MethodInfo methodInfo = target.GetType().GetMethod(
                handlerName,
                BindingFlags.IgnoreCase
                | BindingFlags.Instance
                | BindingFlags.NonPublic
                | BindingFlags.Public);
            if (methodInfo != null)
            {
                EventInfo eventInfo = component.GetType().GetEvent(
                    eventName,
                    BindingFlags.IgnoreCase
                    | BindingFlags.Instance
                    | BindingFlags.Public);
                Delegate eventDelegate = Delegate.CreateDelegate(
                    typeof(EventHandler), target, methodInfo);
                if (add)
                {
                    eventInfo.AddEventHandler(component, eventDelegate);
                }
                else
                {
                    eventInfo.RemoveEventHandler(component, eventDelegate);
                }
            }
        }

        public event EventHandler UpdateEventHandler;
        public void Update()
        {
            if (UpdateEventHandler != null)
            {
                UpdateEventHandler(this, EventArgs.Empty);
            }
        }

        private Control target;
        public Control Target
        {
            get { return target; }
            private set { target = value; }
        }

        private string name;
        public string Name
        {
            get { return name; }
            private set { name = value; }
        }

        private ToolStripButton button;
        public ToolStripButton Button
        {
            get { return button; }
            private set { button = value; }
        }

        private ToolStripMenuItem menuItem;
        public ToolStripMenuItem MenuItem
        {
            get { return menuItem; }
            private set { menuItem = value; }
        }

        private ToolStripMenuItem popupMenuItem;
        public ToolStripMenuItem PopupMenuItem
        {
            get { return popupMenuItem; }
            private set { popupMenuItem = value; }
        }

        private void SetEventHandlers(bool add)
        {
            if (Button == null)
            {
                Button = FindButton(GetFieldName(FieldNameTemplate.UIC__command__ToolStripButton));
            }
            if (Button != null)
            {
                CreateEventHandler(
                    Target,
                    Button,
                    "Click",
                    GetHandlerName(EventHandlerTemplate.On__command__),
                    add);
            }
            if (MenuItem == null)
            {
                MenuItem = FindMenuItem(GetFieldName(FieldNameTemplate.UIC__command__ToolStripMenuItem));
            }
            if (MenuItem != null)
            {
                CreateEventHandler(
                    Target,
                    MenuItem,
                    "Click",
                    GetHandlerName(EventHandlerTemplate.On__command__),
                    add);
            }
            if (PopupMenuItem == null)
            {
                PopupMenuItem = FindMenuItem(GetFieldName(FieldNameTemplate.UIC__command__PopupMenuItem));
            }
            if (PopupMenuItem != null)
            {
                CreateEventHandler(
                    Target,
                    PopupMenuItem,
                    "Click",
                    GetHandlerName(EventHandlerTemplate.On__command__),
                    add);
            }
            CreateEventHandler(
                Target,
                this,
                "UpdateEventHandler",
                GetHandlerName(EventHandlerTemplate.On__command__UpdateUI),
                add);
        }

        public UICommand(Control target, string name)
        {
            Target = target;
            Name = name;
            SetEventHandlers(true);
        }

        ~UICommand()
        {
            SetEventHandlers(false);
        }

        public void DefaultUpdateUI(bool enabled)
        {
            if (Button != null) Button.Enabled = enabled;
            if (MenuItem != null) MenuItem.Enabled = enabled;
            if (PopupMenuItem != null) PopupMenuItem.Enabled = enabled;
        }

        private static List<UICommand> commands = new List<UICommand>();
        public static List<UICommand> Commands
        {
            get { return commands; }
            set { commands = value; }
        }

        public static void UpdateCommands()
        {
            foreach (UICommand command in Commands)
            {
                command.Update();
            }
        }
    }
}
