/*
 * RVOMath.cs
 * RVO2 Library C#
 *
 * Copyright 2008 University of North Carolina at Chapel Hill
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Please send all bug reports to <geom@cs.unc.edu>.
 *
 * The authors may be contacted via:
 *
 * Jur van den Berg, Stephen J. Guy, Jamie Snape, Ming C. Lin, Dinesh Manocha
 * Dept. of Computer Science
 * 201 S. Columbia St.
 * Frederick P. Brooks, Jr. Computer Science Bldg.
 * Chapel Hill, N.C. 27599-3175
 * United States of America
 *
 * <http://gamma.cs.unc.edu/RVO2/>
 */


// This file contains general auxiliary structs and functions.

using System;
using System.Globalization;

namespace Orca
{
    public struct OrcaVector3
    {
        internal float x_;
        internal float y_;
        internal float z_;

        public OrcaVector3(float x, float y, float z)
        {
            x_ = x;
            y_ = y;
            z_ = z;
        }

        public override string ToString()
        {
            return "(" + x_.ToString(new CultureInfo("").NumberFormat) + "," + y_.ToString(new CultureInfo("").NumberFormat) + "," + z_.ToString(new CultureInfo("").NumberFormat) + ")";
        }

        public float x()
        {
            return x_;
        }

        public float y()
        {
            return y_;
        }

        public float z()
        {
            return z_;
        }

        public float this[int key]
        {
            get
            {
                if (key == 0) return x_;
                if (key == 1) return y_;
                return z_;
            }

            set
            {
                if (key == 0) x_ = value;
                if (key == 1) y_ = value;
                if (key == 2) z_ = value;
            }
        }

        public static OrcaVector3 cross(OrcaVector3 vector1, OrcaVector3 vector2)
        {
            return new OrcaVector3(vector1[1] * vector2[2] - vector1[2] * vector2[1], vector1[2] * vector2[0] - vector1[0] * vector2[2], vector1[0] * vector2[1] - vector1[1] * vector2[0]);
        }

        public static float operator *(OrcaVector3 vector1, OrcaVector3 vector2)
        {
            return vector1.x_ * vector2.x_ + vector1.y_ * vector2.y_ + vector1.z_ * vector2.z_;
        }

        public static OrcaVector3 operator *(float scalar, OrcaVector3 vector)
        {
            return vector * scalar;
        }

        public static OrcaVector3 operator *(OrcaVector3 vector, float scalar)
        {
            return new OrcaVector3(vector.x_ * scalar, vector.y_ * scalar, vector.z_ * scalar);
        }

        public static OrcaVector3 operator /(OrcaVector3 vector, float scalar)
        {
            return new OrcaVector3(vector.x_ / scalar, vector.y_ / scalar, vector.z_ / scalar);
        }

        public static OrcaVector3 operator +(OrcaVector3 vector1, OrcaVector3 vector2)
        {
            return new OrcaVector3(vector1.x_ + vector2.x_, vector1.y_ + vector2.y_, vector1.z_ + vector2.z_);
        }

        public static OrcaVector3 operator -(OrcaVector3 vector1, OrcaVector3 vector2)
        {
            return new OrcaVector3(vector1.x_ - vector2.x_, vector1.y_ - vector2.y_, vector1.z_ - vector2.z_);
        }

        public static OrcaVector3 operator -(OrcaVector3 vector)
        {
            return new OrcaVector3(-vector.x_, -vector.y_, -vector.z_);
        }

        public static implicit operator UnityEngine.Vector3(Orca.OrcaVector3 vec)
        {
            return new UnityEngine.Vector3(vec.x_, vec.y_, vec.z_);
        }
    }
    public struct Line
    {
        public OrcaVector3 direction;
        public OrcaVector3 point;
    }
    public struct Plane
    {
        public OrcaVector3 point;
        public OrcaVector3 normal;
    }

    /// <summary>
    ///  General math functions and conversions.
    /// </summary>
    public static class OrcaMath
    {
        public static bool ValidVector3(OrcaVector3 vector)
        {
            for (int i = 0; i < 3; i++)
            {
                float component = vector[i];
                if (float.IsNaN(component) || float.IsInfinity(component)) return false;
            }
            return true;
        }

        public static Orca.OrcaVector3 Unity2Rvo(UnityEngine.Vector3 vec)
        {
            return new Orca.OrcaVector3(vec.x, vec.y, vec.z);
        }

        internal const float RVO_EPSILON = 0.00001f;

        public static float abs(OrcaVector3 vector)
        {
            return sqrt(absSq(vector));
        }

        public static float absSq(OrcaVector3 vector)
        {
            return vector * vector;
        }

        public static OrcaVector3 normalize(OrcaVector3 vector)
        {
            return vector / abs(vector);
        }

        internal static float sqr(float scalar)
        {
            return scalar * scalar;
        }

        internal static float sqrt(float scalar)
        {
            return (float)Math.Sqrt(scalar);
        }
    }
}
