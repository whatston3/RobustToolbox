using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Log;

namespace Robust.Client.ViewVariables.Editors
{
    /// <summary>
    ///     Just writes out the ToString of the object.
    ///     The ultimate fallback.
    /// </summary>
    internal sealed class VVPropEditorDummy : VVPropEditor
    {
        private LazySawmill _lazyLog = new("vv");

        protected override Control MakeUI(object? value)
        {
            if (!ReadOnly)
            {
                _lazyLog.Sawmill.Warning("ViewVariablesPropertyEditorDummy being selected for editable field.");
            }
            return new Label
            {
                Text = value == null ? "null" : value.ToString() ?? "<null ToString()>",
                Align = Label.AlignMode.Right,
                HorizontalExpand = true
            };
        }
    }
}
