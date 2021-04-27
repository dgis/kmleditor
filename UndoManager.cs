//    KMLEditor - A small KML files editor to edit the skins for the emulators like Emu48.
//    Copyright (C) 2021 Regis COSNIER
//
//    This program is free software; you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation; either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program; if not, write to the Free Software
//    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System.Collections.Generic;

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
