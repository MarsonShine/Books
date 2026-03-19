namespace chapter_2
{
    public class UndoRedo<T>
    {
        private IImStack<T> undoStack = ImStack<T>.Empty;
        private IImStack<T> redoStack = ImStack<T>.Empty;
        public T? State { get; private set; }

        public UndoRedo(T initial)
        {
            State = initial;
        }

        public void Do(T newState)
        {
            undoStack = undoStack.Push(newState);
            State = newState;
            redoStack = ImStack<T>.Empty;
        }
        public bool CanUndo => !undoStack.IsEmpty;
        public void Undo()
        {
            if (!CanUndo)
                throw new InvalidOperationException("No more undos.");
            redoStack = redoStack.Push(State!);
            State = undoStack.Peek();
            undoStack = undoStack.Pop();
        }
        public bool CanRedo => !redoStack.IsEmpty;
        public T Redo()
        {
            if (!CanRedo)
                throw new InvalidOperationException("No more redos.");
            undoStack = undoStack.Push(State!);
            State = redoStack.Peek();
            redoStack = redoStack.Pop();
            return State;
        }
    }
}
