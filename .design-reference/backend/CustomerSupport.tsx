import { Send, X } from "lucide-react";
import { hapticTap } from "../../utils/haptics";
import { useTickets } from "../../context/TicketContext";

const conversations = [
  { name: "Sarah K.", waiting: "2 min", order: "#12345", status: "waiting" },
  { name: "Marcus C.", waiting: "8 min", order: "#12342", status: "active" },
  { name: "Elena R.", waiting: "15 min", order: "#12338", status: "waiting" },
];

interface Props {
  activeTab: string;
}

export function CustomerSupport({ activeTab }: Props) {
  const { tickets } = useTickets();

  const renderLiveChat = () => (
    <div className="grid grid-cols-3 gap-6 h-[calc(100vh-200px)]">
      <div className="col-span-1 border rounded-lg" style={{ borderColor: "#E5E7EB" }}>
        <div className="p-4 border-b" style={{ borderColor: "#E5E7EB" }}>
          <h3 className="font-semibold">Active Conversations (3)</h3>
        </div>
        <div className="divide-y" style={{ borderColor: "#E5E7EB" }}>
          {conversations.map((conv, idx) => (
            <button
              key={idx}
              onClick={hapticTap}
              className="w-full p-4 text-left hover:bg-gray-50 transition-colors"
            >
              <div className="flex items-center justify-between mb-1">
                <span className="font-semibold">{conv.name}</span>
                <span className="text-xs opacity-70">{conv.waiting}</span>
              </div>
              <p className="text-sm opacity-70">Order: {conv.order}</p>
              <div className="flex items-center gap-2 mt-2">
                <div
                  className="w-2 h-2 rounded-full"
                  style={{ backgroundColor: conv.status === "waiting" ? "#FEF3C7" : "#D1FAE5" }}
                />
                <span className="text-xs capitalize">{conv.status}</span>
              </div>
            </button>
          ))}
        </div>
      </div>

      <div className="col-span-2 border rounded-lg flex flex-col" style={{ borderColor: "#E5E7EB" }}>
        <div className="p-4 border-b flex items-center justify-between" style={{ borderColor: "#E5E7EB" }}>
          <div>
            <h3 className="font-semibold">Sarah K.</h3>
            <p className="text-sm opacity-70">Order #12345</p>
          </div>
          <button
            onClick={hapticTap}
            className="px-4 py-2 rounded-lg hover:opacity-80 transition-opacity"
            style={{ backgroundColor: "#C44545", color: "#FFFFFF" }}
          >
            CLOSE CHAT
          </button>
        </div>

        <div className="flex-1 p-4 overflow-y-auto space-y-3">
          <div className="flex justify-start">
            <div className="max-w-sm p-3 rounded-lg" style={{ backgroundColor: "#E6ECF5" }}>
              <p className="text-sm">Hi, I haven't received my order yet. It's been 5 days.</p>
              <span className="text-xs opacity-70 mt-1 block">10:32 AM</span>
            </div>
          </div>
          <div className="flex justify-end">
            <div className="max-w-sm p-3 rounded-lg" style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}>
              <p className="text-sm">Hi Sarah! Let me check the status of your order #12345 for you.</p>
              <span className="text-xs opacity-70 mt-1 block">10:33 AM</span>
            </div>
          </div>
        </div>

        <div className="p-4 border-t" style={{ borderColor: "#E5E7EB" }}>
          <div className="flex gap-2">
            <input
              type="text"
              placeholder="Type your message..."
              className="flex-1 px-4 py-2 rounded-lg border"
              style={{ borderColor: "#E5E7EB" }}
            />
            <button
              onClick={hapticTap}
              className="px-6 py-2 rounded-lg hover:opacity-80 transition-opacity flex items-center gap-2"
              style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}
            >
              <Send size={16} />
              Send
            </button>
          </div>
        </div>
      </div>
    </div>
  );

  const renderTickets = () => (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-semibold mb-1">Support Tickets ({tickets.length})</h2>
        <p className="text-sm opacity-70">Manage customer support tickets</p>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="border-b" style={{ borderColor: "#E5E7EB" }}>
              <th className="text-left py-3 px-4 text-sm font-semibold opacity-70">TICKET ID</th>
              <th className="text-left py-3 px-4 text-sm font-semibold opacity-70">PRIORITY</th>
              <th className="text-left py-3 px-4 text-sm font-semibold opacity-70">SUBJECT</th>
              <th className="text-left py-3 px-4 text-sm font-semibold opacity-70">CUSTOMER</th>
              <th className="text-left py-3 px-4 text-sm font-semibold opacity-70">CATEGORY</th>
              <th className="text-left py-3 px-4 text-sm font-semibold opacity-70">DATE</th>
              <th className="text-left py-3 px-4 text-sm font-semibold opacity-70">STATUS</th>
            </tr>
          </thead>
          <tbody>
            {tickets.map((ticket) => (
              <tr
                key={ticket.id}
                className="border-b hover:bg-gray-50 transition-colors cursor-pointer"
                style={{ borderColor: "#E5E7EB" }}
                onClick={hapticTap}
              >
                <td className="py-3 px-4 font-mono text-sm">{ticket.id}</td>
                <td className="py-3 px-4">
                  <span
                    className="px-2 py-1 rounded-full text-xs font-medium"
                    style={{
                      backgroundColor: ticket.priority === "high" ? "#FEE2E2" : "#F3F4F6",
                      color: ticket.priority === "high" ? "#991B1B" : "#374151",
                    }}
                  >
                    {ticket.priority.toUpperCase()}
                  </span>
                </td>
                <td className="py-3 px-4">{ticket.subject}</td>
                <td className="py-3 px-4 text-sm opacity-70">{ticket.customerName}</td>
                <td className="py-3 px-4 text-sm opacity-70">{ticket.category}</td>
                <td className="py-3 px-4 text-sm opacity-70">{ticket.createdAt}</td>
                <td className="py-3 px-4">
                  <span
                    className="px-2 py-1 rounded-full text-xs font-medium"
                    style={{
                      backgroundColor:
                        ticket.status === "open" ? "#DBEAFE" :
                        ticket.status === "in-progress" ? "#FEF3C7" : "#D1FAE5",
                      color:
                        ticket.status === "open" ? "#1E3A8A" :
                        ticket.status === "in-progress" ? "#92400E" : "#065F46",
                    }}
                  >
                    {ticket.status === "in-progress" ? "IN PROGRESS" : ticket.status.toUpperCase()}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );

  if (activeTab === "tickets") return renderTickets();
  if (activeTab === "knowledge") {
    return (
      <div>
        <h2 className="text-2xl font-semibold mb-1">Knowledge Base</h2>
        <p className="text-sm opacity-70">Manage help articles and FAQs</p>
      </div>
    );
  }
  return renderLiveChat();
}
