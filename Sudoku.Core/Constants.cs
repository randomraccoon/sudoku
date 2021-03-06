using System;
using System.Collections.Generic;

namespace Sudoku.Core
{
    public static class Constants
    {
        public static int BoardLength = 9;
        public static int TotalCellCount = BoardLength*BoardLength;
        public static int TotalHouseCount = BoardLength*3;

        /// <summary>
        /// List of strategies and order of difficulty taken from paper in Docs folder "Sudoku Creation and Grading".
        /// Keep enum commented out until the corresponding method (in Solver) has been programmed.
        /// Order/value determines which methods are tried first.
        /// </summary>
        public enum SolvingTechnique
        { 
            Provided,
            PlayerInput,
            NakedSingle, // Last unfilled cell in a house
            HiddenSingle, // Only cell in a house with a particular candidate
            NakedPair, // Two cells in the same house have the same two candidates, eliminating those candidates from the rest of the house
            HiddenPair, // Two candidates can only be found in two cells of the same house, eliminating all other candidates in those two cells
            IntersectionRemoval, // All of a certain candidate in a house share another house, eliminating them from the other cells in the second house
            NakedTriple, // Three cells in the same house have only three candidates between them, eliminating those candidates from the rest of the house
            HiddenTriple, // Three candidates can only be found in three cells of the same house, eliminating all other candidates in those three cells
            NakedQuad, // Four cells in the same house have only four candidates between them, eliminating those candidates from the rest of the house
            HiddenQuad, // Four candidates can only be found in four cells of the same house, eliminating all other candidates in those three cells
            XWing, // For a certain candidate, pick two lines (base sets) in which all the candidate is in the same two opposing lines (cover sets). Remove candidate from all other cells in the cover sets.
            Skyscraper, //Taken from Hodoku's Single digit patterns, start like X-wing. But if any two cells from different lines see each other, remove candidate from all cells that see both the ends of the chain.
            TwoStringKite, //Taken from Hodoku's single digit patterns, pick a digit and find a row & column of two candidates each, where a box is shared by a cell from each. Remove from cell that sees both ends.
            SimpleColoring,
            YWing, // For a certain candidate, find two cells which contain c and one other candidate, but are different (ac & bc). If any of the cells that see both are ab, remove c from all cells that see both
            Swordfish, // 3-line basic fish (like X-Wing)
            BiValueUniversalGrave, // if all unsolved cells have two candidates except one with three, the odd number that couldn't be paired with another cell is the solution for that cell
            //XCycle,
            //// ReSharper disable once InconsistentNaming
            //XYChain,
            //Medusa3D,
            Jellyfish, // 4-line basic fish
            //AvoidableRectangle,
            //UniqueRectangle,
            //HiddenUniqueRectangle,
            //// ReSharper disable once InconsistentNaming
            XYZWing,
            //// ReSharper disable once InconsistentNaming
            WXYZWing,
            //AlignedPairExclusion,
            //GroupedXCycle,
            //EmptyRectangle,
            //FinnedXWing,
            //FinnedSwordfish,
            //FrankenSwordfish,
            //AlternInferenceChain,
            //DigitForcingChain,
            //CellForcingChain,
            //UnitForcingChain,
            SueDeCoq,
            //AlmostLockedSet,
            //DeathBlossom,
            //PatternOverlay,
            //QuadForcingChain,
            //Nishio,
            //BowmanBingo,
            Unsolved
        }

        public static Dictionary<SolvingTechnique, int> TechniquePointValue = new Dictionary<SolvingTechnique, int>()
        {
            { SolvingTechnique.NakedSingle, 1 },
            { SolvingTechnique.HiddenSingle, 1 },
            { SolvingTechnique.NakedPair, 3 },
            { SolvingTechnique.HiddenPair, 3 },
            { SolvingTechnique.IntersectionRemoval, 5 },
            { SolvingTechnique.NakedTriple, 10 },
            { SolvingTechnique.HiddenTriple, 15 },
            { SolvingTechnique.NakedQuad, 30 },
            { SolvingTechnique.HiddenQuad, 50 },
            { SolvingTechnique.XWing, 25 },
            { SolvingTechnique.Skyscraper, 40 },
            { SolvingTechnique.TwoStringKite, 40 },
            { SolvingTechnique.SimpleColoring, 70 },
            { SolvingTechnique.YWing, 60 },
            { SolvingTechnique.Swordfish, 100 },
            { SolvingTechnique.Jellyfish, 200 },
            { SolvingTechnique.XYZWing, 120 },
            { SolvingTechnique.WXYZWing, 300 },
            { SolvingTechnique.SueDeCoq, 500 },
            { SolvingTechnique.BiValueUniversalGrave, 50 }
        };

        public static string GetEasiestMove(string techniqueName1, string techniqueName2)
        {
            SolvingTechnique tech1;
            SolvingTechnique tech2;
            try
            {
                tech1 = (SolvingTechnique) Enum.Parse(typeof(SolvingTechnique), techniqueName1);
            }
            catch (Exception)
            {
                tech1 = SolvingTechnique.Unsolved;
            }

            try
            {
                tech2 = (SolvingTechnique) Enum.Parse(typeof(SolvingTechnique), techniqueName2);
            }
            catch (Exception)
            {
                tech2 = SolvingTechnique.Unsolved;
            }
            
            return tech1 < tech2 ? tech1.ToString() : tech2.ToString();
        }
    }
}
