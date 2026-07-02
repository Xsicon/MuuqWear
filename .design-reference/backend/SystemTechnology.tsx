import { CheckCircle, RefreshCw, Database, CreditCard, Server, Send } from "lucide-react";
import { hapticTap } from "../../utils/haptics";

const logs = [
  { timestamp: "Mar 24, 2025 14:32", level: "Error", message: "Stripe webhook failed - Retrying" },
  { timestamp: "Mar 24, 2025 09:15", level: "Info", message: "Database backup completed" },
  { timestamp: "Mar 24, 2025 06:00", level: "Info", message: "Daily inventory sync completed" },
  { timestamp: "Mar 23, 2025 23:45", level: "Warning", message: "High API usage detected" },
];

interface Props {
  activeTab: string;
}

export function SystemTechnology({ activeTab }: Props) {
  const renderHealth = () => (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-semibold mb-1">System & Technology</h2>
        <p className="text-sm opacity-70">Monitor system health and integrations</p>
      </div>

      <div className="grid grid-cols-3 gap-4 mb-6">
        <div className="p-6 rounded-lg border" style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}>
          <div className="flex items-center gap-3 mb-4">
            <Database size={24} style={{ color: "#2E7D32" }} />
            <div>
              <h3 className="font-semibold">Database</h3>
              <div className="flex items-center gap-2">
                <CheckCircle size={16} style={{ color: "#2E7D32" }} />
                <span className="text-sm" style={{ color: "#2E7D32" }}>Connected</span>
              </div>
            </div>
          </div>
          <p className="text-sm opacity-70">Last Backup: Today 2:00 AM</p>
        </div>

        <div className="p-6 rounded-lg border" style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}>
          <div className="flex items-center gap-3 mb-4">
            <CreditCard size={24} style={{ color: "#2E7D32" }} />
            <div>
              <h3 className="font-semibold">Stripe</h3>
              <div className="flex items-center gap-2">
                <CheckCircle size={16} style={{ color: "#2E7D32" }} />
                <span className="text-sm" style={{ color: "#2E7D32" }}>Connected</span>
              </div>
            </div>
          </div>
          <p className="text-sm opacity-70">Processing payments normally</p>
        </div>

        <div className="p-6 rounded-lg border" style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}>
          <div className="flex items-center gap-3 mb-4">
            <Server size={24} style={{ color: "#2E7D32" }} />
            <div>
              <h3 className="font-semibold">Supabase</h3>
              <div className="flex items-center gap-2">
                <CheckCircle size={16} style={{ color: "#2E7D32" }} />
                <span className="text-sm" style={{ color: "#2E7D32" }}>Connected</span>
              </div>
            </div>
          </div>
          <p className="text-sm opacity-70">Active Users: 47</p>
        </div>
      </div>

      <div className="p-6 rounded-lg border mb-6" style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}>
        <h3 className="font-semibold mb-4">System Health</h3>
        <div className="grid grid-cols-2 gap-4 text-sm">
          <div className="flex items-center justify-between">
            <span className="opacity-70">Database Status:</span>
            <span className="font-medium" style={{ color: "#2E7D32" }}>✅ Connected</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="opacity-70">Stripe Status:</span>
            <span className="font-medium" style={{ color: "#2E7D32" }}>✅ Connected</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="opacity-70">Supabase Status:</span>
            <span className="font-medium" style={{ color: "#2E7D32" }}>✅ Connected</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="opacity-70">Last Backup:</span>
            <span className="font-medium">Today 2:00 AM</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="opacity-70">Active Users:</span>
            <span className="font-medium">47</span>
          </div>
        </div>
      </div>
    </div>
  );

  const renderIntegrations = () => (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-semibold mb-1">Integrations</h2>
        <p className="text-sm opacity-70">Manage third-party integrations</p>
      </div>

      <div className="grid grid-cols-2 gap-4">
        {["Stripe", "Supabase", "SendGrid", "Cloudflare"].map((integration) => (
          <div
            key={integration}
            className="p-6 rounded-lg border"
            style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}
          >
            <div className="flex items-center justify-between mb-4">
              <h3 className="font-semibold text-lg">{integration}</h3>
              <CheckCircle size={20} style={{ color: "#2E7D32" }} />
            </div>
            <div className="flex gap-2">
              <button
                onClick={hapticTap}
                className="px-4 py-2 rounded-lg border hover:bg-gray-50 transition-colors"
                style={{ borderColor: "#E5E7EB" }}
              >
                Reconnect
              </button>
              <button
                onClick={hapticTap}
                className="px-4 py-2 rounded-lg hover:opacity-80 transition-opacity"
                style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}
              >
                Test Connection
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );

  const renderSync = () => (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-semibold mb-1">Sync Tools</h2>
        <p className="text-sm opacity-70">Manually trigger data synchronization</p>
      </div>

      <div className="grid grid-cols-2 gap-4">
        {[
          { label: "Resync Stripe Orders", description: "Re-import all orders from Stripe" },
          { label: "Recalculate Affiliate Commissions", description: "Recalculate all pending commissions" },
          { label: "Sync Inventory from ERP", description: "Update stock levels from ERP system" },
          { label: "Clear Cache", description: "Clear all application caches" },
        ].map((tool) => (
          <div
            key={tool.label}
            className="p-6 rounded-lg border"
            style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}
          >
            <h3 className="font-semibold mb-2">{tool.label}</h3>
            <p className="text-sm opacity-70 mb-4">{tool.description}</p>
            <button
              onClick={hapticTap}
              className="w-full px-4 py-2 rounded-lg hover:opacity-80 transition-opacity flex items-center justify-center gap-2"
              style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}
            >
              <RefreshCw size={16} />
              Run Sync
            </button>
          </div>
        ))}
      </div>
    </div>
  );

  const renderLogs = () => (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-semibold mb-1">Logs</h2>
        <p className="text-sm opacity-70">View system logs and errors</p>
      </div>

      <div className="flex gap-2 mb-6">
        <select
          className="px-4 py-2 rounded-lg border"
          style={{ borderColor: "#E5E7EB" }}
        >
          <option>Last 7 Days</option>
          <option>Last 30 Days</option>
          <option>Last 90 Days</option>
        </select>
        <select
          className="px-4 py-2 rounded-lg border"
          style={{ borderColor: "#E5E7EB" }}
        >
          <option>All Levels</option>
          <option>Error</option>
          <option>Warning</option>
          <option>Info</option>
        </select>
        <input
          type="text"
          placeholder="Search..."
          className="flex-1 px-4 py-2 rounded-lg border"
          style={{ borderColor: "#E5E7EB" }}
        />
      </div>

      <div className="space-y-2">
        {logs.map((log, idx) => (
          <div
            key={idx}
            className="p-4 rounded-lg border hover:shadow-md transition-shadow"
            style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}
          >
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-4">
                <span className="text-sm opacity-70 font-mono">{log.timestamp}</span>
                <span
                  className="px-3 py-1 rounded-full text-xs font-medium"
                  style={{
                    backgroundColor: log.level === "Error" ? "#FEE2E2" : log.level === "Warning" ? "#FEF3C7" : "#DBEAFE",
                    color: log.level === "Error" ? "#991B1B" : log.level === "Warning" ? "#92400E" : "#1E3A8A",
                  }}
                >
                  {log.level}
                </span>
                <span className="text-sm">{log.message}</span>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );

  const renderJobs = () => (
    <div>
      <h2 className="text-2xl font-semibold mb-1">Background Jobs</h2>
      <p className="text-sm opacity-70">Monitor scheduled and running background jobs</p>
    </div>
  );

  if (activeTab === "integrations") return renderIntegrations();
  if (activeTab === "sync") return renderSync();
  if (activeTab === "logs") return renderLogs();
  if (activeTab === "jobs") return renderJobs();
  return renderHealth();
}
