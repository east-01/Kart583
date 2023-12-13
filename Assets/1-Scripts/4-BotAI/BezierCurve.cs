using UnityEngine;

public class BezierCurve
{

    private Vector3 start, cp1, cp2, end;
    public float Length { get; private set; }

    public BezierCurve(Vector3 start, Vector3 cp1, Vector3 cp2, Vector3 end) 
    {
        this.start = start;
        this.cp1 = cp1;
        this.cp2 = cp2;
        this.end = end;
        this.Length = 0;

        int intervals = (int)Vector3.Distance(start, end);
        float intervalWidth = 1.0f / intervals;

        for (int i = 0; i < intervals; i++) {
            float t1 = i * intervalWidth;
            float t2 = (i + 1) * intervalWidth;

            Vector3 p1 = CalculateBezierPoint(t1);
            Vector3 p2 = CalculateBezierPoint(t2);

            Length += Vector3.Distance(p1, p2);
        }
    }

    public Vector3 CalculateBezierPoint(float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * start; // (1-t)^3 * start
        p += 3 * uu * t * cp1; // 3 * (1-t)^2 * t * cp1
        p += 3 * u * tt * cp2; // 3 * (1-t) * t^2 * cp2
        p += ttt * end; // t^3 * end

        return p;
    }

    public Vector3 CalculateBezierDerivative(float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 derivative = 3 * uu * (cp1 - start);
        derivative += 6 * u * t * (cp2 - cp1);
        derivative += 3 * tt * (end - cp2);

        return derivative;
    }

    public Vector3 CalculateBezierSecondDerivative(float t)
    {
        float u = 1 - t;

        Vector3 secondDerivative = 6 * u * (cp2 - 2 * cp1 + start);
        secondDerivative += 6 * t * (end - 2 * cp2 + cp1);

        return secondDerivative;
    }

    public Vector3 CalculateTangent(float t)
    {
        float u = 1 - t;
        float uu = u * u;
        float tt = t * t;

        Vector3 tangent =
            3 * uu * (cp1 - start) +
            6 * u * t * (cp2 - cp1) +
            3 * tt * (end - cp2);

        return tangent.normalized;
    }

    /** Estimate the closest point on the curve, the t value (or progress value) that gives us
      *   the closest point will be returned. */
    public float ClosestEstimate(Vector3 point, int iterations, float initialEstimate)
    {
        float t = initialEstimate;
        float epsilon = 0.001f; // Desired precision

        for (int i = 0; i < iterations; i++)
        {
            // Calculate the point on the Bezier curve for the current t
            Vector3 currentPoint = CalculateBezierPoint(t);

            // Calculate the derivative of the distance function
            Vector3 derivative = CalculateBezierDerivative(t);

            // Update the parameter using the Newton-Raphson method
            t -= Vector3.Dot(currentPoint - point, derivative) / derivative.sqrMagnitude;

            // Clamp t to the valid range [0, 1]
            t = Mathf.Clamp01(t);

            // Check if the change in t is smaller than the desired precision
            if (Vector3.Distance(currentPoint, point) < epsilon)
            {
                break;
            }
        }

        // Return the final point on the Bezier curve
        return t;
    }        

}