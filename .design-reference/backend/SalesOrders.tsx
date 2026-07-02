import { useState } from "react";
import { Eye, Download, CheckCircle, XCircle, Clock } from "lucide-react";
import { hapticTap } from "../../utils/haptics";

const orders = [
  { id: "#12345", customer: "jordan@email.com", date: "Mar 24, 2025", total: "$89.00", status: "Pending", items: "The Relaxed Hoodie (L) x1" },
  { id: "#12344", customer: "alex@email.com", date: "Mar 24, 2025", total: "$245.00", status: "Shipped", items: "Cargo Tech Pants (M) x1, Veil Crossbody x1" },
  { id: "#12343", customer: "elena@email.com", date: "Mar 23, 2025", total: "$67.00", status: "Delivered", items: "Sapphire Tee (S) x1" },
  { id: "#12342", customer: "sarah@email.com", date: "Mar 23, 2025", total: "$178.00", status: "Processing", items: "Explorer Jacket (M) x1" },
  { id: "#12341", customer: "marcus@email.com", date: "Mar 22, 2025", total: "$134.00", status: "Pending", items: "The Relaxed Hoodie (XL) x1, Sapphire Tee (L) x1" },
  { id: "#12340", customer: "david@email.com", date: "Mar 22, 2025", total: "$89.00", status: "Delivered", items: "The Relaxed Hoodie (M) x1" },
];

const returns = [
  { id: "#R234", order: "#12340", customer: "alex@email.com", date: "Mar 23, 2025", reason: "Wrong size", status: "Pending" },
  { id: "#R233", order: "#12335", customer: "john@email.com", date: "Mar 21, 2025", reason: "Defective item", status: "Approved" },
  { id: "#R232", order: "#12330", customer: "maria@email.com", date: "Mar 20, 2025", reason: "Changed mind", status: "Denied" },
];

const refunds = [
  { id: "#RF123", order: "#12330", customer: "maria@email.com", amount: "$89.00", date: "Mar 21, 2025", status: "Processing" },
  { id: "#RF122", order: "#12325", customer: "alex@email.com", amount: "$145.00", date: "Mar 20, 2025", status: "Completed" },
];

interface Props {
  activeTab: string;
}

export function SalesOrders({ activeTab }: Props) {
  const [filterStatus, setFilterStatus] = useState("All");

  const renderOrders = () => (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold mb-1">Orders & Returns</h2>
          <p className="text-sm opacity-70">Manage customer orders and returns</p>
        </div>
        <button
          onClick={hapticTap}
          className="px-4 py-2 rounded-lg flex items-center gap-2 hover:opacity-80 transition-opacity"
          style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}
        >
          <Download size={16} />
          Export
        </button>
      </div>

      <div className="flex gap-2 mb-6">
        {["All", "Pending", "Processing", "Shipped", "Delivered"].map((status) => (
          <button
            key={status}
            onClick={() => {
              hapticTap();
              setFilterStatus(status);
            }}
            className="px-4 py-2 rounded-full text-sm transition-all"
            style={{
              backgroundColor: filterStatus === status ? "#1E2A47" : "#E6ECF5",
              color: filterStatus === status ? "#FFFFFF" : "#1E2A47",
            }}
          >
            {status}
          </button>
        ))}
      </div>

      <div className="space-y-3">
        {orders.map((order) => (
          <div
            key={order.id}
            className="p-4 rounded-lg border hover:shadow-md transition-shadow"
            style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}
          >
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <div className="flex items-center gap-3 mb-2">
                  <span className="font-semibold text-lg">{order.id}</span>
                  <span
                    className="px-3 py-1 rounded-full text-xs font-medium"
                    style={{
                      backgroundColor: order.status === "Pending" ? "#FEF3C7" : order.status === "Shipped" ? "#DBEAFE" : "#D1FAE5",
                      color: order.status === "Pending" ? "#92400E" : order.status === "Shipped" ? "#1E3A8A" : "#065F46",
                    }}
                  >
                    {order.status}
                  </span>
                </div>
                <p className="text-sm opacity-70 mb-1">{order.customer} · {order.date} · {order.total}</p>
                <p className="text-sm">Items: {order.items}</p>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg border hover:bg-gray-50 transition-colors"
                  style={{ borderColor: "#E5E7EB" }}
                >
                  <Eye size={16} />
                </button>
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg hover:opacity-80 transition-opacity"
                  style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}
                >
                  Process
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );

  const renderReturns = () => (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-semibold mb-1">Process Returns</h2>
        <p className="text-sm opacity-70">Review and approve return requests</p>
      </div>

      <div className="space-y-3">
        {returns.map((ret) => (
          <div
            key={ret.id}
            className="p-4 rounded-lg border hover:shadow-md transition-shadow"
            style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}
          >
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <div className="flex items-center gap-3 mb-2">
                  <span className="font-semibold">{ret.id}</span>
                  <span className="text-sm opacity-70">Order {ret.order}</span>
                  <span
                    className="px-3 py-1 rounded-full text-xs font-medium"
                    style={{
                      backgroundColor: ret.status === "Pending" ? "#FEF3C7" : ret.status === "Approved" ? "#D1FAE5" : "#FEE2E2",
                      color: ret.status === "Pending" ? "#92400E" : ret.status === "Approved" ? "#065F46" : "#991B1B",
                    }}
                  >
                    {ret.status}
                  </span>
                </div>
                <p className="text-sm opacity-70 mb-1">{ret.customer} · {ret.date}</p>
                <p className="text-sm">Reason: {ret.reason}</p>
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
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );

  const renderRefunds = () => (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-semibold mb-1">Refunds</h2>
        <p className="text-sm opacity-70">Manage refund processing</p>
      </div>

      <div className="space-y-3">
        {refunds.map((refund) => (
          <div
            key={refund.id}
            className="p-4 rounded-lg border hover:shadow-md transition-shadow"
            style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}
          >
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <div className="flex items-center gap-3 mb-2">
                  <span className="font-semibold">{refund.id}</span>
                  <span className="text-sm opacity-70">Order {refund.order}</span>
                  <span
                    className="px-3 py-1 rounded-full text-xs font-medium"
                    style={{
                      backgroundColor: refund.status === "Processing" ? "#DBEAFE" : "#D1FAE5",
                      color: refund.status === "Processing" ? "#1E3A8A" : "#065F46",
                    }}
                  >
                    {refund.status}
                  </span>
                </div>
                <p className="text-sm opacity-70 mb-1">{refund.customer} · {refund.date}</p>
                <p className="text-sm font-semibold">{refund.amount}</p>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg hover:opacity-80 transition-opacity"
                  style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}
                >
                  Process Refund
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );

  if (activeTab === "returns") return renderReturns();
  if (activeTab === "refunds") return renderRefunds();
  return renderOrders();
}
