using System;
using System.Linq;
using Cairo;
using Gtk;
using Gdk;

namespace GCross
{
    public class PuzzleDrawingArea : DrawingArea
    {
        private const int CellSize = 30;
        private const int PuzzleMargin = 40;
        private double _drawScale;
        private (double X, double Y) _translation;
        private (int X, int Y) _puzzleStart;
        private int _cluesWidth;
        private int _cluesHeight;
        private (int X, int Y) _selectedIndex;
        
        private const double BorderLineWidth = 4;
        private const double MajorGridLineWidth = 3;
        private const double MinorGridLineWidth = 1;

        private bool _buttonJustPressed;
        private bool _buttonPressed;
        private (int X, int Y) _mouse;
        private (int X, int Y) _lastMouse;
        private bool _mouseEntered;
        private Puzzle _puzzle;

        private CellStatus _clickAction;

        /// <summary>
        /// Create a drawing area to display a puzzle.
        /// </summary>
        /// <param name="puzzle"></param>
        public PuzzleDrawingArea(Puzzle puzzle = null)
        {
            SetSizeRequest(300, 300);
            
            Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask |
                      EventMask.PointerMotionMask | EventMask.PointerMotionHintMask | 
                      EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask;
            ButtonPressEvent += OnButtonPress;
            ButtonReleaseEvent += OnButtonRelease;
            MotionNotifyEvent += OnMotion;
            EnterNotifyEvent += OnMouseEnter;
            LeaveNotifyEvent += OnMouseLeave;

            _puzzle = puzzle;
        }

#region Event methods
        /// <summary>
        /// Called whenever a mouse button is pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnButtonPress(object sender, ButtonPressEventArgs args)
        {
            switch (args.Event.Button)
            {
                case 1:
                    _clickAction = CellStatus.Fill;
                    break;
                case 3:
                    _clickAction = CellStatus.Cross;
                    break;
                default:
                    return;
            }
            _buttonJustPressed = true;
            _mouse.X = (int) args.Event.X;
            _mouse.Y = (int) args.Event.Y;
            QueueDraw();
        }

        /// <summary>
        /// Called whenever a mouse button is released.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnButtonRelease(object sender, ButtonReleaseEventArgs args)
        {
            switch (args.Event.Button)
            {
                case 1:
                    if (_clickAction != CellStatus.Fill && _clickAction != CellStatus.Empty) return;
                    break;
                case 3:
                    if (_clickAction != CellStatus.Cross && _clickAction != CellStatus.Empty) return;
                    break;
                default:
                    return;
            }
            _buttonJustPressed = false;
            _buttonPressed = false;
            _mouse.X = (int) args.Event.X;
            _mouse.Y = (int) args.Event.Y;
            QueueDraw();
        }

        /// <summary>
        /// Called whenever the mouse pointer is moved in this drawing area.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnMotion(object sender, MotionNotifyEventArgs args)
        {
            _mouse.X = (int) args.Event.X;
            _mouse.Y = (int) args.Event.Y;
            QueueDraw();
        }

        /// <summary>
        /// Called whenever the mouse pointer enters this drawing area.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnMouseEnter(object sender, EnterNotifyEventArgs args)
        {
            _mouseEntered = true;
        }

        /// <summary>
        /// Called whenever the mouse pointer leaves this drawing area.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnMouseLeave(object sender, LeaveNotifyEventArgs args)
        {
            _mouseEntered = false;
            QueueDraw();
        }
        
        /// <summary>
        /// Called whenever this drawing area is updated on the screen. Use QueueDraw() to force an update.
        /// </summary>
        /// <param name="cr"></param>
        protected override bool OnDrawn(Context cr)
        {
            DrawPuzzle(cr);
            return base.OnDrawn(cr);
        }
#endregion

#region Draw methods
        /// <summary>
        /// Draw the entire puzzle to the drawing area.
        /// </summary>
        /// <param name="cr">Cairo context</param>
        private void DrawPuzzle(Context cr)
        {
            if (_puzzle == null) return;
            
            cr.Antialias = Antialias.Subpixel;
            cr.LineCap = LineCap.Square;

            _selectedIndex = GetHoveredCell();
            HandleClickedCells();
            
            ScaleToFit(cr);
            DrawBackground(cr);
            DrawHighlight(cr);
            DrawClues(cr);
            DrawCells(cr);
            DrawGrid(cr);
            //DrawHighlightBorder(cr);
        }

        /// <summary>
        /// Scale the puzzle drawing to fit the current size of the drawing area.
        /// </summary>
        /// <param name="cr">Cairo context</param>
        private void ScaleToFit(Context cr)
        {
            // Determine the size of the puzzle, including clues
            _cluesHeight = _puzzle.ColumnClues.Select(n => n.Length).Max() * CellSize;
            _cluesWidth = _puzzle.RowClues.Select(n => n.Length).Max() * CellSize;

            int drawWidth = _puzzle.Width * CellSize + PuzzleMargin * 2 + _cluesWidth;
            int drawHeight = _puzzle.Height * CellSize + PuzzleMargin * 2 + _cluesHeight;
            
            // Scale and transform the puzzle as needed
            double sx = (double) AllocatedWidth / drawWidth;
            double sy = (double) AllocatedHeight / drawHeight;
            
            if (sx < sy)
            {
                _drawScale = sx;
                _translation.Y = (AllocatedHeight / sx - drawHeight) / 2.0; // Center vertically
                _translation.X = 0;
            }
            else
            {
                _drawScale = sy;
                _translation.X = (AllocatedWidth / sy - drawWidth) / 2.0; // Center horizontally
                _translation.Y = 0;
            }
            cr.Scale(_drawScale, _drawScale);
            cr.Translate(_translation.X, _translation.Y);

            _puzzleStart.X = PuzzleMargin + _cluesWidth;
            _puzzleStart.Y = PuzzleMargin + _cluesHeight;
        }

        /// <summary>
        /// Fill the puzzle background in the drawing area.
        /// </summary>
        /// <param name="cr">Cairo context</param>
        private void DrawBackground(Context cr)
        {
            cr.SetSourceRGBA(1, 1, 1, 1);
            cr.Rectangle(_puzzleStart.X, PuzzleMargin, _puzzle.Width * CellSize,
                _puzzle.Height * CellSize + _cluesHeight);
            cr.Rectangle(PuzzleMargin, _puzzleStart.Y, _puzzle.Width * CellSize + _cluesWidth, 
                _puzzle.Height * CellSize);
            cr.Fill();
        }

        /// <summary>
        /// Draw a highlight behind the row and column of the currently selected cell.
        /// </summary>
        /// <param name="cr">Cairo context</param>
        private void DrawHighlight(Context cr)
        {
            cr.SetSourceRGBA(0, 0.83, 0.98, .2);
            if (_selectedIndex.X >= 0 && _selectedIndex.X < _puzzle.Width)
            {
                int x = _selectedIndex.X * CellSize + _puzzleStart.X;
                cr.Rectangle(x, PuzzleMargin, CellSize, _puzzle.Height * CellSize + _cluesHeight);
            }
            
            if (_selectedIndex.Y >= 0 && _selectedIndex.Y < _puzzle.Height)
            {
                int y = _selectedIndex.Y * CellSize + _puzzleStart.Y;
                cr.Rectangle(PuzzleMargin, y, _puzzle.Width * CellSize + _cluesWidth, CellSize);
            }
            
            cr.Fill();
        }

        /// <summary>
        /// Draw a border around the highlight above the cells.
        /// </summary>
        /// <param name="cr">Cairo context</param>
        private void DrawHighlightBorder(Context cr)
        {
            cr.SetSourceRGBA(0, 0.83, 0.98, 1);
            cr.LineWidth = BorderLineWidth;
            if (_selectedIndex.X >= 0 && _selectedIndex.X < _puzzle.Width)
            {
                int x = _selectedIndex.X * CellSize + _puzzleStart.X;
                cr.Rectangle(x, _puzzleStart.Y, CellSize, _puzzle.Height * CellSize);
            }
            
            if (_selectedIndex.Y >= 0 && _selectedIndex.Y < _puzzle.Height)
            {
                int y = _selectedIndex.Y * CellSize + _puzzleStart.Y;
                cr.Rectangle(_puzzleStart.X, y, _puzzle.Width * CellSize, CellSize);
            }

            cr.Stroke();
        }

        /// <summary>
        /// Draw the row and column clues of the puzzle to the drawing area.
        /// </summary>
        /// <param name="cr">Cairo context</param>
        private void DrawClues(Context cr)
        {
            cr.SetSourceRGBA(0, 0, 0, 1);
            cr.SetFontSize(20);
            
            // Column clues
            for (int column = 0; column < _puzzle.Width; column++)
            {
                int[] clues = _puzzle.ColumnClues[column];
                int x = _puzzleStart.X + column * CellSize;
                for (int i = 0; i < clues.Length; i++)
                {
                    string clue = clues[clues.Length - i - 1].ToString();
                    TextExtents te = cr.TextExtents(clue);
                    cr.MoveTo(x + (CellSize - te.Width) / 2.0 - te.XBearing,
                        _puzzleStart.Y - (CellSize * i) - (CellSize - te.Height) / 2.0);
                    cr.ShowText(clue);
                }
            }
            
            // Row clues
            for (int row = 0; row < _puzzle.Height; row++)
            {
                int[] clues = _puzzle.RowClues[row];
                int y = _puzzleStart.Y + (row + 1) * CellSize;
                for (int i = 0; i < clues.Length; i++)
                {
                    string clue = clues[clues.Length - i - 1].ToString();
                    TextExtents te = cr.TextExtents(clue);
                    cr.MoveTo(_puzzleStart.X - (CellSize * (i + 1)) + (CellSize - te.Width) / 2.0 - te.XBearing,
                        y - (CellSize - te.Height) / 2.0);
                    cr.ShowText(clue);
                }
            }
        }

        /// <summary>
        /// Draw the borders and grid of the puzzle and clues to the drawing area.
        /// </summary>
        /// <param name="cr">Cairo context</param>
        private void DrawGrid(Context cr)
        {
            cr.SetSourceRGBA(0.35, 0.35, 0.35, 1);
            
            // Puzzle border
            cr.LineWidth = BorderLineWidth;
            
            cr.MoveTo(_puzzleStart.X, PuzzleMargin);
            cr.LineTo(_puzzleStart.X, _puzzleStart.Y + _puzzle.Height * CellSize);
            
            cr.MoveTo(_puzzleStart.X + _puzzle.Width * CellSize, PuzzleMargin);
            cr.LineTo(_puzzleStart.X + _puzzle.Width * CellSize, _puzzleStart.Y + _puzzle.Height * CellSize);

            cr.MoveTo(PuzzleMargin, _puzzleStart.Y);
            cr.LineTo(_puzzleStart.X + _puzzle.Width * CellSize, _puzzleStart.Y);

            cr.MoveTo(PuzzleMargin, _puzzleStart.Y + _puzzle.Height * CellSize);
            cr.LineTo(_puzzleStart.X + _puzzle.Width * CellSize, _puzzleStart.Y + _puzzle.Height * CellSize);
            
            cr.Stroke();
            
            // Major grid (every five cells)
            cr.LineWidth = MajorGridLineWidth;
            for (int x = 1; x <= _puzzle.Width / 5; x++)
            {
                cr.MoveTo(x * 5 * CellSize + _puzzleStart.X, PuzzleMargin);
                cr.LineTo(x * 5 * CellSize + _puzzleStart.X, _puzzle.Height * CellSize + _puzzleStart.Y);
            }
            for (int y = 1; y <= _puzzle.Height / 5; y++)
            {
                cr.MoveTo(PuzzleMargin, y * 5 * CellSize + _puzzleStart.Y);
                cr.LineTo(_puzzle.Width * CellSize + _puzzleStart.X, y * 5 * CellSize + _puzzleStart.Y);
            }
            
            cr.Stroke();
                
            // Minor grid (every cell)
            cr.LineWidth = MinorGridLineWidth;
            for (int x = 1; x < _puzzle.Width; x++)
            {
                cr.MoveTo(x * CellSize + _puzzleStart.X, PuzzleMargin);
                cr.LineTo(x * CellSize + _puzzleStart.X, _puzzle.Height * CellSize + _puzzleStart.Y);
            }
            for (int y = 1; y < _puzzle.Height; y++)
            {
                cr.MoveTo(PuzzleMargin, y * CellSize + _puzzleStart.Y);
                cr.LineTo(_puzzle.Width * CellSize + _puzzleStart.X, y * CellSize + _puzzleStart.Y);
            }
            
            cr.Stroke();
        }

        /// <summary>
        /// Draw each cell with its current status to the grid in the drawing area.
        /// </summary>
        /// <param name="cr">Cairo context</param>
        private void DrawCells(Context cr)
        {
            cr.SetSourceRGBA(0, 0, 0, 1);
            cr.LineWidth = 2;
            
            for (int x = 0; x < _puzzle.Width; x++)
            {
                for (int y = 0; y < _puzzle.Height; y++)
                {
                    int cellX = _puzzleStart.X + x * CellSize;
                    int cellY = _puzzleStart.Y + y * CellSize;
                    
                    if (_puzzle.Grid[x, y] == CellStatus.Fill)
                    {
                        cr.Rectangle(cellX, cellY, CellSize, CellSize);
                        cr.Fill();
                    }
                    else if (_puzzle.Grid[x, y] == CellStatus.Cross)
                    {
                        // Clip the cross in this square so it doesn't extend to other cells
                        cr.Rectangle(cellX, cellY, CellSize, CellSize);
                        cr.Clip();

                        cr.MoveTo(cellX, cellY);
                        cr.LineTo(cellX + CellSize, cellY + CellSize);
                        cr.MoveTo(cellX + CellSize, cellY);
                        cr.LineTo(cellX, cellY + CellSize);
                        
                        cr.Stroke();
                        cr.ResetClip();
                    }
                }
            }
        }
#endregion

#region Mouse handling methods
        /// <summary>
        /// Get the index of the cell currently being hovered by the mouse. A negative value means a cell clue is being
        /// hovered. If no cells or clues are hovered, (-1, -1) is returned.
        /// </summary>
        /// <returns>x and y indices of the cell</returns>
        private (int, int) GetHoveredCell()
        {
            double x = _mouse.X - _translation.X * _drawScale - _puzzleStart.X * _drawScale;
            double y = _mouse.Y - _translation.Y * _drawScale - _puzzleStart.Y * _drawScale;

            if (x < 0 && y < 0)
                return (-1, -1);

            x = x / _drawScale / CellSize;
            y = y / _drawScale / CellSize;

            if (x >= _puzzle.Width || y >= _puzzle.Height ||
                x < (double) -_cluesWidth / CellSize || y < (double) -_cluesHeight / CellSize) 
                return (-1, -1);
                    
            return ((int, int)) (Math.Floor(x), Math.Floor(y));
        }
        
        /// <summary>
        /// Update the player's puzzle grid based on mouse actions.
        /// </summary>
        private void HandleClickedCells()
        {
            if (!_buttonPressed && !_buttonJustPressed) return;
            
            CellStatus cellStatus = CellStatus.None;
            if (_selectedIndex.X >= 0 && _selectedIndex.Y >= 0)
                cellStatus = _puzzle.Grid[_selectedIndex.X, _selectedIndex.Y];
            
            if (_buttonJustPressed)
            {
                _buttonJustPressed = false;
                
                // Make sure the player clicked inside the puzzle
                if (_selectedIndex != (-1, -1))
                {
                    if (_selectedIndex.X < 0)
                    {
                        // TODO: row clue clicked
                    }
                    else if (_selectedIndex.Y < 0)
                    {
                        // TODO: column clue clicked
                    }
                    else
                    {
                        // Get the status of the clicked cell to see if the action needs to be updated
                        if (cellStatus == _clickAction)
                            _clickAction = CellStatus.Empty;
                        _buttonPressed = true;
                    }
                }
            }

            if (_selectedIndex.X < 0 || _selectedIndex.Y < 0) return;
            
            if (_buttonPressed)
            {
                if (cellStatus != _clickAction)
                {
                    _puzzle.Grid[_selectedIndex.X, _selectedIndex.Y] = _clickAction;
                }
            }
        }
#endregion

    }
}