using Godot;
using System;
using System.Collections.Generic;

public static class InitialCurve
{
    public static Vector2[] Make(Vector2 startPoint, Vector2 endPoint, float mass, float arcLength, int segmentCount)
    {
        // Switch if start is on the right
        if (startPoint.X > endPoint.X)
        {
            var temp = startPoint;
            startPoint = endPoint;
            endPoint = temp;
        }

        Vector2 localEnd = endPoint - startPoint;
        float d = localEnd.X;
        float y1 = localEnd.Y;

        // Use a straight line if arcLength is nearly the distance
        float straightDistance = localEnd.Length();
        if (arcLength - straightDistance < 0.05f)
        {
            arcLength = straightDistance + .0005f;
        }

        // === Solve parabola coefficients ===
        float ArcLengthForA(float a)
        {
            float b = (y1 - a * d * d) / d;
            float Integrand(float x)
            {
                float dydx = 2 * a * x + b;
                return Mathf.Sqrt(1 + dydx * dydx);
            }

            int steps = 100;
            float sum = 0f;
            float dx = d / steps;
            for (int i = 0; i < steps; i++)
            {
                float x = i * dx;
                sum += Integrand(x) * dx;
            }

            return sum;
        }

        float aMin = -10f, aMax = 10f;
        float a = 0f;
        for (int i = 0; i < 100; i++)
        {
            float mid = (aMin + aMax) / 2f;
            float arc = ArcLengthForA(mid);

            if (Mathf.Abs(arc - arcLength) < 0.0001f)
            {
                a = mid;
                break;
            }

            if (arc > arcLength)
                aMax = mid;
            else
                aMin = mid;

            a = mid;
        }

        float bFinal = (y1 - a * d * d) / d;
        float cFinal = 0f;

        Vector2[] points = new Vector2[segmentCount + 1];
        for (int i = 0; i <= segmentCount; i++)
        {
            float t = (float)i / segmentCount;
            float x = t * d;
            float y = a * x * x + bFinal * x + cFinal;

            Vector2 localPoint = new Vector2(x, y);
            Vector2 worldPoint = startPoint + localPoint;
            points[i] = worldPoint;
        }

        return points;
    }
}
