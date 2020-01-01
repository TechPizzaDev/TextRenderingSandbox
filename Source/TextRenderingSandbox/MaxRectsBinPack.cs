using System;
using System.Collections.Generic;

namespace TextRenderingSandbox
{
    public struct Rect
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public int Area => Width * Height;

        public Rect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    public enum FreeRectChoiceHeuristic
    {
        RectBestShortSideFit, ///< -BSSF: Positions the rectangle against the short side of a free rectangle into which it fits the best.
        RectBestLongSideFit, ///< -BLSF: Positions the rectangle against the long side of a free rectangle into which it fits the best.
        RectBestAreaFit, ///< -BAF: Positions the rectangle into the smallest free rect into which it fits.
        RectBottomLeftRule, ///< -BL: Does the Tetris placement.
        RectContactPointRule ///< -CP: Choosest the placement where the rectangle touches other rects as much as possible.
    }

    public class MaxRectsBinPack
    {
        public int BinWidth { get; private set; }
        public int BinHeight { get; private set; }
        public bool AllowRotations { get; private set; }

        public ListArray<Rect> UsedRectangles { get; } = new ListArray<Rect>();
        public ListArray<Rect> FreeRectangles { get; } = new ListArray<Rect>();

        public MaxRectsBinPack(int width, int height, bool rotations = true)
        {
            Init(width, height, rotations);
        }

        public void Init(int width, int height, bool rotations = true)
        {
            BinWidth = width;
            BinHeight = height;
            AllowRotations = rotations;

            Rect n = new Rect();
            n.X = 0;
            n.Y = 0;
            n.Width = width;
            n.Height = height;

            UsedRectangles.Clear();

            FreeRectangles.Clear();
            FreeRectangles.Add(n);
        }

        public Rect Insert(int width, int height, FreeRectChoiceHeuristic method)
        {
            Rect newNode = new Rect();
            int score1 = 0; // Unused in this function. We don't need to know the score after finding the position.
            int score2 = 0;
            switch (method)
            {
                case FreeRectChoiceHeuristic.RectBestShortSideFit:
                    newNode = FindPositionForNewNodeBestShortSideFit(width, height, ref score1, ref score2);
                    break;

                case FreeRectChoiceHeuristic.RectBottomLeftRule:
                    newNode = FindPositionForNewNodeBottomLeft(width, height, ref score1, ref score2);
                    break;

                case FreeRectChoiceHeuristic.RectContactPointRule:
                    newNode = FindPositionForNewNodeContactPoint(width, height, ref score1);
                    break;

                case FreeRectChoiceHeuristic.RectBestLongSideFit:
                    newNode = FindPositionForNewNodeBestLongSideFit(width, height, ref score2, ref score1);
                    break;

                case FreeRectChoiceHeuristic.RectBestAreaFit:
                    newNode = FindPositionForNewNodeBestAreaFit(width, height, ref score1, ref score2);
                    break;
            }

            if (newNode.Height == 0)
                return newNode;

            int numRectanglesToProcess = FreeRectangles.Count;
            for (int i = 0; i < numRectanglesToProcess; ++i)
            {
                if (SplitFreeNode(FreeRectangles[i], ref newNode))
                {
                    FreeRectangles.RemoveAt(i);
                    --i;
                    --numRectanglesToProcess;
                }
            }

            PruneFreeList();

            UsedRectangles.Add(newNode);
            return newNode;
        }

        public void Insert(List<Rect> rects, List<Rect> dst, FreeRectChoiceHeuristic method)
        {
            dst.Clear();

            while (rects.Count > 0)
            {
                int bestScore1 = int.MaxValue;
                int bestScore2 = int.MaxValue;
                int bestRectIndex = -1;
                Rect bestNode = new Rect();

                for (int i = 0; i < rects.Count; ++i)
                {
                    int score1 = 0;
                    int score2 = 0;
                    Rect newNode = ScoreRect(rects[i].Width, rects[i].Height, method, ref score1, ref score2);

                    if (score1 < bestScore1 || (score1 == bestScore1 && score2 < bestScore2))
                    {
                        bestScore1 = score1;
                        bestScore2 = score2;
                        bestNode = newNode;
                        bestRectIndex = i;
                    }
                }

                if (bestRectIndex == -1)
                    return;

                PlaceRect(bestNode);
                rects.RemoveAt(bestRectIndex);
            }
        }

        void PlaceRect(Rect node)
        {
            int numRectanglesToProcess = FreeRectangles.Count;
            for (int i = 0; i < numRectanglesToProcess; ++i)
            {
                if (SplitFreeNode(FreeRectangles[i], ref node))
                {
                    FreeRectangles.RemoveAt(i);
                    --i;
                    --numRectanglesToProcess;
                }
            }

            PruneFreeList();

            UsedRectangles.Add(node);
        }

        Rect ScoreRect(int width, int height, FreeRectChoiceHeuristic method, ref int score1, ref int score2)
        {
            Rect newNode = new Rect();
            score1 = int.MaxValue;
            score2 = int.MaxValue;
            switch (method)
            {
                case FreeRectChoiceHeuristic.RectBestShortSideFit:
                    newNode = FindPositionForNewNodeBestShortSideFit(width, height, ref score1, ref score2);
                    break;

                case FreeRectChoiceHeuristic.RectBottomLeftRule:
                    newNode = FindPositionForNewNodeBottomLeft(width, height, ref score1, ref score2);
                    break;

                case FreeRectChoiceHeuristic.RectContactPointRule:
                    newNode = FindPositionForNewNodeContactPoint(width, height, ref score1);
                    score1 = -score1; // Reverse since we are minimizing, but for contact point score bigger is better.
                    break;

                case FreeRectChoiceHeuristic.RectBestLongSideFit:
                    newNode = FindPositionForNewNodeBestLongSideFit(width, height, ref score2, ref score1);
                    break;

                case FreeRectChoiceHeuristic.RectBestAreaFit:
                    newNode = FindPositionForNewNodeBestAreaFit(width, height, ref score1, ref score2);
                    break;
            }

            // Cannot fit the current rectangle.
            if (newNode.Height == 0)
            {
                score1 = int.MaxValue;
                score2 = int.MaxValue;
            }

            return newNode;
        }

        /// Computes the ratio of used surface area.
        public float Occupancy()
        {
            ulong usedSurfaceArea = 0;
            for (int i = 0; i < UsedRectangles.Count; ++i)
                usedSurfaceArea += (uint)UsedRectangles[i].Width * (uint)UsedRectangles[i].Height;

            return (float)usedSurfaceArea / (BinWidth * BinHeight);
        }

        Rect FindPositionForNewNodeBottomLeft(int width, int height, ref int bestY, ref int bestX)
        {
            Rect bestNode = new Rect();

            bestY = int.MaxValue;

            for (int i = 0; i < FreeRectangles.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                Rect rect = FreeRectangles[i];
                if (rect.Width >= width && rect.Height >= height)
                {
                    int topSideY = rect.Y + height;
                    if (topSideY < bestY || (topSideY == bestY && rect.X < bestX))
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestY = topSideY;
                        bestX = rect.X;
                    }
                }
                if (AllowRotations && rect.Width >= height && rect.Height >= width)
                {
                    int topSideY = rect.Y + width;
                    if (topSideY < bestY || (topSideY == bestY && rect.X < bestX))
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestY = topSideY;
                        bestX = rect.X;
                    }
                }
            }
            return bestNode;
        }

        Rect FindPositionForNewNodeBestShortSideFit(int width, int height, ref int bestShortSideFit, ref int bestLongSideFit)
        {
            Rect bestNode = new Rect();

            bestShortSideFit = int.MaxValue;

            for (int i = 0; i < FreeRectangles.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                Rect rect = FreeRectangles[i];
                if (rect.Width >= width && rect.Height >= height)
                {
                    int leftoverHoriz = Math.Abs(rect.Width - width);
                    int leftoverVert = Math.Abs(rect.Height - height);
                    int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                    int longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                    if (shortSideFit < bestShortSideFit || (shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit))
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                    }
                }

                if (AllowRotations && rect.Width >= height && rect.Height >= width)
                {
                    int flippedLeftoverHoriz = Math.Abs(rect.Width - height);
                    int flippedLeftoverVert = Math.Abs(rect.Height - width);
                    int flippedShortSideFit = Math.Min(flippedLeftoverHoriz, flippedLeftoverVert);
                    int flippedLongSideFit = Math.Max(flippedLeftoverHoriz, flippedLeftoverVert);

                    if (flippedShortSideFit < bestShortSideFit || (flippedShortSideFit == bestShortSideFit && flippedLongSideFit < bestLongSideFit))
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestShortSideFit = flippedShortSideFit;
                        bestLongSideFit = flippedLongSideFit;
                    }
                }
            }
            return bestNode;
        }

        Rect FindPositionForNewNodeBestLongSideFit(int width, int height, ref int bestShortSideFit, ref int bestLongSideFit)
        {
            Rect bestNode = new Rect();

            bestLongSideFit = int.MaxValue;

            for (int i = 0; i < FreeRectangles.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                Rect rect = FreeRectangles[i];
                if (rect.Width >= width && rect.Height >= height)
                {
                    int leftoverHoriz = Math.Abs(rect.Width - width);
                    int leftoverVert = Math.Abs(rect.Height - height);
                    int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                    int longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                    if (longSideFit < bestLongSideFit || (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                    }
                }

                if (AllowRotations && rect.Width >= height && rect.Height >= width)
                {
                    int leftoverHoriz = Math.Abs(rect.Width - height);
                    int leftoverVert = Math.Abs(rect.Height - width);
                    int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                    int longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                    if (longSideFit < bestLongSideFit ||
                        (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                    }
                }
            }
            return bestNode;
        }

        Rect FindPositionForNewNodeBestAreaFit(int width, int height, ref int bestAreaFit, ref int bestShortSideFit)
        {
            Rect bestNode = new Rect();

            bestAreaFit = int.MaxValue;

            for (int i = 0; i < FreeRectangles.Count; ++i)
            {
                Rect rect = FreeRectangles[i];
                int areaFit = rect.Width * rect.Height - width * height;

                // Try to place the rectangle in upright (non-flipped) orientation.
                if (rect.Width >= width && rect.Height >= height)
                {
                    int leftoverHoriz = Math.Abs(rect.Width - width);
                    int leftoverVert = Math.Abs(rect.Height - height);
                    int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);

                    if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestShortSideFit = shortSideFit;
                        bestAreaFit = areaFit;
                    }
                }

                if (AllowRotations && rect.Width >= height && rect.Height >= width)
                {
                    int leftoverHoriz = Math.Abs(rect.Width - height);
                    int leftoverVert = Math.Abs(rect.Height - width);
                    int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);

                    if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestShortSideFit = shortSideFit;
                        bestAreaFit = areaFit;
                    }
                }
            }
            return bestNode;
        }

        /// Returns 0 if the two intervals i1 and i2 are disjoint, or the length of their overlap otherwise.
        int CommonIntervalLength(int i1start, int i1end, int i2start, int i2end)
        {
            if (i1end < i2start || i2end < i1start)
                return 0;
            return Math.Min(i1end, i2end) - Math.Max(i1start, i2start);
        }

        int ContactPointScoreNode(int x, int y, int width, int height)
        {
            int score = 0;

            if (x == 0 || x + width == BinWidth)
                score += height;
            if (y == 0 || y + height == BinHeight)
                score += width;

            for (int i = 0; i < UsedRectangles.Count; ++i)
            {
                if (UsedRectangles[i].X == x + width ||
                    UsedRectangles[i].X + UsedRectangles[i].Width == x)
                    score += CommonIntervalLength(
                        UsedRectangles[i].Y, UsedRectangles[i].Y + UsedRectangles[i].Height, y, y + height);

                if (UsedRectangles[i].Y == y + height ||
                    UsedRectangles[i].Y + UsedRectangles[i].Height == y)
                    score += CommonIntervalLength(
                        UsedRectangles[i].X, UsedRectangles[i].X + UsedRectangles[i].Width, x, x + width);
            }
            return score;
        }

        Rect FindPositionForNewNodeContactPoint(int width, int height, ref int bestContactScore)
        {
            Rect bestNode = new Rect();

            bestContactScore = -1;

            for (int i = 0; i < FreeRectangles.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                Rect rect = FreeRectangles[i];
                if (rect.Width >= width && rect.Height >= height)
                {
                    int score = ContactPointScoreNode(rect.X, rect.Y, width, height);
                    if (score > bestContactScore)
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestContactScore = score;
                    }
                }
                if (AllowRotations && rect.Width >= height && rect.Height >= width)
                {
                    int score = ContactPointScoreNode(rect.X, rect.Y, height, width);
                    if (score > bestContactScore)
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestContactScore = score;
                    }
                }
            }
            return bestNode;
        }

        bool SplitFreeNode(in Rect freeNode, ref Rect usedNode)
        {
            // Test with SAT if the rectangles even intersect.
            if (usedNode.X >= freeNode.X + freeNode.Width || usedNode.X + usedNode.Width <= freeNode.X ||
                usedNode.Y >= freeNode.Y + freeNode.Height || usedNode.Y + usedNode.Height <= freeNode.Y)
                return false;

            if (usedNode.X < freeNode.X + freeNode.Width && usedNode.X + usedNode.Width > freeNode.X)
            {
                // New node at the top side of the used node.
                if (usedNode.Y > freeNode.Y && usedNode.Y < freeNode.Y + freeNode.Height)
                {
                    Rect newNode = freeNode;
                    newNode.Height = usedNode.Y - newNode.Y;
                    FreeRectangles.Add(newNode);
                }

                // New node at the bottom side of the used node.
                if (usedNode.Y + usedNode.Height < freeNode.Y + freeNode.Height)
                {
                    Rect newNode = freeNode;
                    newNode.Y = usedNode.Y + usedNode.Height;
                    newNode.Height = freeNode.Y + freeNode.Height - (usedNode.Y + usedNode.Height);
                    FreeRectangles.Add(newNode);
                }
            }

            if (usedNode.Y < freeNode.Y + freeNode.Height && usedNode.Y + usedNode.Height > freeNode.Y)
            {
                // New node at the left side of the used node.
                if (usedNode.X > freeNode.X && usedNode.X < freeNode.X + freeNode.Width)
                {
                    Rect newNode = freeNode;
                    newNode.Width = usedNode.X - newNode.X;
                    FreeRectangles.Add(newNode);
                }

                // New node at the right side of the used node.
                if (usedNode.X + usedNode.Width < freeNode.X + freeNode.Width)
                {
                    Rect newNode = freeNode;
                    newNode.X = usedNode.X + usedNode.Width;
                    newNode.Width = freeNode.X + freeNode.Width - (usedNode.X + usedNode.Width);
                    FreeRectangles.Add(newNode);
                }
            }

            return true;
        }

        void PruneFreeList()
        {
            var array = FreeRectangles.InnerArray;
            int count = FreeRectangles.Count;
            for (int i = 0; i < count; ++i)
            {
                ref Rect ri = ref array[i];
                for (int j = i + 1; j < count; ++j)
                {
                    ref Rect rj = ref array[j];
                    if (IsContainedIn(ri, rj))
                    {
                        FreeRectangles.RemoveAt(i);
                        i--;
                        count--;
                        break;
                    }
                    if (IsContainedIn(rj, ri))
                    {
                        FreeRectangles.RemoveAt(j);
                        j--;
                        count--;
                    }
                }
            }
        }

        bool IsContainedIn(in Rect a, in Rect b)
        {
            return a.X >= b.X
                && a.Y >= b.Y
                && a.X + a.Width <= b.X + b.Width
                && a.Y + a.Height <= b.Y + b.Height;
        }
    }
}
