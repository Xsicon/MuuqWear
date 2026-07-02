import { useBackendAuth } from "../../contexts/BackendAuthContext";
import {
  ShoppingCart, Users, Package, FileText, UserPlus, MessageCircle,
  Briefcase, Server, TrendingUp, AlertCircle, CheckCircle2, Clock
} from "lucide-react";

export function Overview() {
  const { user } = useBackendAuth();

  const getWelcomeMessage = () => {
    switch (user?.role) {
      case "Admin":
        return "You have full access to all dashboard features and settings.";
      case "Operations Manager":
        return "Manage orders, affiliates, and career postings from your dashboard.";
      case "Customer Support":
        return "Handle customer inquiries through live chat and support tickets.";
      case "Merchandising":
        return "Manage product catalog, inventory, and stock levels.";
      case "Creative & Content":
        return "Create and publish content across journal, events, and help center.";
      case "Technology & Systems":
        return "Monitor system health, integrations, and manage technical operations.";
      default:
        return "Welcome to your dashboard.";
    }
  };

  const getRoleAccess = () => {
    switch (user?.role) {
      case "Admin":
        return [
          { icon: ShoppingCart, label: "Sales & Orders", description: "Full order management" },
          { icon: Users, label: "Customers", description: "Customer database access" },
          { icon: Package, label: "Products & Inventory", description: "Complete product control" },
          { icon: FileText, label: "Content", description: "Content management system" },
          { icon: UserPlus, label: "Affiliate Program", description: "Affiliate management" },
          { icon: MessageCircle, label: "Customer Support", description: "Support system access" },
          { icon: Briefcase, label: "Career Management", description: "Job posting & hiring" },
          { icon: Server, label: "System & Technology", description: "System administration" },
        ];
      case "Operations Manager":
        return [
          { icon: ShoppingCart, label: "Sales & Orders", description: "Process orders and returns" },
          { icon: UserPlus, label: "Affiliate Program", description: "Manage affiliates & payouts" },
          { icon: Briefcase, label: "Career Management", description: "Hiring & job postings" },
        ];
      case "Customer Support":
        return [
          { icon: MessageCircle, label: "Customer Support", description: "Live chat & ticket system" },
        ];
      case "Merchandising":
        return [
          { icon: Package, label: "Products & Inventory", description: "Product & stock management" },
        ];
      case "Creative & Content":
        return [
          { icon: FileText, label: "Content", description: "Publish journal, events, & FAQs" },
        ];
      case "Technology & Systems":
        return [
          { icon: Server, label: "System & Technology", description: "System health & integrations" },
        ];
      default:
        return [];
    }
  };

  const stats = [
    { label: "Active Orders", value: "12", trend: "+3", icon: ShoppingCart, color: "#2E7D32" },
    { label: "Pending Support", value: "3", trend: "-2", icon: MessageCircle, color: "#ED6C02" },
    { label: "Low Stock Items", value: "3", trend: "0", icon: AlertCircle, color: "#C44545" },
    { label: "System Status", value: "Healthy", trend: "100%", icon: CheckCircle2, color: "#2E7D32" },
  ];

  const recentActivity = [
    { action: "New order #12345", time: "2 min ago", icon: ShoppingCart },
    { action: "Return request #R234", time: "5 min ago", icon: Clock },
    { action: "Affiliate application from Sarah K.", time: "10 min ago", icon: UserPlus },
    { action: "Low stock alert: Cargo Tech Pants", time: "1 hour ago", icon: AlertCircle },
  ];

  return (
    <div>
      {/* Welcome Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-semibold mb-2">
          Welcome back, {user?.name}!
        </h1>
        <p className="opacity-70">{getWelcomeMessage()}</p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-4 gap-6 mb-8">
        {stats.map((stat, idx) => {
          const Icon = stat.icon;
          return (
            <div
              key={idx}
              className="p-6 rounded-lg border hover:shadow-md transition-shadow"
              style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}
            >
              <div className="flex items-start justify-between mb-4">
                <Icon size={24} style={{ color: stat.color }} />
                <span
                  className="text-xs px-2 py-1 rounded-full"
                  style={{
                    backgroundColor: stat.trend.startsWith("+") ? "#D1FAE5" : stat.trend.startsWith("-") ? "#FEE2E2" : "#E5E7EB",
                    color: stat.trend.startsWith("+") ? "#065F46" : stat.trend.startsWith("-") ? "#991B1B" : "#374151",
                  }}
                >
                  {stat.trend}
                </span>
              </div>
              <div className="text-2xl font-semibold mb-1">{stat.value}</div>
              <div className="text-sm opacity-70">{stat.label}</div>
            </div>
          );
        })}
      </div>

      {/* Two Column Layout */}
      <div className="grid grid-cols-2 gap-6">
        {/* Your Access */}
        <div className="p-6 rounded-lg border" style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}>
          <h2 className="text-xl font-semibold mb-4">Your Access</h2>
          <div className="space-y-3">
            {getRoleAccess().map((access, idx) => {
              const Icon = access.icon;
              return (
                <div
                  key={idx}
                  className="flex items-center gap-3 p-3 rounded-lg hover:bg-gray-50 transition-colors"
                >
                  <div
                    className="w-10 h-10 rounded-lg flex items-center justify-center"
                    style={{ backgroundColor: "#E6ECF5" }}
                  >
                    <Icon size={20} style={{ color: "#1E2A47" }} />
                  </div>
                  <div>
                    <div className="font-medium">{access.label}</div>
                    <div className="text-sm opacity-70">{access.description}</div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Recent Activity */}
        <div className="p-6 rounded-lg border" style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}>
          <h2 className="text-xl font-semibold mb-4">Recent Activity</h2>
          <div className="space-y-3">
            {recentActivity.map((activity, idx) => {
              const Icon = activity.icon;
              return (
                <div
                  key={idx}
                  className="flex items-center gap-3 p-3 rounded-lg hover:bg-gray-50 transition-colors"
                >
                  <div
                    className="w-10 h-10 rounded-lg flex items-center justify-center"
                    style={{ backgroundColor: "#E6ECF5" }}
                  >
                    <Icon size={18} style={{ color: "#1E2A47" }} />
                  </div>
                  <div className="flex-1">
                    <div className="text-sm">{activity.action}</div>
                    <div className="text-xs opacity-70">{activity.time}</div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="mt-6 p-6 rounded-lg border" style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}>
        <h2 className="text-xl font-semibold mb-4">Quick Actions</h2>
        <div className="flex gap-3">
          <button
            className="px-4 py-2 rounded-lg hover:opacity-80 transition-opacity"
            style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}
          >
            View All Orders
          </button>
          <button
            className="px-4 py-2 rounded-lg border hover:bg-gray-50 transition-colors"
            style={{ borderColor: "#E5E7EB" }}
          >
            Export Data
          </button>
          <button
            className="px-4 py-2 rounded-lg border hover:bg-gray-50 transition-colors"
            style={{ borderColor: "#E5E7EB" }}
          >
            System Health
          </button>
        </div>
      </div>
    </div>
  );
}
