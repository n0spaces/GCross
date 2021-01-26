using System;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;

namespace GCross
{
    /// <summary>
    /// Status of a cell in the puzzle.
    /// </summary>
    public enum CellStatus
    {
        /// <summary>
        /// The cell is unmarked. Not used in the solution.
        /// </summary>
        Empty,
        
        /// <summary>
        /// The cell is filled in.
        /// </summary>
        Fill,
        
        /// <summary>
        /// The cell is marked as empty.
        /// </summary>
        Cross,
        
        /// <summary>
        /// No cell status. (Only used in PuzzleDrawingArea)
        /// </summary>
        None
    }

    public enum ClueStatus
    {
        /// <summary>
        /// The clue is not solved and not marked.
        /// </summary>
        Unmarked,
        
        /// <summary>
        /// The clue is manually marked by the player.
        /// </summary>
        Marked,
        
        /// <summary>
        /// The clue is solved and can automatically be marked.
        /// </summary>
        Solved
    }

    public class Puzzle
    {
        /// <summary>
        /// The player's puzzle grid.
        /// </summary>
        public CellStatus[,] Grid { get; set; }

        /// <summary>
        /// The solved puzzle grid. Filled cells are CellStatus.Fill, crossed or empty cells are CellStatus.Cross.
        /// </summary>
        public CellStatus[,] Solution { get; private set; }

        /// <summary>
        /// Number clues appearing to the left of the puzzle. Numbers go from left to right.
        /// </summary>
        public int[][] RowClues { get; private set; }

        /// <summary>
        /// Status of each row clue.
        /// </summary>
        public ClueStatus[][] RowCluesStatus { get; set; }

        /// <summary>
        /// Number clues appearing above the puzzle. Numbers go from top to bottom.
        /// </summary>
        public int[][] ColumnClues { get; private set; }
        
        /// <summary>
        /// Status of each column clue.
        /// </summary>
        public ClueStatus[][] ColumnCluesStatus { get; set; }
        
        /// <summary>
        /// Number of vertical columns in the puzzle.
        /// </summary>
        public int Width => Grid.GetLength(0);

        /// <summary>
        /// Number of horizontal rows in the puzzle.
        /// </summary>
        public int Height => Grid.GetLength(1);

        /// <summary>
        /// Create a new Puzzle object from the given solution.
        /// </summary>
        /// <param name="solution"></param>
        public Puzzle(CellStatus[,] solution)
        {
            Solution = solution;
            Grid = new CellStatus[solution.GetLength(0), solution.GetLength(1)];
            GenerateClues();
        }

        /// <summary>
        /// Create a puzzle by loading a CSV file. Each value must be comma-separated. The first line should contain
        /// the number of rows and columns in the puzzle. Each line after that should contain the pattern of each row.
        /// The values should be "1" if the cell is filled, and "0" if it is empty.
        /// </summary>
        /// <param name="fp">Filepath</param>
        /// <returns>Puzzle</returns>
        public static Puzzle LoadFromCsv(string fp)
        {
            using var parser = new TextFieldParser(fp);
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");

            string[] values = parser.ReadFields();
            int numRows = Convert.ToInt32(values[1]);
            int numColumns = Convert.ToInt32(values[0]);
            var solution = new CellStatus[numColumns, numRows];

            for (int y = 0; y < numRows; y++)
            {
                if (parser.EndOfData)
                    // File ended earlier than expected
                    throw new ArgumentException($"{fp} has less rows than listed.");
                        
                values = parser.ReadFields();
                    
                if (values.Length != numColumns)
                    // Column count is wrong in this row
                    throw new ArgumentException($"{fp} has a bad or inconsistent column count.");

                for (int x = 0; x < numColumns; x++)
                {
                    solution[x, y] = values[x] switch
                    {
                        "1" => CellStatus.Fill,
                        "0" => CellStatus.Cross,
                        _ => throw new ArgumentException($"A value in {fp} is not 0 or 1.")
                    };
                }
            }
                
            if (!parser.EndOfData)
                // File ends later than expected
                throw new ArgumentException($"{fp} has more rows than listed.");

            return new Puzzle(solution);
        }

        /// <summary>
        /// Generate the clues for each row and column from the grid.
        /// </summary>
        private void GenerateClues()
        {
            RowClues = new int[Height][];
            RowCluesStatus = new ClueStatus[Height][];
            ColumnClues = new int[Width][];
            ColumnCluesStatus = new ClueStatus[Width][];
            
            // Find clues for each row
            for (int y = 0; y < Height; y++)
            {
                var clues = new List<int>(Width / 2 + 1); // max possible number of clues in this row
                var count = 0;
                for (int x = 0; x < Width; x++)
                {
                    // Iter through each cell in this row to determine the clues
                    CellStatus cell = Solution[x, y];
                    if (cell == CellStatus.Fill)
                    {
                        count++;
                    }
                    else if (count > 0)
                    {
                        clues.Add(count);
                        count = 0;
                    }
                }
                
                if (count > 0 || clues.Count == 0)
                {
                    // Add clue even if it is zero all cells are empty
                    clues.Add(count);
                }
                
                RowClues[y] = clues.ToArray();
                RowCluesStatus[y] = new ClueStatus[clues.Count];
                for (int i = 0; i < RowCluesStatus[y].Length; i++)
                {
                    RowCluesStatus[y][i] = ClueStatus.Unmarked;
                }
            }
            
            // Find clues for each column
            for (int x = 0; x < Width; x++)
            {
                var clues = new List<int>(Height / 2 + 1); // max possible number of clues in this column
                int count = 0;
                for (int y = 0; y < Height; y++)
                {
                    // Iter through each cell in this column to determine the clues
                    CellStatus cell = Solution[x, y];
                    if (cell == CellStatus.Fill)
                    {
                        count++;
                    }
                    else if (count > 0)
                    {
                        clues.Add(count);
                        count = 0;
                    }
                }

                if (count > 0 || clues.Count == 0)
                {
                    // Add last clue even if it is zero all cells are empty
                    clues.Add(count);
                }

                ColumnClues[x] = clues.ToArray();
                ColumnCluesStatus[x] = new ClueStatus[clues.Count];
                for (int i = 0; i < ColumnCluesStatus[x].Length; i++)
                {
                    ColumnCluesStatus[x][i] = ClueStatus.Unmarked;
                }
            }
        }
        
        /// <summary>
        /// Get the number of filled cells in the given column of the given grid.
        /// </summary>
        /// <param name="y">Column index</param>
        /// <param name="grid">Puzzle grid</param>
        /// <returns>Number of filled cells in the column</returns>
        private int GetColumnFilledCount(int x, CellStatus[,] grid)
        {
            int count = 0;
            for (int y = 0; y < Height; y++)
            {
                if (grid[x, y] == CellStatus.Fill)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Get the number of filled cells in the given row of the given grid.
        /// </summary>
        /// <param name="x">Row index</param>
        /// <param name="grid">Puzzle grid</param>
        /// <returns>Number of filled cells in the row</returns>
        private int GetRowFilledCount(int y, CellStatus[,] grid)
        {
            int count = 0;
            for (int x = 0; x < Width; x++)
            {
                if (grid[x, y] == CellStatus.Fill)
                    count++;
            }
            return count;
        }
        
        /// <summary>
        /// Get cells that the player marked incorrectly.
        /// </summary>
        /// <returns>Array of indices as tuples</returns>
        public (int, int)[] GetMistakes()
        {
            List<(int, int)> mistakes = new List<(int, int)>();
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if ((Grid[x, y] == CellStatus.Cross && Solution[x, y] == CellStatus.Fill) ||
                        (Grid[x, y] == CellStatus.Fill && Solution[x, y] == CellStatus.Cross))
                    {
                        mistakes.Add((x, y));
                    }
                }
            }
            return mistakes.ToArray();
        }

        /// <summary>
        /// Update the status of each clue that is solved.
        /// </summary>
        public void UpdateSolvedClues()
        {
            // Search each column
            for (int x = 0; x < Width; x++)
            {
                UpdateSolvedCluesInIndex(x, -1);
            }
            
            // Update each row
            for (int y = 0; y < Height; y++)
            {
                UpdateSolvedCluesInIndex(-1, y);
            }
        }

        /// <summary>
        /// Update the solved clues in the given row/column index.
        /// </summary>
        /// <param name="x">The column to search, or -1 if a row should be searched.</param>
        /// <param name="y">The row to search, or -1 if a column should be searched.</param>
        private void UpdateSolvedCluesInIndex(int x, int y)
        {
            int[] clues;
            ClueStatus[] cluesStatus;
            int cellCount;
            bool isRow;

            if (y == -1 && x != -1)
            {
                clues = ColumnClues[x];
                cluesStatus = ColumnCluesStatus[x];
                cellCount = Height;
                isRow = false;
            }
            else if (x == -1 && y != -1)
            {
                clues = RowClues[y];
                cluesStatus = RowCluesStatus[y];
                cellCount = Width;
                isRow = true;
            }
            else throw new ArgumentException("A given index must be -1 to tell if this is a row or column.");
            
            int clueIndex;
            int fillCount = 0;
            int cluesSolved = 0;

            // Unmark any clues in this column as solved
            for (clueIndex = 0; clueIndex < cluesStatus.Length; clueIndex++)
            {
                if (cluesStatus[clueIndex] == ClueStatus.Solved) 
                    cluesStatus[clueIndex] = ClueStatus.Unmarked;
            }

            clueIndex = 0;
            
            // Skip if too many cells are filled
            if (isRow)
            {
                if (GetRowFilledCount(y, Grid) > GetRowFilledCount(y, Solution)) return;
            }
            else
            {
                if (GetColumnFilledCount(x, Grid) > GetColumnFilledCount(x, Solution)) return;
            }

            int i = 0;
            bool reverse = false;
            
            // Step through each cell form start to end to find solved clues.
            // If an empty cell is reached, go in reverse and start searching from end to start.
            while (i >= 0 && i < cellCount)
            {
                if (isRow) x = i;
                else y = i;
                
                // Stop if an empty cell is reached
                if (Grid[x, y] == CellStatus.Empty)
                {
                    // Start searching from end to start to find more solved cells if not done already, otherwise stop
                    if (!reverse)
                    {
                        reverse = true;
                        i = cellCount;
                        clueIndex = clues.Length - 1;
                        fillCount = 0;
                    }
                    else break;
                }
                
                if (Grid[x, y] == CellStatus.Fill)
                    fillCount++;

                if (Grid[x, y] == CellStatus.Cross || 
                    (i == cellCount - 1 && !reverse) || (i == 0 && reverse)) // Crossed cell or end of cells reached
                {
                    if (fillCount != 0)
                    {
                        if (fillCount != clues[clueIndex])
                        {
                            // Player made a mistake, start counting in reverse
                            if (!reverse)
                            {
                                reverse = true;
                                i = cellCount;
                                clueIndex = clues.Length - 1;
                                fillCount = 0;
                            }
                            else break;
                        }
                        else
                        {
                            // This one clue is solved
                            cluesSolved++;
                            cluesStatus[clueIndex] = ClueStatus.Solved;
                            fillCount = 0;

                            if (reverse) clueIndex--;
                            else clueIndex++;

                            if (cluesSolved == clues.Length) break; // All clues solved
                        }
                    }
                }

                if (reverse) i--;
                else i++;
            }
        }
    }
}