using System;
using System.Diagnostics.CodeAnalysis;

namespace MomeryAllocation {
    public class Vector : IEquatable<Vector> {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public int Magnitude { get; set; }

        // override object.Equals
        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            return this.Equals((Vector) obj);
        }

        public bool Equals([AllowNull] Vector other) {
            return this.X == other.X &&
                this.Y == other.Y &&
                this.Z == other.Z &&
                this.Magnitude == other.Magnitude;
        }

        // override object.GetHashCode
        public override int GetHashCode() {
            return X ^ Y ^ Z ^ Magnitude;
        }
    }
}