using Godot;
using System;

public static class InitialCurve
{
    public static Vector2[] Make(Vector2 startPoint, Vector2 endPoint, float mass, float arcLength, int segmentCount)
    {
        int n = segmentCount;
        float L = endPoint.X - startPoint.X;
        float h_diff = endPoint.Y - startPoint.Y;
        float gamma = mass / arcLength;
        float sw = gamma * 9.81f;

        double H = SolveForH(L, arcLength, sw);
        (double x0, double C) = SolveForX0AndC_Asym(H, L, h_diff, sw);
        Vector2[] relativePoints = GenerateAsymmetricCatenaryCurve(H, x0, C, L, sw, n);

        // Translate to startPoint
        for (int i = 0; i <= n; i++)
        {
            relativePoints[i] += startPoint;
        }

        return relativePoints;
    }

    private static double SolveForH(double L, double arcLength, float sw)
    {
        double tolerance = 0.5;
        for (double H = 10; H <= 20000; H += 1)
        {
            double L_approx = ComputeCableLength(H, L, sw);
            if (Math.Abs(L_approx - arcLength) < tolerance)
            {
                return H;
            }
        }

        GD.PrintErr("No valid H found within search range.");
        return 0;
    }

    private static double ComputeCableLength(double H, double L, float sw)
    {
        int segments = 1000;
        double[] x = new double[segments];
        double dx = L / (segments - 1);
        for (int i = 0; i < segments; i++)
            x[i] = -L / 2 + i * dx;

        double[] y = new double[segments];
        for (int i = 0; i < segments; i++)
            y[i] = (H / sw) * (Math.Cosh(sw / H * x[i]) - 1);

        double length = 0;
        for (int i = 0; i < segments - 1; i++)
        {
            double dy = y[i + 1] - y[i];
            length += Math.Sqrt(dx * dx + dy * dy);
        }
        return length;
    }

    private static (double, double) SolveForX0AndC_Asym(double H, float L, float h_diff, float sw)
    {
        double x0 = 0, C = 0;
        double tolerance = 1e-6;
        int maxIter = 100;

        for (int i = 0; i < maxIter; i++)
        {
            double eq1 = (H / sw) * Math.Cosh(-x0 * sw / H) + C;
            double eq2 = (H / sw) * Math.Cosh((L - x0) * sw / H) + C - h_diff;

            double d_eq1_x0 = -Math.Sinh(-x0 * sw / H);
            double d_eq2_x0 = -Math.Sinh((L - x0) * sw / H);

            double d_eq1_C = 1;
            double d_eq2_C = 1;

            double det = d_eq1_x0 * d_eq2_C - d_eq2_x0 * d_eq1_C;
            if (Math.Abs(det) < tolerance)
                break;

            double dx0 = (eq1 * d_eq2_C - eq2 * d_eq1_C) / det;
            double dC = (d_eq1_x0 * eq2 - d_eq2_x0 * eq1) / det;

            x0 -= dx0;
            C -= dC;

            if (Math.Abs(dx0) < tolerance && Math.Abs(dC) < tolerance)
                return (x0, C);
        }

        GD.PrintErr("Failed to solve for x0 and C.");
        return (0, 0);
    }

    private static Vector2[] GenerateAsymmetricCatenaryCurve(double H, double x0, double C, float L, float sw, int n)
    {
        Vector2[] points = new Vector2[n + 1];
        for (int i = 0; i <= n; i++)
        {
            double x = (L / n) * i;
            double y = (H / sw) * Math.Cosh((sw / H) * (x - x0)) + C;
            points[i] = new Vector2((float)x, (float)y);
        }
        return points;
    }
}
