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
        Empty,
        Fill,
        Cross,
        None
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
        /// Number clues appearing above the puzzle. Numbers go from top to bottom.
        /// </summary>
        public int[][] ColumnClues { get; private set; }
        
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
            GenerateClues();
            Grid = new CellStatus[solution.GetLength(1), solution.GetLength(0)];
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
            int numRows = Convert.ToInt32(values[0]);
            int numColumns = Convert.ToInt32(values[1]);
            var solution = new CellStatus[numRows, numColumns];

            for (int r = 0; r < numRows; r++)
            {
                if (parser.EndOfData)
                    // File ended earlier than expected
                    throw new ArgumentException($"{fp} has less rows than listed.");
                        
                values = parser.ReadFields();
                    
                if (values.Length != numColumns)
                    // Column count is wrong in this row
                    throw new ArgumentException($"{fp} has a bad or inconsistent column count.");

                for (int c = 0; c < numColumns; c++)
                {
                    solution[r, c] = values[c] switch
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
            int numRows = Solution.GetLength(0);
            int numColumns = Solution.GetLength(1);
            RowClues = new int[numRows][];
            ColumnClues = new int[numColumns][];
            
            // Find clues for each row
            for (int r = 0; r < numRows; r++)
            {
                var clues = new List<int>(numColumns / 2 + 1); // max possible number of clues in this row
                var count = 0;
                for (int c = 0; c < numColumns; c++)
                {
                    // Iter through each cell in this row to determine the clues
                    CellStatus cell = Solution[r,c];
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
                
                RowClues[r] = clues.ToArray();
            }
            
            // Find clues for each column
            for (int c = 0; c < numColumns; c++)
            {
                var clues = new List<int>(numRows / 2 + 1); // max possible number of clues in this column
                int count = 0;
                for (int r = 0; r < numRows; r++)
                {
                    // Iter through each cell in this column to determine the clues
                    CellStatus cell = Solution[r,c];
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

                ColumnClues[c] = clues.ToArray();
            }
        }
    }
}