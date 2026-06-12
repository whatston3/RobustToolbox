using System.Globalization;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.ViewVariables.Editors
{
    internal sealed class VVPropEditorBoolean : VVPropEditor
    {
        protected override Control MakeUI(object? value)
        {
            if (Nullable)
            {
                var hBox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    MinSize = new Vector2(200, 0)
                };
                var boolValue = (bool?)value;
                var lineEdit = new LineEdit
                {
                    Text = boolValue == null ? NullString : boolValue.Value.ToString(CultureInfo.InvariantCulture),
                    Editable = !ReadOnly,
                    HorizontalExpand = true
                };

                if (!ReadOnly)
                {
                    lineEdit.OnTextEntered += e =>
                    {
                        if (IsNullString(e.Text))
                        {
                            ValueChanged(null);
                            return;
                        }

                        if (!bool.TryParse(e.Text, out var value))
                            return;

                        ValueChanged(value);
                    };
                }

                hBox.AddChild(lineEdit);
                return hBox;
            }
            else
            {
                var box = new CheckBox
                {
                    Pressed = (bool)value!,
                    Disabled = ReadOnly,
                    Text = value!.ToString()!,
                    MinSize = new Vector2(70, 0)
                };
                if (!ReadOnly)
                {
                    box.OnToggled += args => ValueChanged(args.Pressed);
                }
                return box;
            }
        }
    }
}
