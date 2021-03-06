﻿using System;
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
        public Puzzle Puzzle;

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

            Puzzle = puzzle;
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
            if (Puzzle == null) return;
            
            cr.Antialias = Antialias.Subpixel;
            cr.LineCap = LineCap.Square;

            _selectedIndex = GetHoveredCell();
            HandleClickedCells();
            Puzzle.UpdateSolvedClues();

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
            _cluesHeight = Puzzle.ColumnClues.Select(n => n.Length).Max() * CellSize;
            _cluesWidth = Puzzle.RowClues.Select(n => n.Length).Max() * CellSize;

            int drawWidth = Puzzle.Width * CellSize + PuzzleMargin * 2 + _cluesWidth;
            int drawHeight = Puzzle.Height * CellSize + PuzzleMargin * 2 + _cluesHeight;
            
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
            cr.Rectangle(_puzzleStart.X, PuzzleMargin, Puzzle.Width * CellSize,
                Puzzle.Height * CellSize + _cluesHeight);
            cr.Rectangle(PuzzleMargin, _puzzleStart.Y, Puzzle.Width * CellSize + _cluesWidth, 
                Puzzle.Height * CellSize);
            cr.Fill();
        }

        /// <summary>
        /// Draw a highlight behind the row and column of the currently selected cell.
        /// </summary>
        /// <param name="cr">Cairo context</param>
        private void DrawHighlight(Context cr)
        {
            cr.SetSourceRGBA(0, 0.83, 0.98, .2);
            if (_selectedIndex.X >= 0 && _selectedIndex.X < Puzzle.Width)
            {
                int x = _selectedIndex.X * CellSize + _puzzleStart.X;
                cr.Rectangle(x, PuzzleMargin, CellSize, Puzzle.Height * CellSize + _cluesHeight);
            }
            
            if (_selectedIndex.Y >= 0 && _selectedIndex.Y < Puzzle.Height)
            {
                int y = _selectedIndex.Y * CellSize + _puzzleStart.Y;
                cr.Rectangle(PuzzleMargin, y, Puzzle.Width * CellSize + _cluesWidth, CellSize);
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
            if (_selectedIndex.X >= 0 && _selectedIndex.X < Puzzle.Width)
            {
                int x = _selectedIndex.X * CellSize + _puzzleStart.X;
                cr.Rectangle(x, _puzzleStart.Y, CellSize, Puzzle.Height * CellSize);
            }
            
            if (_selectedIndex.Y >= 0 && _selectedIndex.Y < Puzzle.Height)
            {
                int y = _selectedIndex.Y * CellSize + _puzzleStart.Y;
                cr.Rectangle(_puzzleStart.X, y, Puzzle.Width * CellSize, CellSize);
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
            for (int cx = 0; cx < Puzzle.Width; cx++)
            {
                int[] clues = Puzzle.ColumnClues[cx];
                ClueStatus[] cluesStatus = Puzzle.ColumnCluesStatus[cx];
                int x = _puzzleStart.X + cx * CellSize;
                for (int i = 0; i < clues.Length; i++)
                {
                    if (cluesStatus[clues.Length - i - 1] == ClueStatus.Unmarked)
                    {
                        cr.SelectFontFace("Arial", FontSlant.Normal, FontWeight.Bold);
                        cr.SetSourceRGBA(0, 0, 0, 1);
                    }
                    else
                    {
                        cr.SelectFontFace("Arial", FontSlant.Normal, FontWeight.Normal);
                        cr.SetSourceRGBA(0.5, 0.5, 0.5, 1);
                    }
                    
                    string clue = clues[clues.Length - i - 1].ToString();
                    TextExtents te = cr.TextExtents(clue);
                    cr.MoveTo(x + (CellSize - te.Width) / 2.0 - te.XBearing,
                        _puzzleStart.Y - (CellSize * i) - (CellSize - te.Height) / 2.0);

                    cr.ShowText(clue);
                }
            }
            
            // Row clues
            for (int cy = 0; cy < Puzzle.Height; cy++)
            {
                int[] clues = Puzzle.RowClues[cy];
                ClueStatus[] cluesStatus = Puzzle.RowCluesStatus[cy];
                int y = _puzzleStart.Y + (cy + 1) * CellSize;
                for (int i = 0; i < clues.Length; i++)
                {
                    if (cluesStatus[clues.Length - i - 1] == ClueStatus.Unmarked)
                    {
                        cr.SelectFontFace("Arial", FontSlant.Normal, FontWeight.Bold);
                        cr.SetSourceRGBA(0, 0, 0, 1);
                    }
                    else
                    {
                        cr.SelectFontFace("Arial", FontSlant.Normal, FontWeight.Normal);
                        cr.SetSourceRGBA(0.5, 0.5, 0.5, 1);
                    }
                    
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
            cr.LineTo(_puzzleStart.X, _puzzleStart.Y + Puzzle.Height * CellSize);
            
            cr.MoveTo(_puzzleStart.X + Puzzle.Width * CellSize, PuzzleMargin);
            cr.LineTo(_puzzleStart.X + Puzzle.Width * CellSize, _puzzleStart.Y + Puzzle.Height * CellSize);

            cr.MoveTo(PuzzleMargin, _puzzleStart.Y);
            cr.LineTo(_puzzleStart.X + Puzzle.Width * CellSize, _puzzleStart.Y);

            cr.MoveTo(PuzzleMargin, _puzzleStart.Y + Puzzle.Height * CellSize);
            cr.LineTo(_puzzleStart.X + Puzzle.Width * CellSize, _puzzleStart.Y + Puzzle.Height * CellSize);
            
            cr.Stroke();
            
            // Major grid (every five cells)
            cr.LineWidth = MajorGridLineWidth;
            for (int x = 1; x <= Puzzle.Width / 5; x++)
            {
                cr.MoveTo(x * 5 * CellSize + _puzzleStart.X, PuzzleMargin);
                cr.LineTo(x * 5 * CellSize + _puzzleStart.X, Puzzle.Height * CellSize + _puzzleStart.Y);
            }
            for (int y = 1; y <= Puzzle.Height / 5; y++)
            {
                cr.MoveTo(PuzzleMargin, y * 5 * CellSize + _puzzleStart.Y);
                cr.LineTo(Puzzle.Width * CellSize + _puzzleStart.X, y * 5 * CellSize + _puzzleStart.Y);
            }
            
            cr.Stroke();
                
            // Minor grid (every cell)
            cr.LineWidth = MinorGridLineWidth;
            for (int x = 1; x < Puzzle.Width; x++)
            {
                cr.MoveTo(x * CellSize + _puzzleStart.X, PuzzleMargin);
                cr.LineTo(x * CellSize + _puzzleStart.X, Puzzle.Height * CellSize + _puzzleStart.Y);
            }
            for (int y = 1; y < Puzzle.Height; y++)
            {
                cr.MoveTo(PuzzleMargin, y * CellSize + _puzzleStart.Y);
                cr.LineTo(Puzzle.Width * CellSize + _puzzleStart.X, y * CellSize + _puzzleStart.Y);
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
            
            for (int x = 0; x < Puzzle.Width; x++)
            {
                for (int y = 0; y < Puzzle.Height; y++)
                {
                    int cellX = _puzzleStart.X + x * CellSize;
                    int cellY = _puzzleStart.Y + y * CellSize;
                    
                    if (Puzzle.Grid[x, y] == CellStatus.Fill)
                    {
                        cr.Rectangle(cellX, cellY, CellSize, CellSize);
                        cr.Fill();
                    }
                    else if (Puzzle.Grid[x, y] == CellStatus.Cross)
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

            if (x >= Puzzle.Width || y >= Puzzle.Height ||
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

            int cx = _selectedIndex.X; // cell x
            int cy = _selectedIndex.Y; // cell y
            
            CellStatus cellStatus = CellStatus.None;
            if (cx >= 0 && cy >= 0)
                cellStatus = Puzzle.Grid[cx, cy];
            
            if (_buttonJustPressed)
            {
                _buttonJustPressed = false;
                
                // Make sure the player clicked inside the puzzle
                if (_selectedIndex != (-1, -1))
                {
                    if (cx < 0)
                    {
                        // Row clue clicked
                        int clueIndex = cx + Puzzle.RowClues[cy].Length;
                        if (clueIndex >= 0)
                        {
                            Puzzle.RowCluesStatus[cy][clueIndex] = Puzzle.RowCluesStatus[cy][clueIndex] switch
                            {
                                ClueStatus.Unmarked => ClueStatus.Marked,
                                ClueStatus.Marked => ClueStatus.Unmarked,
                                _ => Puzzle.RowCluesStatus[cy][clueIndex]
                            };
                        }
                    }
                    else if (cy < 0)
                    {
                        // Column clue clicked
                        int clueIndex = cy + Puzzle.ColumnClues[cx].Length;
                        if (clueIndex >= 0)
                        {
                            Puzzle.ColumnCluesStatus[cx][clueIndex] = Puzzle.ColumnCluesStatus[cx][clueIndex] switch
                            {
                                ClueStatus.Unmarked => ClueStatus.Marked,
                                ClueStatus.Marked => ClueStatus.Unmarked,
                                _ => Puzzle.ColumnCluesStatus[cx][clueIndex]
                            };
                        }
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

            if (cx < 0 || cy < 0) return;
            
            if (_buttonPressed)
            {
                if (cellStatus != _clickAction)
                {
                    Puzzle.Grid[cx, cy] = _clickAction;
                }
            }
        }
#endregion

    }
}