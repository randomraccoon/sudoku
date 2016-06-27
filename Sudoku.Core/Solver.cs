using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sudoku.Core
{
    public class Solver
    {
        private readonly Dictionary<Constants.SolvingTechnique, int> _moveCount =
            new Dictionary<Constants.SolvingTechnique, int>();

        public Solver(Board board)
        {
            Board = board;
            foreach (Constants.SolvingTechnique method in Enum.GetValues(typeof(Constants.SolvingTechnique)))
            {
                if (method > Constants.SolvingTechnique.Provided) _moveCount.Add(method, 0);
            }
        }

        public Board Board { get; set; }

        public bool SolveEasiestMove()
        {
            bool moveSolved = false;
            Constants.SolvingTechnique max =
                Enum.GetValues(typeof(Constants.SolvingTechnique)).Cast<Constants.SolvingTechnique>().Last();
            for (var method = Constants.SolvingTechnique.NakedSingle; !moveSolved && method <= max; method++)
            {
                moveSolved = SolveOneMove(method);
            }
            return moveSolved;
        }

        public string MoveCountsToString()
        {
            return _moveCount.Where(i => i.Value > 0).Aggregate("", (current, i) => current + $"{i.Key} - {i.Value}\n");
        }

        public string GetHardestMove()
        {
            string move = "No good move known.";
            try
            {
                move = _moveCount.Last(i => i.Value > 0).Key.ToString();
                return move;
            }
            catch (Exception)
            {
                // ignored
            }
            return move;
        }

        public static bool TechniqueHasFalsePositives(Constants.SolvingTechnique tech)
        {
            IList<Constants.SolvingTechnique> techs = new List<Constants.SolvingTechnique>();
            techs.Add(Constants.SolvingTechnique.NakedSingle);
            techs.Add(Constants.SolvingTechnique.HiddenSingle);
            techs.Add(tech);
            for (int i = 0; i < 100; i++)
            {
                Console.WriteLine(i);
                var game = new Board(DBHelper.GetChallengingBoard());
                var solver = new Solver(game);
                bool changed = true;
                while (changed)
                {
                    foreach (Constants.SolvingTechnique technique in techs)
                    {
                        changed = solver.SolveOneMove(technique);
                        if (!game.IsCorrectlySolved() || game.IsProvenInvalid)
                        {
                            return true;
                        }
                        if (changed) break;
                    }
                }
                if (!solver.GetHardestMove().Equals(tech.ToString()))
                {
                    i--;
                }
            }


            return false;
        }

        /// <summary>
        /// Uses reflection to call a method by the same name as the provided enum
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        private bool SolveOneMove(Constants.SolvingTechnique method)
        {
            bool moveSolved;
            try
            {
                MethodInfo theMethod = GetType().GetMethod($"{method}", BindingFlags.NonPublic | BindingFlags.Instance);
                moveSolved = (bool) theMethod.Invoke(this, null);
            }
            catch (Exception)
            {
                throw new Exception(
                    $"There was an attempt to call a solving method ({method}) which hasn't yet been programmed.");
            }
            if (moveSolved) _moveCount[method]++;
            return moveSolved;
        }

        public bool SolvePuzzle()
        {
            bool changed = true;
            if (!Board.IsCorrectlySolved() || Board.IsProvenInvalid) return false;
            while (changed)
            {
                changed = SolveEasiestMove();
            }
            return Board.IsSolved();
        }

        #region Named Solving Techniques

        // All these methods must have the exact same name as the corresponding enum in Constants.SolvingTechnique
        // When coded, make sure to uncomment the matching enum.
        // ReSharper disable UnusedMember.Local

        /// <summary>
        /// Starting with a random cell, solve the first cell with only one candidate.
        /// </summary>
        /// <returns></returns>
        private bool NakedSingle()
        {
            bool changed = false;
            int randCellIndex = Board.RandomCellId() - 1;
            for (int i = randCellIndex; !changed && i < Constants.TotalCellCount + randCellIndex; i++)
            {
                int cellId = i%Constants.TotalCellCount + 1;
                Cell cell = Board.GetCell(cellId);

                if (cell.IsSolved() || cell.Candidates.SolvedValue == 0) continue;

                Board.SetCellValue(cellId, cell.Candidates.SolvedValue, Constants.SolvingTechnique.NakedSingle);

                changed = true;
            }
            return changed;
        }

        /// <summary>
        /// Starting with a random cell in a random house, look for a candidate that appears in only one cell in a house and solve.
        /// </summary>
        /// <returns></returns>
        private bool HiddenSingle()
        {
            var rand = new Random();

            int randomHouseIndex = rand.Next(27);

            for (int i = randomHouseIndex; i < Constants.TotalHouseCount + randomHouseIndex; i++)
            {
                int houseIndex = i%Constants.TotalHouseCount;
                House house = Board.Houses[houseIndex];

                int randomCandidateIndex = rand.Next(Constants.BoardLength);
                for (int j = randomCandidateIndex; j < Constants.BoardLength + randomCandidateIndex; j++)
                {
                    int val = j%Constants.BoardLength + 1;

                    if (house.Contains(val)) continue;

                    int candidateCount = 0;
                    Cell lastCell = null;

                    int randomCellIndex = rand.Next(Constants.BoardLength);

                    for (int k = randomCellIndex;
                        candidateCount < 2 && k < Constants.BoardLength + randomCellIndex;
                        k++)
                    {
                        int cellIndex = k%Constants.BoardLength;
                        Cell cell = house.Cells[cellIndex];
                        if (cell.Candidates.Contains(val))
                        {
                            candidateCount++;
                            lastCell = cell;
                        }
                    }

                    if (candidateCount == 1
                        && lastCell != null
                        && !lastCell.IsSolved()
                        && (lastCell.Candidates.SolvedValue == val || lastCell.Candidates.SolvedValue == 0))
                    {
                        Board.SetCellValue(lastCell.CellId, val, Constants.SolvingTechnique.HiddenSingle);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Starting with a random house, find a pair of cells with the same two candidates. 
        /// If found, eliminate candidates from remainder of house.
        /// </summary>
        /// <returns></returns>
        private bool NakedPair()
        {
            return NakedTuple(2);
        }

        /// <summary>
        /// Starting with a random house, find a pair of candidates that are only contained within the same two cells of that house.
        /// Eliminate all other candidates from those two cells.
        /// </summary>
        /// <returns></returns>
        private bool HiddenPair()
        {
            return HiddenTuple(2);
        }

        /// <summary>
        /// Starting with a random house, find a candidate that is only found in cells contained in another single house.
        /// Remove that candidate from all cells in the other house.
        /// </summary>
        /// <returns></returns>
        private bool IntersectionRemoval()
        {
            bool changed = false;

            var rnd = new Random();
            House[] houseArray = Board.GetShuffledCopyOfHouseArray(rnd);

            foreach (House house in houseArray)
            {
                //Get list of house's unsolved candidates ready
                var candidateList = new List<int>() {1, 2, 3, 4, 5, 6, 7, 8, 9};
                foreach (Cell cell in house.Cells)
                {
                    if (cell.IsSolved())
                    {
                        candidateList.Remove(cell.Value);
                    }
                }

                //for each candidate, get a list of cells with that candidate
                foreach (int val in candidateList)
                {
                    Cell[] cellList = CellsWithThisCandidateArray(house.Cells, val);
                    if (cellList.Length > 3 || cellList.Length == 0) continue;

                    //Check if each cell in list shares another house
                    int boxNum = cellList[0].BoxNumber;
                    int rowNum = cellList[0].RowNumber;
                    int colNum = cellList[0].ColumnNumber;
                    bool shareBox = true;
                    bool shareRow = true;
                    bool shareCol = true;
                    for (int cellIndex = 1; cellIndex < cellList.Length; cellIndex++)
                    {
                        shareBox = shareBox && boxNum == cellList[cellIndex].BoxNumber;
                        shareRow = shareRow && rowNum == cellList[cellIndex].RowNumber;
                        shareCol = shareCol && colNum == cellList[cellIndex].ColumnNumber;
                    }
                    // If less than two are true, exit
                    if (shareBox ? !(shareRow || shareCol) : !(shareRow && shareCol))
                    {
                        continue;
                    }
                    // Otherwise, remove candidate from all other cells in two shared houses
                    if (shareBox)
                    {
                        foreach (Cell cell in Board.Boxes[boxNum - 1].Cells)
                        {
                            if (!cellList.Contains(cell))
                            {
                                changed = cell.Candidates.EliminateCandidate(val) || changed;
                            }
                        }
                    }
                    if (shareRow)
                    {
                        foreach (Cell cell in Board.Rows[rowNum - 1].Cells)
                        {
                            if (!cellList.Contains(cell))
                            {
                                changed = cell.Candidates.EliminateCandidate(val) || changed;
                            }
                        }
                    }
                    if (shareCol)
                    {
                        foreach (Cell cell in Board.Columns[colNum - 1].Cells)
                        {
                            if (!cellList.Contains(cell))
                            {
                                changed = cell.Candidates.EliminateCandidate(val) || changed;
                            }
                        }
                    }

                    if (changed) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Starting with a random house, find three cells with the same three candidates between them. Remove the candidates from
        /// all other cells in the house.
        /// </summary>
        /// <returns></returns>
        private bool NakedTriple()
        {
            return NakedTuple(3);
        }

        private bool NakedQuad()
        {
            return NakedTuple(4);
        }

        private bool XWing()
        {
            return BasicFish(2);
        }

        private bool SwordFish()
        {
            return BasicFish(3);
        }

        private bool JellyFish()
        {
            return BasicFish(4);
        }

        private bool HiddenTriple()
        {
            return HiddenTuple(3);
        }

        private bool HiddenQuad()
        {
            return HiddenTuple(4);
        }

        private bool Skyscraper()
        {
            //Start with a random candidate
            var rnd = new Random();
            int randomValIndex = rnd.Next(9);
            for (int i = randomValIndex; i < 9 + randomValIndex; i++)
            {
                int val = i % 9 + 1;
                if (Board.IsValueSolved(val)) continue;

                IList<IList<House>> rowsAndCols = new List<IList<House>>(2);
                //Generate lists of rows and cols in which the candidate appears exactly twice
                //There must be at least two in one set for it to work
                IList<House> rows = (from row in Board.Rows
                                     let count = row.CountCellsWithCandidate(val)
                                     where (count == 2)
                                     select row)
                                     .OrderBy(a => rnd.Next())
                                     .ToList();
                if (rows.Count >= 2) rowsAndCols.Add(rows);


                IList<House> cols = (from col in Board.Columns
                                     let count = col.CountCellsWithCandidate(val)
                                     where (count == 2)
                                     select col)
                                     .OrderBy(a => rnd.Next())
                                     .ToList();
                if (cols.Count >= 2) rowsAndCols.Add(cols);

                // Starting randomly with rows or cols, find a skyscraper
                IList<House>[] shuffledRowsAndCols = rowsAndCols.OrderBy(a => rnd.Next()).ToArray();
                foreach (IList<House> lineSet in shuffledRowsAndCols)
                {
                    int[] indexes = Enumerable.Range(0, 2).ToArray();
                    while (indexes[indexes.Length - 1] > 0)
                    {
                        IList<House> chosenLines = indexes.Select(index => lineSet[index]).ToList();
                        if (CheckForSkyscraper(val, chosenLines))
                            return true;
                        indexes = GetNextCombination(indexes, lineSet.Count);
                    }
                }

            }

            return false;
        }

        private bool TwoStringKite()
        {
            //Start with a random candidate
            var rnd = new Random();
            int randomValIndex = rnd.Next(9);
            for (int i = randomValIndex; i < 9 + randomValIndex; i++)
            {
                int val = i % 9 + 1;
                if (Board.IsValueSolved(val)) continue;
                
                //Generate lists of rows and cols in which the candidate appears exactly twice
                //There must be at least 1 line in both the rows and columns
                IList<House> rows = (from row in Board.Rows
                                     let count = row.CountCellsWithCandidate(val)
                                     where (count == 2)
                                     select row)
                                     .ToList();
                if (rows.Count == 0) continue;


                IList<House> cols = (from col in Board.Columns
                                     let count = col.CountCellsWithCandidate(val)
                                     where (count == 2)
                                     select col)
                                     .ToList();
                if (cols.Count == 0) continue;

                if ((from row in rows from col in cols where CheckForTwoStringKite(val, row, col) select row).Any())
                {
                    return true;
                }

            }

            return false;
        }

        private bool YWing()
        {
            return Wing(false);
        }

        // ReSharper disable once InconsistentNaming
        private bool XYZWing()
        {
            return Wing(true);
        }

        //private bool BowmanBingo() //TODO BROKEN
        //{
        //    //Pick a random candidate in a random cell to test
        //    var rnd = new Random();
        //    Cell cell = Board.Cells.Where(c => !c.IsSolved()).OrderBy(x => rnd.Next()).FirstOrDefault();
        //    if (cell == null) return false;
        //    int candidate = cell.Candidates.GetCandidateArray().OrderBy(x => rnd.Next()).FirstOrDefault();
        //    if (candidate == 0) return false;
        //    //Create new board and attempt to solve
        //    var testBoard = new Board(Board);
        //    testBoard.SetCellValue(cell.CellId, candidate, Constants.SolvingTechnique.BowmanBingo);
        //    var testSolver = new Solver(testBoard);
        //    bool solved = testSolver.SolvePuzzle();
        //    //If it finds a contradiction, rule out that candidate and return true
        //    if (!testBoard.IsCorrectlySolved())
        //    {
        //        cell.Candidates.EliminateCandidate(candidate);
        //        return true;
        //    }
        //    //If it solves correctly, set cell to candidate value and return true
        //    else if (testBoard.IsSolved())
        //    {
        //        Board.SetCellValue(cell.CellId, candidate, Constants.SolvingTechnique.BowmanBingo);
        //        return true;
        //    }
        //    throw new Exception("How did we get here?");
        //}

        #endregion

        //The following methods are generalizations of a type of a group of methods above.
        #region Helper Methods
        private bool NakedTuple(int tuple)
        {
            var rnd = new Random();

            bool changed = false;

            House[] houseArray = Board.GetShuffledCopyOfHouseArray(rnd);

            foreach (House house in houseArray)
            {
                //Get list of house's unsolved candidates ready
                var candidateList = new List<int>();
                for (int val = 1; val <= Constants.BoardLength; val++)
                {
                    if (!house.Contains(val))
                    {
                        candidateList.Add(val);
                    }
                }

                // If the house has <= [tuple] candidates, there aren't any values left to rule out
                if (candidateList.Count <= tuple) continue;

                //Get random-order list of all cells with multiple but at most [tuple] candidates
                List<Cell> cellList = (from cell in house.Cells
                                       let count = cell.Candidates.Count()
                                       where count > 1 && count <= tuple
                                       select cell
                    ).OrderBy(x => rnd.Next()).ToList();

                //If there are less than [tuple] cells in this list, this method is of no use
                if (cellList.Count < tuple) continue;

                // For each combination of three cells in this list, look for a set with only three unique candidates between them

                //create an array of cell indexes {0, 1, 2...}
                int[] indexes = Enumerable.Range(0, tuple).ToArray();
                
                while (indexes[indexes.Length - 1] > 0)
                {
                    ISet<int> candidateSet = new SortedSet<int>();
                    foreach (int index in indexes)
                    {
                        foreach (int cand in candidateList)
                        {
                            if (cellList[index].Candidates.Contains(cand))
                            {
                                candidateSet.Add(cand);
                            }
                        }
                    }
                    if (candidateSet.Count == tuple) // Success! Remove these candidates from all other cells in house
                    {
                        //trim these three cells from cell list
                        for (int i = indexes.Length - 1; i >= 0; i--)
                        {
                            cellList.RemoveAt(indexes[i]);
                        }
                        //for each cell in the list, remove each candidate in the set
                        foreach (Cell cell in cellList)
                        {
                            foreach (int val in candidateSet)
                            {
                                changed = cell.Candidates.EliminateCandidate(val) || changed;
                            }
                        }
                        if (changed) return true;
                    }

                    indexes = GetNextCombination(indexes, cellList.Count);
                }
            }

            return false;
        }

        /// <summary>
        /// x candidates can only be found in x cells of the same house, eliminating all other candidates in those x cells
        /// </summary>
        /// <param name="tuple"></param>
        /// <returns></returns>
        private bool HiddenTuple(int tuple)
        {
            var rnd = new Random();
            House[] houseArray = Board.GetShuffledCopyOfHouseArray(rnd);

            foreach (House house in houseArray)
            {
                //Get list of candidates ready
                var candidateList = new List<int>();
                for (int val = 1; val <= Constants.BoardLength; val++)
                {
                    if (!house.Contains(val) && house.CountCellsWithCandidate(val) <= tuple)
                    {
                        candidateList.Add(val);
                    }
                }

                //If there are less than tuple candidates to work with, the test is invalid
                if (candidateList.Count < tuple) continue;

                //check each combination of the remaining candidates to see if they are in the same cells
                int[] indexes = Enumerable.Range(0, tuple).ToArray();
                while (indexes[indexes.Length - 1] > 0)
                {
                    //Needs to be a set
                    ISet<Cell> cellSet = new SortedSet<Cell>();
                    foreach (Cell cell in house.Cells)
                    {
                        foreach (int index in indexes)
                        {
                            if (cell.CouldBe(candidateList[index]))
                            {
                                cellSet.Add(cell);
                            }
                        }
                    }
                    if (cellSet.Count == tuple) //Success! Remove all other candidates from those cells
                    {
                        bool changed = false;
                        IList<int> candidates = indexes.Select(index => candidateList[index]).ToList();

                        foreach (Cell cell in cellSet)
                        {
                            for (int val = 1; val <= Constants.BoardLength; val++)
                            {
                                if (candidates.Contains(val)) continue;
                                changed = cell.Candidates.EliminateCandidate(val) || changed;
                            }
                        }
                        if (changed)
                            return true;
                    }

                    indexes = GetNextCombination(indexes, candidateList.Count);
                }

            }

            return false;
        }

        /// <summary>
        /// Starting with a random unsolved candidate, find x lines (base set) 
        /// wherein it appears only within the same x (or less) opposing lines 
        /// (cover set). Remove candidate from all other cells in cover set. 
        /// </summary>
        /// <param name="tuple"></param>
        /// <returns></returns>
        private bool BasicFish(int tuple)
        {
            //Start with a random candidate
            var rnd = new Random();
            int randomValIndex = rnd.Next(9);
            for (int i = randomValIndex; i < 9 + randomValIndex; i++)
            {
                int val = i % 9 + 1;
                if (Board.IsValueSolved(val)) continue;

                //Generate lists of rows and cols in which the candidate appears between 2 and tuple times
                //There must be at least x lines in both the base and cover sets
                IList<House> rows = (from row in Board.Rows
                                     let count = row.CountCellsWithCandidate(val)
                                     where (count > 1 && count <= tuple)
                                     select row)
                                     .OrderBy(a => rnd.Next())
                                     .ToList();
                if (rows.Count < tuple) continue;


                IList<House> cols = (from col in Board.Columns
                                     let count = col.CountCellsWithCandidate(val)
                                     where (count > 1 && count <= tuple)
                                     select col)
                                     .OrderBy(a => rnd.Next())
                                     .ToList();
                if (cols.Count < tuple) continue;

                // Starting randomly with rows or cols, find a basic fish
                IList<House>[] rowsAndCols = {rows, cols};
                IList<House>[] shuffledRowsAndCols = rowsAndCols.OrderBy(a => rnd.Next()).ToArray();
                foreach (IList<House> lineSet in shuffledRowsAndCols)
                {
                    int[] indexes = Enumerable.Range(0, tuple).ToArray();
                    while (indexes[indexes.Length - 1] > 0)
                    {
                        IList<House> chosenLines = indexes.Select(index => lineSet[index]).ToList();
                        if (CheckForNewFish(val, chosenLines))
                            return true;
                        indexes = GetNextCombination(indexes, lineSet.Count);
                    }
                }

            }

            return false;
        }

        // ReSharper disable once InconsistentNaming
        private bool Wing(bool isXYZWing)
        {
            //Start with a random candidate
            var rnd = new Random();
            int randomValIndex = rnd.Next(9);
            for (int i = randomValIndex; i < 9 + randomValIndex; i++)
            {
                int val = i % 9 + 1;
                if (Board.IsValueSolved(val)) continue;

                //Get shuffled list of all cells which contain val and one other candidate
                IList<Cell> cellsToCheck = Board.Cells
                    .Where(cell => cell.CouldBe(val) && cell.Candidates.Count() == 2)
                    .OrderBy(x => rnd.Next())
                    .ToList();
                if (cellsToCheck.Count < 2) continue;

                //Check each combination of 2 of these cells
                int[] indexes = { 0, 1 };
                while (indexes[1] != 0)
                {
                    Cell[] pincerCells = { cellsToCheck[indexes[0]], cellsToCheck[indexes[1]] };
                    if (!isXYZWing && CheckForYWing(val, pincerCells)) return true;
                    if (isXYZWing && CheckForXYZWing(val, pincerCells)) return true;
                    indexes = GetNextCombination(indexes, cellsToCheck.Count);
                }
            }
            return false;
        }

        /// <summary>
        /// Checks a certain combination of lines for a new fish (of that many lines)
        /// </summary>
        /// <param name="val"></param>
        /// <param name="chosenLines"></param>
        /// <returns></returns>
        private bool CheckForNewFish(int val, IList<House> chosenLines)
        {
            int len = chosenLines.Count;
            bool change = false;
            // gather indexes of each cell with val as candidate for each line
            var coverSet = new SortedSet<int>();
            foreach (House line in chosenLines)
            {
                if (line.Contains(val))
                    return false;
                for (int i = 0; i < line.Cells.Count; i++)
                {
                    if (line.Cells[i].CouldBe(val))
                    {
                        coverSet.Add(i + 1);
                        if (coverSet.Count > len) return false;
                    }
                }
            }

            // if count of indexes < count of lines, throw exception
            if (coverSet.Count < len) throw new Exception("What the what?");

            // otherwise gather cells in coverset (but not base set) into new list
            House.HouseType baseType = chosenLines[0].MyHouseType;
            House.HouseType coverType = (baseType == House.HouseType.Column) ? House.HouseType.Row : House.HouseType.Column;
            List<Cell> baseCells = chosenLines.SelectMany(line => line.Cells).ToList();
            List<Cell> coverCells = (from i in coverSet
                                     select Board.GetHouse(coverType, i - 1)
                                        into line
                                     from cell
                                     in line.Cells
                                     where !baseCells.Contains(cell)
                                     select cell)
                                    .ToList();
            // and eliminate that candidate from each cell in the list
            foreach (Cell cell in coverCells)
            {
                change = cell.Candidates.EliminateCandidate(val) || change;
            }
            // return whether change was made
            return change;
        }

        private bool CheckForSkyscraper(int val, IEnumerable<House> chosenLines) //TODO false positives
        {
            bool change = false;
            
            // create set of chosen 4 cells
            IList<Cell> chosenCells = (from line in chosenLines from cell in line.Cells where cell.CouldBe(val) select cell).ToList();
            IList<Cell> endCells = new List<Cell>(2);

            //make sure there's a link between the two lines
            int maxCount = 1;
            foreach (Cell chosenCell in chosenCells)
            {
                int count = chosenCells.Count(c => chosenCell.CanSee(c));
                if (count == 1) endCells.Add(chosenCell);
                maxCount = Math.Max(count, maxCount);
            }
            if (maxCount != 2) return false;

            // gather cells which see two of these cells into a list
            IList<Cell> otherCells = new List<Cell>();
            foreach (Cell otherCell in Board.Cells)
            {
                if (otherCell.IsSolved()) continue;
                if (!otherCell.CouldBe(val)) continue;
                if (chosenCells.Contains(otherCell)) continue;
                int seeCount = endCells.Count(endCell => otherCell.CanSee(endCell));
                if (seeCount == 2) otherCells.Add(otherCell);
            }

            // and eliminate that candidate from each cell in the second list
            foreach (Cell cell in otherCells)
            {
                change = cell.Candidates.EliminateCandidate(val) || change;
            }
            // return whether change was made
            return change;
        }
        
        private bool CheckForTwoStringKite(int val, House row, House col)
        {
            //Get cells of note in row and column
            IList<Cell> rowCells = row.Cells.Where(cell => cell.CouldBe(val)).ToList();
            IList<Cell> colCells = col.Cells.Where(cell => cell.CouldBe(val)).ToList();

            //Make sure that none of the cells are the same
            //Make sure that (only) one combination of cells from row and col share a box and are not the same point
            int boxCount = 0;
            int box = -1;
            
            foreach (Cell rowCell in rowCells)
            {
                foreach (Cell colCell in colCells)
                {
                    if (rowCell.Equals(colCell)) return false;
                    if (rowCell.BoxNumber == colCell.BoxNumber)
                    {
                        boxCount++;
                        box = rowCell.BoxNumber;
                    }

                }
            }
            if (boxCount != 1) return false;
            //Identify end points & cell that can see both
            int colNum = -1;
            int rowNum = -1;
            foreach (Cell rowCell in rowCells)
            {
                if (rowCell.BoxNumber != box) colNum = rowCell.ColumnNumber;
            }
            foreach (Cell colCell in colCells)
            {
                if (colCell.BoxNumber != box) rowNum = colCell.RowNumber;
            }
            Cell newCell = Board.GetCell(Cell.GetCellId(rowNum, colNum));

            //if it has this candidate, remove it
            bool changed = false;
            if (!newCell.IsSolved() && newCell.CouldBe(val))
            {
                changed = newCell.Candidates.EliminateCandidate(val);
            }

            //return whether a change was made
            return changed;
        }

        private bool CheckForYWing(int val, IReadOnlyList<Cell> pincerCells)
        {
            if (pincerCells[0].Candidates.Equals(pincerCells[1].Candidates)) return false;
            
            //identify a & b
            int[] hingeValues = (from pincerCell in pincerCells
                                      from candidate in pincerCell.Candidates.GetCandidateArray()
                                      where candidate != val
                                      select candidate)
                                      .ToArray();

            //find all cells which see both, could be the shared value, aren't solved
            //while doing so, look for hinge
            IList<Cell> seeingCells = new List<Cell>();
            bool foundHinge = false;
            foreach (Cell cell in Board.Cells)
            {
                if (cell.CanSee(pincerCells[0]) && cell.CanSee(pincerCells[1]) && !cell.IsSolved())
                {
                    if (cell.CouldBe(hingeValues[0]) && cell.CouldBe(hingeValues[1]) && cell.Candidates.Count() == 2)
                    {
                        foundHinge = true;
                    }
                    else if (cell.CouldBe(val))
                    {
                        seeingCells.Add(cell);
                    }
                }
            }
            if (!foundHinge || seeingCells.Count == 0) return false;

            // Remove candidate from all cells in list
            foreach (Cell seeingCell in seeingCells)
            {
                seeingCell.Candidates.EliminateCandidate(val);
            }
            return true;
        }

        // ReSharper disable once InconsistentNaming
        private bool CheckForXYZWing(int val, IReadOnlyList<Cell> pincerCells)
        {
            if (pincerCells[0].Candidates.Equals(pincerCells[1].Candidates)) return false;

            //identify a & b
            int[] hingeValues = (from pincerCell in pincerCells
                                 from candidate in pincerCell.Candidates.GetCandidateArray()
                                 where candidate != val
                                 select candidate)
                                      .ToArray();
            
            //find hinge
            Cell hinge = Board.Cells.FirstOrDefault(cell => cell
                            .CanSee(pincerCells[0]) 
                            && cell.CanSee(pincerCells[1]) 
                            && cell.CouldBe(val) 
                            && cell.CouldBe(hingeValues[0]) 
                            && cell.CouldBe(hingeValues[1]) 
                            && cell.Candidates.Count() == 3);
            if (hinge == null) return false;

            //find all cells which see all hinge and pincers, have value, and aren't solved
            IList<Cell> seeingCells = Board.Cells
                                        .Where(cell => cell.CanSee(pincerCells[0]) 
                                            && cell.CanSee(pincerCells[1]) 
                                            && cell.CanSee(hinge) 
                                            && !cell.IsSolved() 
                                            && cell.CouldBe(val))
                                        .ToList();
            if (seeingCells.Count == 0) return false;

            // Remove candidate from all cells in list
            foreach (Cell seeingCell in seeingCells)
            {
                seeingCell.Candidates.EliminateCandidate(val);
            }
            return true;
        }

        #endregion


        #region Static Methods

        /// <summary>
        /// Takes an array with a combination of indexes [0,1,2,3] and properly increments it. [0,1,2,4]
        /// </summary>
        /// <param name="indexes"></param>
        /// <param name="indexCount">Typically "myList.Count"</param>
        /// <returns>The incremented array, or all zeroes if that was the last combination</returns>
        public static int[] GetNextCombination(int[] indexes, int indexCount)
        {

            //increment the last index
            indexes[indexes.Length - 1]++;

            //while the last index is too high and two neighboring indexes have a difference > 1
            //  find the last cell that is over one lower than the one after it
            //  increment that cell, and fill all the others after it
            //  if no change is made, the set is ruled out


            int pointer = 0;
            while (indexes[indexes.Length - 1] >= indexCount && pointer >= 0)
            {
                pointer = LastNonConsecutiveIndex(indexes);
                if (pointer >= 0)
                {
                    //increment appropriate indexes
                    indexes[pointer]++;
                    for (int i = pointer + 1; i < indexes.Length; i++)
                    {
                        indexes[i] = indexes[pointer] + (i - pointer);
                    }
                }
            }
            return indexes[indexes.Length - 1] <= indexCount && pointer >= 0 ? indexes : new int[indexes.Length];
        }


        /// <summary>
        /// Returns the last index that is at least two apart from the one after it
        /// Returns -1 if no such index exists (or the pattern for the whole array is arr[i+1} = arr[i]+1)
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public static int LastNonConsecutiveIndex(IReadOnlyList<int> arr)
        {
            if (arr.Count < 2) return -2;
            for (int i = arr.Count - 2; i >= 0; i--)
            {
                if (arr[i + 1] - (i + 1) != arr[i] - (i)) return i;
            }
            return -1;
        }

        private static Cell[] CellsWithThisCandidateArray(IEnumerable<Cell> inList, int candidate)
        {
            return inList.Where(cell => cell.Candidates.Contains(candidate)).ToArray();
        }

        #endregion

        
    }
}