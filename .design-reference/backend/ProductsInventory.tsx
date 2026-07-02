import { Plus, Download, Upload, Edit } from "lucide-react";
import { hapticTap } from "../../utils/haptics";

const products = [
  { name: "The Relaxed Hoodie", sku: "MH-001", price: "$89", category: "Apparel", sizes: { XS: 12, S: 24, M: 32, L: 18, XL: 8 }, status: "In Stock" },
  { name: "Cargo Tech Pants", sku: "MT-003", price: "$145", category: "Apparel", sizes: { XS: 2, S: 3, M: 1, L: 0, XL: 0 }, status: "Low Stock" },
  { name: "Veil Crossbody Bag", sku: "MA-012", price: "$60", category: "Accessories", sizes: { "One Size": 45 }, status: "In Stock" },
  { name: "Sapphire Tee", sku: "MT-007", price: "$45", category: "Apparel", sizes: { XS: 0, S: 0, M: 0, L: 0, XL: 0 }, status: "Out of Stock" },
  { name: "Explorer Jacket", sku: "MJ-002", price: "$195", category: "Apparel", sizes: { XS: 8, S: 15, M: 20, L: 12, XL: 5 }, status: "In Stock" },
  { name: "Midnight Wrap Scarf", sku: "MA-018", price: "$75", category: "Accessories", sizes: { "One Size": 30 }, status: "In Stock" },
];

interface Props {
  activeTab: string;
}

export function ProductsInventory({ activeTab }: Props) {
  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold mb-1">Products & Inventory</h2>
          <p className="text-sm opacity-70">Manage product catalog and stock levels</p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={hapticTap}
            className="px-4 py-2 rounded-lg flex items-center gap-2 hover:opacity-80 transition-opacity"
            style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}
          >
            <Plus size={16} />
            ADD NEW PRODUCT
          </button>
          <button
            onClick={hapticTap}
            className="px-4 py-2 rounded-lg border flex items-center gap-2 hover:bg-gray-50 transition-colors"
            style={{ borderColor: "#E5E7EB" }}
          >
            <Upload size={16} />
            IMPORT CSV
          </button>
          <button
            onClick={hapticTap}
            className="px-4 py-2 rounded-lg border flex items-center gap-2 hover:bg-gray-50 transition-colors"
            style={{ borderColor: "#E5E7EB" }}
          >
            <Download size={16} />
            EXPORT
          </button>
        </div>
      </div>

      <div className="flex gap-2 mb-6">
        {["All", "Clothing", "Accessories", "Low Stock", "Out of Stock"].map((filter) => (
          <button
            key={filter}
            onClick={hapticTap}
            className="px-4 py-2 rounded-full text-sm transition-all hover:opacity-80"
            style={{ backgroundColor: "#E6ECF5", color: "#1E2A47" }}
          >
            {filter}
          </button>
        ))}
      </div>

      <div className="space-y-3">
        {products.map((product, idx) => (
          <div
            key={idx}
            className="p-4 rounded-lg border hover:shadow-md transition-shadow"
            style={{ backgroundColor: "#FFFFFF", borderColor: "#E5E7EB" }}
          >
            <div className="flex items-start justify-between mb-3">
              <div>
                <div className="flex items-center gap-3 mb-1">
                  <h3 className="font-semibold text-lg">{product.name}</h3>
                  {product.status === "Low Stock" && (
                    <span
                      className="px-3 py-1 rounded-full text-xs font-medium"
                      style={{ backgroundColor: "#FEF3C7", color: "#92400E" }}
                    >
                      LOW STOCK
                    </span>
                  )}
                  {product.status === "Out of Stock" && (
                    <span
                      className="px-3 py-1 rounded-full text-xs font-medium"
                      style={{ backgroundColor: "#FEE2E2", color: "#991B1B" }}
                    >
                      OUT OF STOCK
                    </span>
                  )}
                </div>
                <p className="text-sm opacity-70">
                  SKU: {product.sku} · Price: {product.price} · {product.category}
                </p>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg border hover:bg-gray-50 transition-colors flex items-center gap-2"
                  style={{ borderColor: "#E5E7EB" }}
                >
                  <Edit size={16} />
                  Edit
                </button>
                <button
                  onClick={hapticTap}
                  className="px-4 py-2 rounded-lg hover:opacity-80 transition-opacity"
                  style={{ backgroundColor: "#1E2A47", color: "#FFFFFF" }}
                >
                  Update Stock
                </button>
              </div>
            </div>
            <div className="flex gap-4 text-sm">
              {Object.entries(product.sizes).map(([size, count]) => (
                <div key={size} className="flex items-center gap-1">
                  <span className="font-medium">{size}</span>
                  <span
                    className="px-2 py-0.5 rounded"
                    style={{
                      backgroundColor: count === 0 ? "#FEE2E2" : count < 5 ? "#FEF3C7" : "#D1FAE5",
                      color: count === 0 ? "#991B1B" : count < 5 ? "#92400E" : "#065F46",
                    }}
                  >
                    {count}
                  </span>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
