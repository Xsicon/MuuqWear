import { Eye, CheckCircle, XCircle, Clock, DollarSign } from "lucide-react";
import { hapticTap } from "../../utils/haptics";

const applications = [
  { name: "Sarah K.", handle: "@sarahkstyle", followers: "12,400", date: "Mar 22, 2025", niche: "Streetwear Fashion" },
  { name: "Marcus C.", handle: "@marcuschen", followers: "45,200", date: "Mar 21, 2025", niche: "Menswear & Lifestyle" },
  { name: "Elena R.", handle: "@elenar.style", followers: "8,900", date: "Mar 20, 2025", niche: "Minimalist Fashion" },
  { name: "Priya D.", handle: "@priyadesigns", followers: "23,100", date: "Mar 19, 2025", niche: "Sustainable Fashion" },
];

const activeAffiliates = [
  { name: "Amara O.", tier: "Gold", items: 1234, earned: "$12,450", lastSale: "Mar 24, 2025", status: "Active" },
  { name: "David K.", tier: "Silver", items: 312, earned: "$3,240", lastSale: "Mar 22, 2025", status: "Active" },
  { name: "Fatima H.", tier: "Gold", items: 890, earned: "$9,870", lastSale: "Mar 23, 2025", status: "Active" },
  { name: "James P.", tier: "Bronze", items: 78, earned: "$890", lastSale: "Mar 20, 2025", status: "Active" },
  { name: "Lisa T.", tier: "Silver", items: 245, earned: "$2,180", lastSale: "Mar 21, 2025", status: "Inactive" },
];

const payouts = [
  { name: "Amara O.", amount: "$1,250", date: "Mar 24", status: "Pending" },
  { name: "David K.", amount: "$450", date: "Mar 23", status: "Pending" },
  { name: "Elena R.", amount: "$125", date: "Mar 22", status: "Pending" },
  { name: "Fatima H.", amount: "$890", date: "Mar 21", status: "Pending" },
];

interface Props {
  activeTab: string;
}

export function AffiliateProgram({ activeTab }: Props) {
  const renderPending = () => (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-semibold mb-1">Affiliate Program → Pending Applications (47)</h2>
        <p className="text-sm opacity-70">Review and approve affiliate applications</p>
      </div>

      <div className="flex gap-2 mb-6">
        {["All Niches", "All Tiers"].map((filter) => (
          <button
            key={filter}
            onClick={hapticTap}
            className="px-4 py-2 rounded-full text-sm transition-all hover:opacity-80"
            style={{ backgroundColor: "#E6ECF5", color: "#1E2A47" }}
          >
            {filter} ▼
          </button>
        ))}
      </div>

      <div className="space-y-3">
        {applications.map((app, idx) => (
          <div
            key={idx}
            className="p-4 rounded-lg border hover:shadow-md transition-shadow"
            style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}
          >
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <div className="flex items-center gap-3 mb-2">
                  <h3 className="font-semibold text-lg">{app.name}</h3>
                  <span className="opacity-70">{app.handle}</span>
                </div>
                <p className="text-sm opacity-70 mb-1">
                  Followers: {app.followers} · Niche: {app.niche} · Applied: {app.date}
                </p>
                <p className="text-sm text-blue-600">Content samples: [View Instagram] [View TikTok]</p>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg hover:opacity-80 transition-opacity flex items-center gap-2"
                  style={{ backgroundColor: "#2E7D32", color: "#FFFFFF" }}
                >
                  <CheckCircle size={16} />
                  Approve
                </button>
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg hover:opacity-80 transition-opacity flex items-center gap-2"
                  style={{ backgroundColor: "#C44545", color: "#FFFFFF" }}
                >
                  <XCircle size={16} />
                  Deny
                </button>
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg border hover:bg-gray-50 transition-colors"
                  style={{ borderColor: "#E5E7EB" }}
                >
                  Waitlist
                </button>
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg border hover:bg-gray-50 transition-colors flex items-center gap-2"
                  style={{ borderColor: "#E5E7EB" }}
                >
                  <Eye size={16} />
                  View Full Application
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );

  const renderActive = () => (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-semibold mb-1">Active Affiliates (453/500)</h2>
        <p className="text-sm opacity-70">Manage active affiliate accounts</p>
      </div>

      <div className="space-y-3">
        {activeAffiliates.map((aff, idx) => (
          <div
            key={idx}
            className="p-4 rounded-lg border hover:shadow-md transition-shadow"
            style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}
          >
            <div className="flex items-center justify-between">
              <div className="flex-1">
                <div className="flex items-center gap-3 mb-1">
                  <h3 className="font-semibold">{aff.name}</h3>
                  <span
                    className="px-3 py-1 rounded-full text-xs font-medium"
                    style={{
                      backgroundColor: aff.tier === "Gold" ? "#FEF3C7" : aff.tier === "Silver" ? "#E5E7EB" : "#FEE2E2",
                      color: aff.tier === "Gold" ? "#92400E" : aff.tier === "Silver" ? "#374151" : "#991B1B",
                    }}
                  >
                    {aff.tier}
                  </span>
                </div>
                <p className="text-sm opacity-70">
                  Items Sold: {aff.items} · Earned: {aff.earned} · Last Sale: {aff.lastSale}
                </p>
              </div>
              <button
                onClick={hapticTap}
                className="px-4 py-2 rounded-lg border hover:bg-gray-50 transition-colors"
                style={{ borderColor: "#E5E7EB" }}
              >
                View Details
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );

  const renderPayouts = () => (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-semibold mb-1">Payouts (4)</h2>
        <p className="text-sm opacity-70">Process affiliate commission payouts</p>
      </div>

      <div className="space-y-3">
        {payouts.map((payout, idx) => (
          <div
            key={idx}
            className="p-4 rounded-lg border hover:shadow-md transition-shadow"
            style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}
          >
            <div className="flex items-center justify-between">
              <div className="flex-1">
                <h3 className="font-semibold mb-1">{payout.name}</h3>
                <p className="text-sm opacity-70">Amount: {payout.amount} · Date: {payout.date}</p>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg hover:opacity-80 transition-opacity flex items-center gap-2"
                  style={{ backgroundColor: "#2E7D32", color: "#FFFFFF" }}
                >
                  <DollarSign size={16} />
                  Process Payout
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );

  if (activeTab === "active") return renderActive();
  if (activeTab === "payouts") return renderPayouts();
  if (activeTab === "tiers") {
    return (
      <div>
        <h2 className="text-2xl font-semibold mb-1">Tier Settings</h2>
        <p className="text-sm opacity-70">Configure affiliate tier thresholds and commission rates</p>
      </div>
    );
  }
  return renderPending();
}
