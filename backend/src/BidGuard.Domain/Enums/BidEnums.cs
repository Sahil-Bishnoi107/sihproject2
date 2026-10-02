namespace BidGuard.Domain.Enums;

public enum BidStatus { Uploaded, Extracting, Verifying, Evaluating, UnderReview, Decided, Failed }

public enum BidDecision { Compliant, NonCompliant, NeedsClarification }
