import type { NotificationDto, UserRole } from "../api/types";

/** Where a notification should take the user when clicked. */
export function notificationLink(n: NotificationDto, role: UserRole): string | null {
  if (!n.referenceId) return null;
  switch (n.type) {
    case "RulesReady":
      return `/tenders/${n.referenceId}`;
    case "BidEvaluated":
    case "BidDecided":
      return `/bids/${n.referenceId}`;
    case "CredentialExpiring":
      return "/organization";
    case "ProcessingFailed":
      // Officers are told about tender extraction failures, bidders about their bids.
      return role === "Bidder" ? `/bids/${n.referenceId}` : `/tenders/${n.referenceId}`;
  }
}
