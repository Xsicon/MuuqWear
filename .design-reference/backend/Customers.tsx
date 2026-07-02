import { Download, Eye } from "lucide-react";
import { hapticTap } from "../../utils/haptics";

const customers = [
  { name: "Jordan Mitchell", email: "jordan@email.com", orders: 12, spent: "$1,245", joined: "Jan 15, 2024", lastOrder: "Mar 24, 2025" },
  { name: "Sarah Kim", email: "sarah@email.com", orders: 8, spent: "$892", joined: "Mar 3, 2024", lastOrder: "Mar 23, 2025" },
  { name: "Marcus Chen", email: "marcus@email.com", orders: 5, spent: "$567", joined: "Jun 12, 2024", lastOrder: "Mar 22, 2025" },
  { name: "Elena Rodriguez", email: "elena@email.com", orders: 15, spent: "$2,134", joined: "Nov 8, 2023", lastOrder: "Mar 23, 2025" },
  { name: "David Kowalski", email: "david@email.com", orders: 3, spent: "$267", joined: "Feb 1, 2025", lastOrder: "Mar 22, 2025" },
  { name: "Amina Osman", email: "amina@email.com", orders: 22, spent: "$3,456", joined: "Sep 5, 2023", lastOrder: "Mar 24, 2025" },
];

interface Props {
  activeTab: string;
}

export function Customers({ activeTab }: Props) {
  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold mb-1">CUSTOMERS ({customers.length})</h2>
          <p className="text-sm opacity-70">View and manage customer accounts</p>
        </div>
        <button
          onClick={hapticTap}
          className="px-4 py-2 rounded-lg flex items-center gap-2 hover:opacity-80 transition-opacity"
          style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}
        >
          <Download size={16} />
          EXPORT
        </button>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="border-b" style={{ borderColor: "#E5E7EB" }}>
              <th className="text-left py-3 px-4 text-sm font-semibold opacity-70">NAME</th>
              <th className="text-left py-3 px-4 text-sm font-semibold opacity-70">EMAIL</th>
              <th className="text-left py-3 px-4 text-sm font-semibold opacity-70">ORDERS</th>
              <th className="text-left py-3 px-4 text-sm font-semibold opacity-70">TOTAL SPENT</th>
              <th className="text-left py-3 px-4 text-sm font-semibold opacity-70">JOINED</th>
              <th className="text-left py-3 px-4 text-sm font-semibold opacity-70">LAST ORDER</th>
              <th className="text-left py-3 px-4 text-sm font-semibold opacity-70">ACTIONS</th>
            </tr>
          </thead>
          <tbody>
            {customers.map((customer, idx) => (
              <tr
                key={idx}
                className="border-b hover:bg-gray-50 transition-colors"
                style={{ borderColor: "#E5E7EB" }}
              >
                <td className="py-3 px-4">{customer.name}</td>
                <td className="py-3 px-4 text-sm opacity-70">{customer.email}</td>
                <td className="py-3 px-4">{customer.orders}</td>
                <td className="py-3 px-4 font-semibold">{customer.spent}</td>
                <td className="py-3 px-4 text-sm opacity-70">{customer.joined}</td>
                <td className="py-3 px-4 text-sm opacity-70">{customer.lastOrder}</td>
                <td className="py-3 px-4">
                  <button
                    onClick={hapticTap}
                    className="p-2 rounded-lg border hover:bg-gray-100 transition-colors"
                    style={{ borderColor: "#E5E7EB" }}
                  >
                    <Eye size={16} />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
