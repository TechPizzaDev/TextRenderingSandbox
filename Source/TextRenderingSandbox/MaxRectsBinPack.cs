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

        public readonly int Area => Width * Height;

        public Rect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public readonly override string ToString()
        {
            return $"X: {X}, Y: {Y}, W: {Width}, H: {Height}";
        }
    }

    public struct PackedRect
    {
        public Rect Rect;
        public bool IsRotated;

        public readonly override string ToString()
        {
            return Rect.ToString();
        }
    }

    public class MaxRectsBinPack
    {
        public enum FreeRectChoiceHeuristic
        {
            BestShortSideFit, ///< -BSSF: Positions the rectangle against the short side of a free rectangle into which it fits the best.
            BestLongSideFit, ///< -BLSF: Positions the rectangle against the long side of a free rectangle into which it fits the best.
            BestAreaFit, ///< -BAF: Positions the rectangle into the smallest free rect into which it fits.
            BottomLeftRule, ///< -BL: Does the Tetris placement.
            ContactPointRule ///< -CP: Choosest the placement where the rectangle touches other rects as much as possible.
        }

        public int BinWidth { get; private set; }
        public int BinHeight { get; private set; }
        public bool AllowRotations { get; private set; }

        public ListArray<PackedRect> UsedRectangles { get; } = new ListArray<PackedRect>();
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

        public PackedRect Insert(int width, int height, FreeRectChoiceHeuristic method)
        {
            int score1 = 0; // Unused here. We don't need to know the score after finding the position.
            int score2 = 0;
            var newNode = new PackedRect();

            switch (method)
            {
                case FreeRectChoiceHeuristic.BestShortSideFit:
                    newNode = FindPositionForNewNodeBestShortSideFit(
                        width, height, ref score1, ref score2);
                    break;

                case FreeRectChoiceHeuristic.BottomLeftRule:
                    newNode = FindPositionForNewNodeBottomLeft(
                        width, height, ref score1, ref score2);
                    break;

                case FreeRectChoiceHeuristic.ContactPointRule:
                    newNode = FindPositionForNewNodeContactPoint(
                        width, height, ref score1);
                    break;

                case FreeRectChoiceHeuristic.BestLongSideFit:
                    newNode = FindPositionForNewNodeBestLongSideFit(
                        width, height, ref score2, ref score1);
                    break;

                case FreeRectChoiceHeuristic.BestAreaFit:
                    newNode = FindPositionForNewNodeBestAreaFit(
                        width, height, ref score1, ref score2);
                    break;
            }

            if (newNode.Rect.Height == 0)
                return newNode;

            var array = FreeRectangles.InnerArray;
            for (int i = FreeRectangles.Count; i-- > 0;)
            {
                if (SplitFreeNode(array[i], newNode.Rect))
                    FreeRectangles.RemoveAt(i);
            }

            PruneFreeList();

            UsedRectangles.Add(newNode);
            return newNode;
        }

        public void Insert(List<Rect> rects, FreeRectChoiceHeuristic method)
        {
            while (rects.Count > 0)
            {
                int bestScore1 = int.MaxValue;
                int bestScore2 = int.MaxValue;
                int bestRectIndex = -1;
                PackedRect bestNode = default;

                for (int i = 0; i < rects.Count; ++i)
                {
                    int score1 = 0;
                    int score2 = 0;
                    PackedRect newNode = ScoreRect(
                        rects[i].Width, rects[i].Height, method, ref score1, ref score2);

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

        void PlaceRect(in PackedRect node)
        {
            int numRectanglesToProcess = FreeRectangles.Count;
            for (int i = 0; i < numRectanglesToProcess; ++i)
            {
                if (SplitFreeNode(FreeRectangles[i], node.Rect))
                {
                    FreeRectangles.RemoveAt(i);
                    --i;
                    --numRectanglesToProcess;
                }
            }

            PruneFreeList();

            UsedRectangles.Add(node);
        }

        PackedRect ScoreRect(
            int width, int height, FreeRectChoiceHeuristic method,
            ref int score1, ref int score2)
        {
            score1 = int.MaxValue;
            score2 = int.MaxValue;
            var newNode = new PackedRect();

            switch (method)
            {
                case FreeRectChoiceHeuristic.BestShortSideFit:
                    newNode = FindPositionForNewNodeBestShortSideFit(
                        width, height, ref score1, ref score2);
                    break;

                case FreeRectChoiceHeuristic.BottomLeftRule:
                    newNode = FindPositionForNewNodeBottomLeft(
                        width, height, ref score1, ref score2);
                    break;

                case FreeRectChoiceHeuristic.ContactPointRule:
                    newNode = FindPositionForNewNodeContactPoint(width, height, ref score1);

                    // Reverse since we are minimizing, but for contact point score bigger is better.
                    score1 = -score1;
                    break;

                case FreeRectChoiceHeuristic.BestLongSideFit:
                    newNode = FindPositionForNewNodeBestLongSideFit(
                        width, height, ref score2, ref score1);
                    break;

                case FreeRectChoiceHeuristic.BestAreaFit:
                    newNode = FindPositionForNewNodeBestAreaFit(
                        width, height, ref score1, ref score2);
                    break;
            }

            // Cannot fit the current rectangle.
            if (newNode.Rect.Height == 0)
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
            var array = UsedRectangles.InnerArray;
            for (int i = 0; i < UsedRectangles.Count; ++i)
            {
                var packed = array[i];
                usedSurfaceArea += (uint)packed.Rect.Width * (uint)packed.Rect.Height;
            }
            return (float)usedSurfaceArea / (BinWidth * BinHeight);
        }

        PackedRect FindPositionForNewNodeBottomLeft(
            int width, int height, ref int bestY, ref int bestX)
        {
            bestY = int.MaxValue;
            var bestNode = new PackedRect();

            var array = FreeRectangles.InnerArray;
            for (int i = 0; i < FreeRectangles.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                ref Rect rect = ref array[i];
                if (rect.Width >= width && rect.Height >= height)
                {
                    int topSideY = rect.Y + height;
                    if (topSideY < bestY || (topSideY == bestY && rect.X < bestX))
                    {
                        bestNode.Rect.X = rect.X;
                        bestNode.Rect.Y = rect.Y;
                        bestNode.Rect.Width = width;
                        bestNode.Rect.Height = height;
                        bestNode.IsRotated = false;
                        bestY = topSideY;
                        bestX = rect.X;
                    }
                }
                if (AllowRotations && rect.Width >= height && rect.Height >= width)
                {
                    int topSideY = rect.Y + width;
                    if (topSideY < bestY || (topSideY == bestY && rect.X < bestX))
                    {
                        bestNode.Rect.X = rect.X;
                        bestNode.Rect.Y = rect.Y;
                        bestNode.Rect.Width = height;
                        bestNode.Rect.Height = width;
                        bestNode.IsRotated = true;
                        bestY = topSideY;
                        bestX = rect.X;
                    }
                }
            }
            return bestNode;
        }

        PackedRect FindPositionForNewNodeBestShortSideFit(
            int width, int height, ref int bestShortSideFit, ref int bestLongSideFit)
        {
            bestShortSideFit = int.MaxValue;
            var bestNode = new PackedRect();

            var array = FreeRectangles.InnerArray;
            for (int i = 0; i < FreeRectangles.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                ref Rect rect = ref array[i];
                if (rect.Width >= width && rect.Height >= height)
                {
                    int leftoverHoriz = Math.Abs(rect.Width - width);
                    int leftoverVert = Math.Abs(rect.Height - height);
                    int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                    int longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                    if (shortSideFit < bestShortSideFit ||
                        (shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit))
                    {
                        bestNode.Rect.X = rect.X;
                        bestNode.Rect.Y = rect.Y;
                        bestNode.Rect.Width = width;
                        bestNode.Rect.Height = height;
                        bestNode.IsRotated = false;
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

                    if (flippedShortSideFit < bestShortSideFit ||
                        (flippedShortSideFit == bestShortSideFit && flippedLongSideFit < bestLongSideFit))
                    {
                        bestNode.Rect.X = rect.X;
                        bestNode.Rect.Y = rect.Y;
                        bestNode.Rect.Width = height;
                        bestNode.Rect.Height = width;
                        bestNode.IsRotated = true;
                        bestShortSideFit = flippedShortSideFit;
                        bestLongSideFit = flippedLongSideFit;
                    }
                }
            }
            return bestNode;
        }

        PackedRect FindPositionForNewNodeBestLongSideFit(
            int width, int height, ref int bestShortSideFit, ref int bestLongSideFit)
        {
            bestLongSideFit = int.MaxValue;
            var bestNode = new PackedRect();

            var array = FreeRectangles.InnerArray;
            for (int i = 0; i < FreeRectangles.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                ref Rect rect = ref array[i];
                if (rect.Width >= width && rect.Height >= height)
                {
                    int leftoverHoriz = Math.Abs(rect.Width - width);
                    int leftoverVert = Math.Abs(rect.Height - height);
                    int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                    int longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                    if (longSideFit < bestLongSideFit ||
                        (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.Rect.X = rect.X;
                        bestNode.Rect.Y = rect.Y;
                        bestNode.Rect.Width = width;
                        bestNode.Rect.Height = height;
                        bestNode.IsRotated = false;
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
                        bestNode.Rect.X = rect.X;
                        bestNode.Rect.Y = rect.Y;
                        bestNode.Rect.Width = height;
                        bestNode.Rect.Height = width;
                        bestNode.IsRotated = true;
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                    }
                }
            }
            return bestNode;
        }

        PackedRect FindPositionForNewNodeBestAreaFit(
            int width, int height, ref int bestAreaFit, ref int bestShortSideFit)
        {
            bestAreaFit = int.MaxValue;
            var bestNode = new PackedRect();

            var array = FreeRectangles.InnerArray;
            for (int i = 0; i < FreeRectangles.Count; ++i)
            {
                ref Rect rect = ref array[i];
                int areaFit = rect.Width * rect.Height - width * height;

                // Try to place the rectangle in upright (non-flipped) orientation.
                if (rect.Width >= width && rect.Height >= height)
                {
                    int leftoverHoriz = Math.Abs(rect.Width - width);
                    int leftoverVert = Math.Abs(rect.Height - height);
                    int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);

                    if (areaFit < bestAreaFit ||
                        (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.Rect.X = rect.X;
                        bestNode.Rect.Y = rect.Y;
                        bestNode.Rect.Width = width;
                        bestNode.Rect.Height = height;
                        bestNode.IsRotated = false;
                        bestShortSideFit = shortSideFit;
                        bestAreaFit = areaFit;
                    }
                }

                if (AllowRotations && rect.Width >= height && rect.Height >= width)
                {
                    int leftoverHoriz = Math.Abs(rect.Width - height);
                    int leftoverVert = Math.Abs(rect.Height - width);
                    int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);

                    if (areaFit < bestAreaFit ||
                        (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.Rect.X = rect.X;
                        bestNode.Rect.Y = rect.Y;
                        bestNode.Rect.Width = height;
                        bestNode.Rect.Height = width;
                        bestNode.IsRotated = true;
                        bestShortSideFit = shortSideFit;
                        bestAreaFit = areaFit;
                    }
                }
            }
            return bestNode;
        }

        /// <summary>
        /// Returns 0 if the two intervals i1 and i2 are disjoint,
        /// or the length of their overlap otherwise.
        /// </summary>
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

            var array = UsedRectangles.InnerArray;
            for (int i = 0; i < UsedRectangles.Count; ++i)
            {
                ref Rect rect = ref array[i].Rect;
                if (rect.X == x + width || rect.X + rect.Width == x)
                    score += CommonIntervalLength(rect.Y, rect.Y + rect.Height, y, y + height);

                if (rect.Y == y + height || rect.Y + rect.Height == y)
                    score += CommonIntervalLength(rect.X, rect.X + rect.Width, x, x + width);
            }
            return score;
        }

        PackedRect FindPositionForNewNodeContactPoint(
            int width, int height, ref int bestContactScore)
        {
            bestContactScore = -1;
            var bestNode = new PackedRect();

            for (int i = 0; i < FreeRectangles.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                ref Rect rect = ref FreeRectangles.InnerArray[i];
                if (rect.Width >= width && rect.Height >= height)
                {
                    int score = ContactPointScoreNode(rect.X, rect.Y, width, height);
                    if (score > bestContactScore)
                    {
                        bestNode.Rect.X = rect.X;
                        bestNode.Rect.Y = rect.Y;
                        bestNode.Rect.Width = width;
                        bestNode.Rect.Height = height;
                        bestNode.IsRotated = false;
                        bestContactScore = score;
                    }
                }
                if (AllowRotations && rect.Width >= height && rect.Height >= width)
                {
                    int score = ContactPointScoreNode(rect.X, rect.Y, height, width);
                    if (score > bestContactScore)
                    {
                        bestNode.Rect.X = rect.X;
                        bestNode.Rect.Y = rect.Y;
                        bestNode.Rect.Width = height;
                        bestNode.Rect.Height = width;
                        bestNode.IsRotated = true;
                        bestContactScore = score;
                    }
                }
            }
            return bestNode;
        }

        bool SplitFreeNode(in Rect freeNode, in Rect usedNode)
        {
            // Test with SAT if the rectangles even intersect.
            if (usedNode.X >= freeNode.X + freeNode.Width ||
                usedNode.X + usedNode.Width <= freeNode.X ||
                usedNode.Y >= freeNode.Y + freeNode.Height ||
                usedNode.Y + usedNode.Height <= freeNode.Y)
                return false;

            if (usedNode.X < freeNode.X + freeNode.Width &&
                usedNode.X + usedNode.Width > freeNode.X)
            {
                // New node at the top side of the used node.
                if (usedNode.Y > freeNode.Y &&
                    usedNode.Y < freeNode.Y + freeNode.Height)
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

            if (usedNode.Y < freeNode.Y + freeNode.Height &&
                usedNode.Y + usedNode.Height > freeNode.Y)
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
            for (int i = FreeRectangles.Count; i-- > 0;)
            {
                ref Rect ri = ref array[i];
                for (int j = FreeRectangles.Count; j-- > i + 1;)
                {
                    ref Rect rj = ref array[j];

                    if (IsContainedIn(ri, rj))
                    {
                        FreeRectangles.RemoveAt(i);
                        break;
                    }

                    if (IsContainedIn(rj, ri))
                        FreeRectangles.RemoveAt(j);
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
