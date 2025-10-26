namespace Gitree.UI;

public enum UiAction
{
    MoveUp,
    MoveDown,
    Quit,
    Interrupt,
    ToggleSelection,
    SelectAllUnder,
    ClearAllUnder,
    ExpandOrEnter,
    CollapseOrUp,
    ToggleFilesOnly,
    NoOp
}

public static class KeyBindings
{
    public static UiAction Map(ConsoleKeyInfo key)
    {
        return key.Key switch
        {
            ConsoleKey.UpArrow => UiAction.MoveUp,
            ConsoleKey.DownArrow => UiAction.MoveDown,
            ConsoleKey.Q when key.Modifiers == 0 => UiAction.Quit,
            ConsoleKey.Q when key.Modifiers.HasFlag(ConsoleModifiers.Shift) => UiAction.Quit,
            ConsoleKey.C when key.Modifiers.HasFlag(ConsoleModifiers.Control) => UiAction.Interrupt,
            ConsoleKey.Spacebar => UiAction.ToggleSelection,
            ConsoleKey.A when key.Modifiers.HasFlag(ConsoleModifiers.Shift) => UiAction.ClearAllUnder,
            ConsoleKey.A when key.Modifiers == 0 => UiAction.SelectAllUnder,
            ConsoleKey.Enter => UiAction.ExpandOrEnter,
            ConsoleKey.RightArrow => UiAction.ExpandOrEnter,
            ConsoleKey.LeftArrow => UiAction.CollapseOrUp,
            ConsoleKey.S when key.Modifiers == 0 => UiAction.ToggleFilesOnly,
            ConsoleKey.S when key.Modifiers == ConsoleModifiers.Shift => UiAction.ToggleFilesOnly,
            ConsoleKey.Escape => UiAction.NoOp,
            _ => UiAction.NoOp,
        };
    }
}
