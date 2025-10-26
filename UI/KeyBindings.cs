namespace Gitree.UI;

public enum UiAction
{
    MoveUp,
    MoveDown,
    Quit,
    Interrupt,
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
            ConsoleKey.Q when key.Modifiers == ConsoleModifiers.Shift => UiAction.Quit,
            ConsoleKey.C when key.Modifiers.HasFlag(ConsoleModifiers.Control) => UiAction.Interrupt,
            ConsoleKey.Escape => UiAction.NoOp,
            _ => UiAction.NoOp,
        };
    }
}
