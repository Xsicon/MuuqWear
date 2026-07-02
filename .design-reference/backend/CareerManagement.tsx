import { Plus, Eye, Edit, X, CheckCircle, XCircle } from "lucide-react";
import { hapticTap } from "../../utils/haptics";

const jobPostings = [
  { title: "Senior Product Designer", department: "Design", location: "New York (Hybrid)", status: "Open", applications: 12 },
  { title: "Customer Support Specialist", department: "Support", location: "Remote", status: "Open", applications: 8 },
  { title: "Frontend Engineer", department: "Engineering", location: "San Francisco", status: "Open", applications: 24 },
];

const applications = [
  { status: "New", name: "Michael Brown", position: "Senior Product Designer", date: "Mar 24, 2025", portfolio: "michaelbrown.design", experience: "6 years" },
  { status: "Reviewed", name: "Jessica Lee", position: "Support Specialist", date: "Mar 23, 2025", portfolio: "", experience: "3 years at Zendesk" },
  { status: "Interview", name: "Alex Chen", position: "Frontend Engineer", date: "Mar 22, 2025", portfolio: "alexchen.dev", experience: "5 years" },
];

interface Props {
  activeTab: string;
}

export function CareerManagement({ activeTab }: Props) {
  const renderJobPostings = () => (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold mb-1">Career Management → Job Postings</h2>
          <p className="text-sm opacity-70">Create and manage job postings</p>
        </div>
        <button
          onClick={hapticTap}
          className="px-4 py-2 rounded-lg flex items-center gap-2 hover:opacity-80 transition-opacity"
          style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}
        >
          <Plus size={16} />
          NEW POSTING
        </button>
      </div>

      <div className="space-y-3">
        {jobPostings.map((job, idx) => (
          <div
            key={idx}
            className="p-4 rounded-lg border hover:shadow-md transition-shadow"
            style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}
          >
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <div className="flex items-center gap-3 mb-2">
                  <h3 className="font-semibold text-lg">{job.title}</h3>
                  <span
                    className="px-3 py-1 rounded-full text-xs font-medium"
                    style={{ backgroundColor: "#D1FAE5", color: "#065F46" }}
                  >
                    {job.department}
                  </span>
                  <span
                    className="px-3 py-1 rounded-full text-xs font-medium"
                    style={{ backgroundColor: "#DBEAFE", color: "#1E3A8A" }}
                  >
                    {job.status}
                  </span>
                  <span className="text-sm opacity-70">{job.applications} applications</span>
                </div>
                <p className="text-sm opacity-70">{job.location} · Full-time</p>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg border hover:bg-gray-50 transition-colors"
                  style={{ borderColor: "#E5E7EB" }}
                >
                  Edit
                </button>
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg border hover:bg-gray-50 transition-colors"
                  style={{ borderColor: "#E5E7EB" }}
                >
                  Duplicate
                </button>
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg border hover:bg-gray-50 transition-colors"
                  style={{ borderColor: "#E5E7EB" }}
                >
                  Close
                </button>
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg hover:opacity-80 transition-opacity"
                  style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}
                >
                  View Applications →
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );

  const renderApplications = () => (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-semibold mb-1">Career Management → Applications Inbox</h2>
        <p className="text-sm opacity-70">Review and manage job applications</p>
      </div>

      <div className="flex gap-2 mb-6">
        {["All Jobs", "All Status"].map((filter) => (
          <button
            key={filter}
            onClick={hapticTap}
            className="px-4 py-2 rounded-full text-sm transition-all hover:opacity-80"
            style={{ backgroundColor: "#E6ECF5", color: "#1E2A47" }}
          >
            {filter} ▼
          </button>
        ))}
        <input
          type="text"
          placeholder="Search by name..."
          className="ml-auto px-4 py-2 rounded-lg border"
          style={{ borderColor: "#E5E7EB", width: "300px" }}
        />
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
                  <span
                    className="px-3 py-1 rounded-full text-xs font-medium"
                    style={{
                      backgroundColor: app.status === "New" ? "#DBEAFE" : app.status === "Reviewed" ? "#FEF3C7" : "#D1FAE5",
                      color: app.status === "New" ? "#1E3A8A" : app.status === "Reviewed" ? "#92400E" : "#065F46",
                    }}
                  >
                    {app.status}
                  </span>
                  <h3 className="font-semibold text-lg">{app.name}</h3>
                  <span className="opacity-70">|</span>
                  <span className="text-sm opacity-70">{app.position}</span>
                  <span className="opacity-70">|</span>
                  <span className="text-sm opacity-70">{app.date}</span>
                </div>
                {app.portfolio && (
                  <p className="text-sm mb-1">Portfolio: <span className="text-blue-600">{app.portfolio}</span></p>
                )}
                <p className="text-sm opacity-70">Experience: {app.experience}</p>
                <div className="flex items-center gap-4 mt-2 text-sm">
                  <span className="flex items-center gap-1">
                    <CheckCircle size={14} style={{ color: "#2E7D32" }} />
                    Cover letter attached
                  </span>
                  <span className="flex items-center gap-1">
                    <CheckCircle size={14} style={{ color: "#2E7D32" }} />
                    Resume attached
                  </span>
                </div>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg border hover:bg-gray-50 transition-colors flex items-center gap-2"
                  style={{ borderColor: "#E5E7EB" }}
                >
                  <Eye size={16} />
                  View Full Application
                </button>
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg border hover:bg-gray-50 transition-colors"
                  style={{ borderColor: "#E5E7EB" }}
                >
                  Mark Reviewed
                </button>
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg hover:opacity-80 transition-opacity"
                  style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}
                >
                  Interview
                </button>
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg hover:opacity-80 transition-opacity"
                  style={{ backgroundColor: "#C44545", color: "#FFFFFF" }}
                >
                  Reject
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="flex gap-2 mt-6">
        <button
          onClick={hapticTap}
          className="px-4 py-2 rounded-lg border hover:bg-gray-50 transition-colors"
          style={{ borderColor: "#E5E7EB" }}
        >
          Export to CSV
        </button>
        <button
          onClick={hapticTap}
          className="px-4 py-2 rounded-lg border hover:bg-gray-50 transition-colors"
          style={{ borderColor: "#E5E7EB" }}
        >
          Bulk Update Status
        </button>
      </div>
    </div>
  );

  const renderSettings = () => (
    <div>
      <h2 className="text-2xl font-semibold mb-1">Career Page Settings</h2>
      <p className="text-sm opacity-70 mb-6">Configure the public careers page content</p>

      <div className="space-y-6">
        <div>
          <label className="block mb-2 font-medium">Hero Text</label>
          <input
            type="text"
            defaultValue="Join the Muuqwear Team"
            className="w-full px-4 py-2 rounded-lg border"
            style={{ borderColor: "#E5E7EB" }}
          />
        </div>

        <div>
          <label className="block mb-2 font-medium">Culture Description</label>
          <textarea
            rows={4}
            defaultValue="We're building the future of performance apparel..."
            className="w-full px-4 py-2 rounded-lg border"
            style={{ borderColor: "#E5E7EB" }}
          />
        </div>

        <button
          onClick={hapticTap}
          className="px-6 py-2 rounded-lg hover:opacity-80 transition-opacity"
          style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}
        >
          Save Changes
        </button>
      </div>
    </div>
  );

  if (activeTab === "applications") return renderApplications();
  if (activeTab === "settings") return renderSettings();
  return renderJobPostings();
}
