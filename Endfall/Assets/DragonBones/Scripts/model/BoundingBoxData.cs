using System;
using System.Collections.Generic;

namespace DragonBones
{

    public abstract class BoundingBoxData : BaseObject
    {

        public BoundingBoxType type;
        public uint color;
        public float width;
        public float height;

        protected override void _OnClear()
        {
            this.color = 0x000000;
            this.width = 0.0f;
            this.height = 0.0f;
        }

        public abstract bool ContainsPoint(float pX, float pY);


        public abstract int IntersectsSegment(float xA, float yA, float xB, float yB,
                                                Point intersectionPointA = null,
                                                Point intersectionPointB = null,
                                                Point normalRadians = null);
    }

    enum OutCode
    {
        InSide = 0,
        Left = 1,
        Right = 2,
        Top = 4,
        Bottom = 8
    }


    public class RectangleBoundingBoxData : BoundingBoxData
    {
        private static int _ComputeOutCode(float x, float y, float xMin, float yMin, float xMax, float yMax)
        {
            var code = OutCode.InSide;

            if (x < xMin)
            {
                code |= OutCode.Left;
            }
            else if (x > xMax)
            {
                code |= OutCode.Right;
            }

            if (y < yMin)
            {
                code |= OutCode.Top;
            }
            else if (y > yMax)
            {
                code |= OutCode.Bottom;
            }

            return (int)code;
        }
        public static int RectangleIntersectsSegment(float xA, float yA, float xB, float yB,
                                                        float xMin, float yMin, float xMax, float yMax,
                                                        Point intersectionPointA = null,
                                                        Point intersectionPointB = null,
                                                        Point normalRadians = null)
        {
            var inSideA = xA > xMin && xA < xMax && yA > yMin && yA < yMax;
            var inSideB = xB > xMin && xB < xMax && yB > yMin && yB < yMax;

            if (inSideA && inSideB)
            {
                return -1;
            }

            var intersectionCount = 0;
            var outcode0 = RectangleBoundingBoxData._ComputeOutCode(xA, yA, xMin, yMin, xMax, yMax);
            var outcode1 = RectangleBoundingBoxData._ComputeOutCode(xB, yB, xMin, yMin, xMax, yMax);

            while (true)
            {
                if ((outcode0 | outcode1) == 0)
                {
                    intersectionCount = 2;
                    break;
                }
                else if ((outcode0 & outcode1) != 0)
                {
                    break;
                }

                var x = 0.0f;
                var y = 0.0f;
                var normalRadian = 0.0f;

                var outcodeOut = outcode0 != 0 ? outcode0 : outcode1;

                if ((outcodeOut & (int)OutCode.Top) != 0)
                {
                    x = xA + (xB - xA) * (yMin - yA) / (yB - yA);
                    y = yMin;

                    if (normalRadians != null)
                    {
                        normalRadian = -(float)Math.PI * 0.5f;
                    }
                }
                else if ((outcodeOut & (int)OutCode.Bottom) != 0)
                {
                    x = xA + (xB - xA) * (yMax - yA) / (yB - yA);
                    y = yMax;

                    if (normalRadians != null)
                    {
                        normalRadian = (float)Math.PI * 0.5f;
                    }
                }
                else if ((outcodeOut & (int)OutCode.Right) != 0)
                {
                    y = yA + (yB - yA) * (xMax - xA) / (xB - xA);
                    x = xMax;

                    if (normalRadians != null)
                    {
                        normalRadian = 0;
                    }
                }
                else if ((outcodeOut & (int)OutCode.Left) != 0)
                {
                    y = yA + (yB - yA) * (xMin - xA) / (xB - xA);
                    x = xMin;

                    if (normalRadians != null)
                    {
                        normalRadian = (float)Math.PI;
                    }
                }

                if (outcodeOut == outcode0)
                {
                    xA = x;
                    yA = y;
                    outcode0 = RectangleBoundingBoxData._ComputeOutCode(xA, yA, xMin, yMin, xMax, yMax);

                    if (normalRadians != null)
                    {
                        normalRadians.x = normalRadian;
                    }
                }
                else
                {
                    xB = x;
                    yB = y;
                    outcode1 = RectangleBoundingBoxData._ComputeOutCode(xB, yB, xMin, yMin, xMax, yMax);

                    if (normalRadians != null)
                    {
                        normalRadians.y = normalRadian;
                    }
                }
            }

            if (intersectionCount > 0)
            {
                if (inSideA)
                {
                    intersectionCount = 2;

                    if (intersectionPointA != null)
                    {
                        intersectionPointA.x = xB;
                        intersectionPointA.y = yB;
                    }

                    if (intersectionPointB != null)
                    {
                        intersectionPointB.x = xB;
                        intersectionPointB.y = xB;
                    }

                    if (normalRadians != null)
                    {
                        normalRadians.x = normalRadians.y + (float)Math.PI;
                    }
                }
                else if (inSideB)
                {
                    intersectionCount = 1;

                    if (intersectionPointA != null)
                    {
                        intersectionPointA.x = xA;
                        intersectionPointA.y = yA;
                    }

                    if (intersectionPointB != null)
                    {
                        intersectionPointB.x = xA;
                        intersectionPointB.y = yA;
                    }

                    if (normalRadians != null)
                    {
                        normalRadians.y = normalRadians.x + (float)Math.PI;
                    }
                }
                else
                {
                    intersectionCount = 3;
                    if (intersectionPointA != null)
                    {
                        intersectionPointA.x = xA;
                        intersectionPointA.y = yA;
                    }

                    if (intersectionPointB != null)
                    {
                        intersectionPointB.x = xB;
                        intersectionPointB.y = yB;
                    }
                }
            }

            return intersectionCount;
        }
        protected override void _OnClear()
        {
            base._OnClear();

            this.type = BoundingBoxType.Rectangle;
        }

        public override bool ContainsPoint(float pX, float pY)
        {
            var widthH = this.width * 0.5f;
            if (pX >= -widthH && pX <= widthH)
            {
                var heightH = this.height * 0.5f;
                if (pY >= -heightH && pY <= heightH)
                {
                    return true;
                }
            }

            return false;
        }

        public override int IntersectsSegment(float xA, float yA, float xB, float yB,
                                             Point intersectionPointA = null,
                                             Point intersectionPointB = null,
                                             Point normalRadians = null)
        {
            var widthH = this.width * 0.5f;
            var heightH = this.height * 0.5f;
            var intersectionCount = RectangleBoundingBoxData.RectangleIntersectsSegment
            (
                xA, yA, xB, yB,
                -widthH, -heightH, widthH, heightH,
                intersectionPointA, intersectionPointB, normalRadians
            );

            return intersectionCount;
        }
    }


    public class EllipseBoundingBoxData : BoundingBoxData
    {
        public static int EllipseIntersectsSegment(float xA, float yA, float xB, float yB,
                                                    float xC, float yC, float widthH, float heightH,
                                                    Point intersectionPointA = null,
                                                    Point intersectionPointB = null,
                                                    Point normalRadians = null)
        {
            var d = widthH / heightH;
            var dd = d * d;

            yA *= d;
            yB *= d;

            var dX = xB - xA;
            var dY = yB - yA;
            var lAB = (float)Math.Sqrt(dX * dX + dY * dY);
            var xD = dX / lAB;
            var yD = dY / lAB;
            var a = (xC - xA) * xD + (yC - yA) * yD;
            var aa = a * a;
            var ee = xA * xA + yA * yA;
            var rr = widthH * widthH;
            var dR = rr - ee + aa;
            var intersectionCount = 0;

            if (dR >= 0.0f)
            {
                var dT = (float)Math.Sqrt(dR);
                var sA = a - dT;
                var sB = a + dT;
                var inSideA = sA < 0.0 ? -1 : (sA <= lAB ? 0 : 1);
                var inSideB = sB < 0.0 ? -1 : (sB <= lAB ? 0 : 1);
                var sideAB = inSideA * inSideB;

                if (sideAB < 0)
                {
                    return -1;
                }
                else if (sideAB == 0)
                {
                    if (inSideA == -1)
                    {
                        intersectionCount = 2;
                        xB = xA + sB * xD;
                        yB = (yA + sB * yD) / d;

                        if (intersectionPointA != null)
                        {
                            intersectionPointA.x = xB;
                            intersectionPointA.y = yB;
                        }

                        if (intersectionPointB != null)
                        {
                            intersectionPointB.x = xB;
                            intersectionPointB.y = yB;
                        }

                        if (normalRadians != null)
                        {
                            normalRadians.x = (float)Math.Atan2(yB / rr * dd, xB / rr);
                            normalRadians.y = normalRadians.x + (float)Math.PI;
                        }
                    }
                    else if (inSideB == 1)
                    {
                        intersectionCount = 1;
                        xA = xA + sA * xD;
                        yA = (yA + sA * yD) / d;

                        if (intersectionPointA != null)
                        {
                            intersectionPointA.x = xA;
                            intersectionPointA.y = yA;
                        }

                        if (intersectionPointB != null)
                        {
                            intersectionPointB.x = xA;
                            intersectionPointB.y = yA;
                        }

                        if (normalRadians != null)
                        {
                            normalRadians.x = (float)Math.Atan2(yA / rr * dd, xA / rr);
                            normalRadians.y = normalRadians.x + (float)Math.PI;
                        }
                    }
                    else
                    {
                        intersectionCount = 3;

                        if (intersectionPointA != null)
                        {
                            intersectionPointA.x = xA + sA * xD;
                            intersectionPointA.y = (yA + sA * yD) / d;

                            if (normalRadians != null)
                            {
                                normalRadians.x = (float)Math.Atan2(intersectionPointA.y / rr * dd, intersectionPointA.x / rr);
                            }
                        }

                        if (intersectionPointB != null)
                        {
                            intersectionPointB.x = xA + sB * xD;
                            intersectionPointB.y = (yA + sB * yD) / d;

                            if (normalRadians != null)
                            {
                                normalRadians.y = (float)Math.Atan2(intersectionPointB.y / rr * dd, intersectionPointB.x / rr);
                            }
                        }
                    }
                }
            }

            return intersectionCount;
        }
        protected override void _OnClear()
        {
            base._OnClear();

            this.type = BoundingBoxType.Ellipse;
        }

        public override bool ContainsPoint(float pX, float pY)
        {
            var widthH = this.width * 0.5f;
            if (pX >= -widthH && pX <= widthH)
            {
                var heightH = this.height * 0.5f;
                if (pY >= -heightH && pY <= heightH)
                {
                    pY *= widthH / heightH;
                    return Math.Sqrt(pX * pX + pY * pY) <= widthH;
                }
            }

            return false;
        }

        public override int IntersectsSegment(float xA, float yA, float xB, float yB,
                                                Point intersectionPointA,
                                                Point intersectionPointB,
                                                Point normalRadians)
        {
            var intersectionCount = EllipseBoundingBoxData.EllipseIntersectsSegment(xA, yA, xB, yB,
                                                                                    0.0f, 0.0f, this.width * 0.5f, this.height * 0.5f,
                                                                                    intersectionPointA, intersectionPointB, normalRadians);

            return intersectionCount;
        }
    }


    public class PolygonBoundingBoxData : BoundingBoxData
    {
        public static int PolygonIntersectsSegment(float xA, float yA, float xB, float yB,
                                                    List<float> vertices,
                                                    Point intersectionPointA = null,
                                                    Point intersectionPointB = null,
                                                    Point normalRadians = null)
        {
            if (xA == xB)
            {
                xA = xB + 0.01f;
            }

            if (yA == yB)
            {
                yA = yB + 0.01f;
            }

            var l = vertices.Count;
            var dXAB = xA - xB;
            var dYAB = yA - yB;
            var llAB = xA * yB - yA * xB;
            int intersectionCount = 0;
            var xC = vertices[l - 2];
            var yC = vertices[l - 1];
            var dMin = 0.0f;
            var dMax = 0.0f;
            var xMin = 0.0f;
            var yMin = 0.0f;
            var xMax = 0.0f;
            var yMax = 0.0f;

            for (int i = 0; i < l; i += 2)
            {
                var xD = vertices[i];
                var yD = vertices[i + 1];

                if (xC == xD)
                {
                    xC = xD + 0.01f;
                }

                if (yC == yD)
                {
                    yC = yD + 0.01f;
                }

                var dXCD = xC - xD;
                var dYCD = yC - yD;
                var llCD = xC * yD - yC * xD;
                var ll = dXAB * dYCD - dYAB * dXCD;
                var x = (llAB * dXCD - dXAB * llCD) / ll;

                if (((x >= xC && x <= xD) || (x >= xD && x <= xC)) && (dXAB == 0 || (x >= xA && x <= xB) || (x >= xB && x <= xA)))
                {
                    var y = (llAB * dYCD - dYAB * llCD) / ll;
                    if (((y >= yC && y <= yD) || (y >= yD && y <= yC)) && (dYAB == 0 || (y >= yA && y <= yB) || (y >= yB && y <= yA)))
                    {
                        if (intersectionPointB != null)
                        {
                            var d = x - xA;
                            if (d < 0.0f)
                            {
                                d = -d;
                            }

                            if (intersectionCount == 0)
                            {
                                dMin = d;
                                dMax = d;
                                xMin = x;
                                yMin = y;
                                xMax = x;
                                yMax = y;

                                if (normalRadians != null)
                                {
                                    normalRadians.x = (float)Math.Atan2(yD - yC, xD - xC) - (float)Math.PI * 0.5f;
                                    normalRadians.y = normalRadians.x;
                                }
                            }
                            else
                            {
                                if (d < dMin)
                                {
                                    dMin = d;
                                    xMin = x;
                                    yMin = y;

                                    if (normalRadians != null)
                                    {
                                        normalRadians.x = (float)Math.Atan2(yD - yC, xD - xC) - (float)Math.PI * 0.5f;
                                    }
                                }

                                if (d > dMax)
                                {
                                    dMax = d;
                                    xMax = x;
                                    yMax = y;

                                    if (normalRadians != null)
                                    {
                                        normalRadians.y = (float)Math.Atan2(yD - yC, xD - xC) - (float)Math.PI * 0.5f;
                                    }
                                }
                            }

                            intersectionCount++;
                        }
                        else
                        {
                            xMin = x;
                            yMin = y;
                            xMax = x;
                            yMax = y;
                            intersectionCount++;

                            if (normalRadians != null)
                            {
                                normalRadians.x = (float)Math.Atan2(yD - yC, xD - xC) - (float)Math.PI * 0.5f;
                                normalRadians.y = normalRadians.x;
                            }
                            break;
                        }
                    }
                }

                xC = xD;
                yC = yD;
            }

            if (intersectionCount == 1)
            {
                if (intersectionPointA != null)
                {
                    intersectionPointA.x = xMin;
                    intersectionPointA.y = yMin;
                }

                if (intersectionPointB != null)
                {
                    intersectionPointB.x = xMin;
                    intersectionPointB.y = yMin;
                }

                if (normalRadians != null)
                {
                    normalRadians.y = normalRadians.x + (float)Math.PI;
                }
            }
            else if (intersectionCount > 1)
            {
                intersectionCount++;

                if (intersectionPointA != null)
                {
                    intersectionPointA.x = xMin;
                    intersectionPointA.y = yMin;
                }

                if (intersectionPointB != null)
                {
                    intersectionPointB.x = xMax;
                    intersectionPointB.y = yMax;
                }
            }

            return intersectionCount;
        }

        public float x;
        public float y;

        public readonly List<float> vertices = new List<float>();

        protected override void _OnClear()
        {
            base._OnClear();

            this.type = BoundingBoxType.Polygon;
            this.x = 0.0f;
            this.y = 0.0f;
            this.vertices.Clear();
        }

        public override bool ContainsPoint(float pX, float pY)
        {
            var isInSide = false;
            if (pX >= this.x && pX <= this.width && pY >= this.y && pY <= this.height)
            {
                for (int i = 0, l = this.vertices.Count, iP = l - 2; i < l; i += 2)
                {
                    var yA = this.vertices[iP + 1];
                    var yB = this.vertices[i + 1];
                    if ((yB < pY && yA >= pY) || (yA < pY && yB >= pY))
                    {
                        var xA = this.vertices[iP];
                        var xB = this.vertices[i];
                        if ((pY - yB) * (xA - xB) / (yA - yB) + xB < pX)
                        {
                            isInSide = !isInSide;
                        }
                    }

                    iP = i;
                }
            }

            return isInSide;
        }

        public override int IntersectsSegment(float xA, float yA, float xB, float yB,
                                                Point intersectionPointA = null,
                                                Point intersectionPointB = null,
                                                Point normalRadians = null)
        {
            var intersectionCount = 0;
            if (RectangleBoundingBoxData.RectangleIntersectsSegment(xA, yA, xB, yB, this.x, this.y, this.x + this.width, this.y + this.height, null, null, null) != 0)
            {
                intersectionCount = PolygonBoundingBoxData.PolygonIntersectsSegment
                                                            (
                                                             xA, yA, xB, yB,
                                                             this.vertices,
                                                             intersectionPointA, intersectionPointB, normalRadians
                                                            );
            }

            return intersectionCount;
        }
    }
}