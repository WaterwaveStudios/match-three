using System.Collections.Generic;
using MatchThree.Tiles;

namespace MatchThree.Matching
{
    public static class MatchFinder
    {
        public static List<HashSet<(int row, int col)>> FindAllMatches(Tile[,] grid, int rows, int cols)
        {
            var allMatched = new HashSet<(int, int)>();
            var matchGroups = new List<HashSet<(int row, int col)>>();

            // Horizontal matches
            for (int r = 0; r < rows; r++)
            {
                int runStart = 0;
                for (int c = 1; c <= cols; c++)
                {
                    bool sameType = c < cols
                        && grid[r, c] != null
                        && grid[r, runStart] != null
                        && grid[r, c].Type == grid[r, runStart].Type;

                    if (!sameType)
                    {
                        int runLength = c - runStart;
                        if (runLength >= 3 && grid[r, runStart] != null)
                        {
                            var group = new HashSet<(int, int)>();
                            for (int k = runStart; k < c; k++)
                            {
                                group.Add((r, k));
                                allMatched.Add((r, k));
                            }
                            matchGroups.Add(group);
                        }
                        runStart = c;
                    }
                }
            }

            // Vertical matches
            for (int c = 0; c < cols; c++)
            {
                int runStart = 0;
                for (int r = 1; r <= rows; r++)
                {
                    bool sameType = r < rows
                        && grid[r, c] != null
                        && grid[runStart, c] != null
                        && grid[r, c].Type == grid[runStart, c].Type;

                    if (!sameType)
                    {
                        int runLength = r - runStart;
                        if (runLength >= 3 && grid[runStart, c] != null)
                        {
                            var group = new HashSet<(int, int)>();
                            for (int k = runStart; k < r; k++)
                            {
                                group.Add((k, c));
                                allMatched.Add((k, c));
                            }
                            matchGroups.Add(group);
                        }
                        runStart = r;
                    }
                }
            }

            return matchGroups;
        }

        public static bool HasAnyMatch(Tile[,] grid, int rows, int cols)
        {
            return FindAllMatches(grid, rows, cols).Count > 0;
        }
    }
}
