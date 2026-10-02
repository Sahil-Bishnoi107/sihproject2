import { Compass } from "lucide-react";
import { Link } from "react-router-dom";
import { Card, EmptyState } from "../components/ui";

export function NotFoundPage() {
  return (
    <Card>
      <EmptyState icon={<Compass className="size-10" />} title="Page not found">
        It may have moved, or you may not have access to it.{" "}
        <Link to="/" className="font-medium text-saffron-600 hover:underline">
          Go home
        </Link>
      </EmptyState>
    </Card>
  );
}
