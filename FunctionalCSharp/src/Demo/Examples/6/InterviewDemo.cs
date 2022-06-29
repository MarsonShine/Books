using MarsonShine.Functional;

namespace Demo.Examples._6
{
    using static F;
    public class InterviewDemo
    {
        private readonly Func<Candidate, bool> IsEligible;
        private readonly Func<Candidate, Either<Rejection, Candidate>> Interview;

        internal InterviewDemo(Func<Candidate, bool> isEligible, Func<Candidate, Either<Rejection, Candidate>> interview)
        {
            IsEligible = c => true;
            Interview = c => Right(c);
        }

        Either<Rejection, Candidate> CheckEligibility(Candidate c)
        {
            if (IsEligible(c)) return c;
            return new Rejection("Not eligible");
        }

        Either<Rejection, Candidate> FirstRound(Candidate c)
           => Right(c)
              .Bind(CheckEligibility)
              .Bind(Interview);
    }

    class Candidate { }
    class Rejection
    {
        private string reason;

        public Rejection(string reason)
        {
            this.reason = reason;
        }
    }
}
