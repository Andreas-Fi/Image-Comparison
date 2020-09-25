using System;

namespace CompareFunctions
{
    public class Match : IEquatable<Match>, IComparable<Match>
    {
        public string FileName1 { get; set; }
        public string FileName2 { get; set; }
        public double EqualElements { get; set; }
        public bool MarkForDeletion { get; set; }

        public Match()
        {
            MarkForDeletion = false;
        }

        public int CompareTo(Match compareMatch)
        {
            if (compareMatch == null)
                return 1;

            else
                return this.EqualElements.CompareTo(compareMatch.EqualElements);
        }
        public bool Equals(Match other)
        {
            if (other == null)
                return false;
            return (this.EqualElements.Equals(other.EqualElements));
        }
    }
}