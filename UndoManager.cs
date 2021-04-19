using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMLEditor
{
    class UndoManager
    {
        internal enum OperationType
        {
            None,
            PanOrResize,
            Add,
            Remove
        }

        internal class DoOperation
        {
            public KMLElement Before { get; set; }
            public KMLElement After { get; set; }
            public OperationType OperationType { get; set; } = OperationType.None;
            public KMLElementWithOffset.SelectionPart ModifiedPart { get; set; } = KMLElementWithOffset.SelectionPart.None;
        }
        List<List<DoOperation>> listOfUndoRedo = new List<List<DoOperation>>();
        int listOfUndoRedoPosition = -1;

        internal bool CanUndo()
        {
            return listOfUndoRedoPosition > -1 && listOfUndoRedoPosition < listOfUndoRedo.Count;
        }

        internal List<DoOperation> Undo()
        {
            if(CanUndo())
                return listOfUndoRedo[listOfUndoRedoPosition--];
            return null;
        }

        internal bool CanRedo()
        {
            return listOfUndoRedoPosition >= -1 && listOfUndoRedoPosition + 1 < listOfUndoRedo.Count;
        }

        internal List<DoOperation> Redo()
        {
            if (CanRedo())
                return listOfUndoRedo[++listOfUndoRedoPosition];
            return null;
        }

        internal void Modify(List<DoOperation> operations)
        {
            if(listOfUndoRedoPosition >= -1 && listOfUndoRedo.Count > listOfUndoRedoPosition + 1)
                listOfUndoRedo.RemoveRange(listOfUndoRedoPosition + 1, listOfUndoRedo.Count - listOfUndoRedoPosition - 1);
            listOfUndoRedo.Add(operations);
            listOfUndoRedoPosition = listOfUndoRedo.Count - 1;
        }
    }
}
