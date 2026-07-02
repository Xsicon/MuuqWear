import { Plus, Edit, Eye, Trash2 } from "lucide-react";
import { hapticTap } from "../../utils/haptics";

const articles = [
  { title: "The Art of the Blue Veil: Inside Muuqsimo 2024", status: "Published", date: "Mar 15, 2025", views: 2345 },
  { title: "The Sapphire Prophecy Collection Reveal", status: "Draft", date: "Mar 24, 2025", views: 0 },
  { title: "Material Science: Behind AeroWeave Technology", status: "Published", date: "Mar 10, 2025", views: 1892 },
  { title: "Heritage Craft: The Nordic Rune Sweater Story", status: "Published", date: "Feb 28, 2025", views: 3210 },
];

interface Props {
  activeTab: string;
}

export function Content({ activeTab }: Props) {
  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold mb-1">Content Management</h2>
          <p className="text-sm opacity-70">Edit website content and articles</p>
        </div>
        <button
          onClick={hapticTap}
          className="px-4 py-2 rounded-lg flex items-center gap-2 hover:opacity-80 transition-opacity"
          style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}
        >
          <Plus size={16} />
          CREATE NEW
        </button>
      </div>

      <div className="flex gap-4 mb-6 border-b" style={{ borderColor: "#E5E7EB" }}>
        {["JOURNAL ARTICLES", "EVENTS", "DESIGN HISTORY"].map((tab) => (
          <button
            key={tab}
            onClick={hapticTap}
            className="px-4 py-3 border-b-2 transition-colors"
            style={{
              borderColor: tab === "JOURNAL ARTICLES" ? "#1E2A47" : "transparent",
              color: tab === "JOURNAL ARTICLES" ? "#1E2A47" : "#6B7280",
            }}
          >
            {tab}
          </button>
        ))}
      </div>

      <div className="space-y-3">
        {articles.map((article, idx) => (
          <div
            key={idx}
            className="p-4 rounded-lg border hover:shadow-md transition-shadow"
            style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}
          >
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <div className="flex items-center gap-3 mb-2">
                  <h3 className="font-semibold">{article.title}</h3>
                  <span
                    className="px-3 py-1 rounded-full text-xs font-medium"
                    style={{
                      backgroundColor: article.status === "Published" ? "#D1FAE5" : "#E5E7EB",
                      color: article.status === "Published" ? "#065F46" : "#374151",
                    }}
                  >
                    {article.status}
                  </span>
                </div>
                <p className="text-sm opacity-70">
                  {article.date} · {article.views.toLocaleString()} views
                </p>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={hapticTap}
                  className="p-2 rounded-lg border hover:bg-gray-50 transition-colors"
                  style={{ borderColor: "#E5E7EB" }}
                >
                  <Edit size={16} />
                </button>
                <button
                  onClick={hapticTap}
                  className="p-2 rounded-lg border hover:bg-gray-50 transition-colors"
                  style={{ borderColor: "#E5E7EB" }}
                >
                  <Eye size={16} />
                </button>
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg hover:opacity-80 transition-opacity"
                  style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}
                >
                  {article.status === "Published" ? "Unpublish" : "Publish"}
                </button>
                <button
                  onClick={hapticTap}
                  className="p-2 rounded-lg border hover:bg-red-50 transition-colors"
                  style={{ borderColor: "#E5E7EB", color: "#C44545" }}
                >
                  <Trash2 size={16} />
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
