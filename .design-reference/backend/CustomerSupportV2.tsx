/**
 * Customer Support V2 — React design prototype (Jul 2026)
 * Live Chat, Support Tickets, Knowledge Base with macros + KB quick panel.
 * Implementation plan: docs/CUSTOMER_SUPPORT_REDESIGN.md
 * Supersedes: CustomerSupport.tsx (original Figma export)
 */
import { useState, useRef, useEffect } from "react";
import {
  Send, X, Search, Plus, Edit2, Trash2, Eye, ChevronDown,
  BookOpen, HelpCircle, Package, Truck, RotateCcw, CreditCard,
  Settings, CheckCircle2, FileText, Globe, Clock, ArrowUpRight,
  MessageCircle, Users, UserCheck, Mail, Zap, Timer,
  ThumbsUp, ThumbsDown, MessageSquare, ChevronLeft, ChevronRight,
} from "lucide-react";
import { hapticTap } from "../../utils/haptics";
import { useTickets, Ticket } from "../../context/TicketContext";
import { useBackendAuth } from "../../contexts/BackendAuthContext";
import { useKB, KBArticle, KBStep, KBCategory, KBStatus } from "../../context/KBContext";
import { ImageWithFallback } from "../figma/ImageWithFallback";

/* ─── Palette (cohesive Sapphire + Sky) ──────────────────────────────────────── */
const INK = "#1E2A47";      // primary navy
const SKY = "#0088CC";      // accent blue (from palette)
const SKY_BG = "#E1F0FA";   // soft blue surface
const SUB = "#5B6B85";      // secondary text
const MUTED = "#94A3BE";    // muted text
const LINE = "#E3E9F4";     // borders
const SURF = "#FBFCFE";     // soft surface

/* ─── Canned replies (macros) — shared by Tickets & Live Chat ────────────────── */
const CANNED_REPLIES: { label: string; text: string }[] = [
  { label: "Greeting", text: "Hi! Thanks for reaching out to Muuqwear — I'd be happy to help you with this." },
  { label: "Order status", text: "I've checked your order and it's currently on its way. You'll receive a tracking update by email shortly." },
  { label: "Return label", text: "No problem — I've emailed you a prepaid return label. Once we receive the item, your refund will be processed within 5–7 business days." },
  { label: "Refund issued", text: "Good news — your refund has been issued. Please allow 3–5 business days for it to appear on your statement." },
  { label: "Apology / delay", text: "I'm really sorry for the inconvenience. I'm prioritising this for you right now and will make it right." },
  { label: "Closing", text: "Is there anything else I can help you with? Thanks for being part of the Muuqwear community!" },
];

/* ─── Time helpers ───────────────────────────────────────────────────────────── */
function timeAgo(iso: string) {
  const then = new Date(iso).getTime();
  if (isNaN(then)) return "";
  const mins = Math.max(0, Math.floor((Date.now() - then) / 60000));
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  return `${Math.floor(hrs / 24)}d ago`;
}
function fmtDate(iso: string) {
  const d = new Date(iso);
  return isNaN(d.getTime()) ? iso : d.toLocaleString("en-US", { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" });
}

/* ─── Category metadata ──────────────────────────────────────────────────────── */
const KB_CATS: KBCategory[] = ["Orders", "Shipping", "Returns", "Payments", "Account", "Product Info"];

const CAT_META: Record<KBCategory, { icon: React.ElementType; bg: string; text: string; dot: string }> = {
  Orders:         { icon: Package,    bg: "#DBEAFE", text: "#1D4ED8", dot: "#3B82F6" },
  Shipping:       { icon: Truck,      bg: "#D1FAE5", text: "#065F46", dot: "#22C55E" },
  Returns:        { icon: RotateCcw,  bg: "#FEE2E2", text: "#991B1B", dot: "#EF4444" },
  Payments:       { icon: CreditCard, bg: "#EDE9FE", text: "#5B21B6", dot: "#8B5CF6" },
  Account:        { icon: Settings,   bg: "#FEF3C7", text: "#92400E", dot: "#F59E0B" },
  "Product Info": { icon: HelpCircle, bg: "#F0FDF4", text: "#166534", dot: "#4ADE80" },
};

/* ─── Shared feedback ────────────────────────────────────────────────────────── */
function Toast({ message, onDone }: { message: string; onDone: () => void }) {
  useEffect(() => { const t = setTimeout(onDone, 2400); return () => clearTimeout(t); }, [onDone]);
  return (
    <div className="fixed bottom-6 right-6 z-[70] flex items-center gap-2 px-4 py-3 rounded-xl shadow-xl" style={{ backgroundColor: INK, color: "#FFFFFF" }}>
      <CheckCircle2 size={16} style={{ color: "#22C55E" }} />
      <span className="text-sm font-medium">{message}</span>
    </div>
  );
}

function ConfirmDelete({ label, onConfirm, onCancel }: { label: string; onConfirm: () => void; onCancel: () => void }) {
  return (
    <div className="fixed inset-0 bg-black/40 z-[60] flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl p-6 max-w-sm w-full shadow-2xl">
        <div className="w-12 h-12 rounded-full flex items-center justify-center mx-auto mb-4" style={{ backgroundColor: "#FEE2E2" }}>
          <Trash2 size={20} style={{ color: "#C44545" }} />
        </div>
        <h3 className="text-base font-semibold text-center mb-1" style={{ color: INK }}>Delete article?</h3>
        <p className="text-sm text-center mb-5" style={{ color: SUB }}>"{label}" will be permanently removed from the Help Center.</p>
        <div className="flex gap-2">
          <button onClick={onCancel} className="flex-1 py-2.5 rounded-xl border text-sm font-medium" style={{ borderColor: LINE, color: SUB }}>Cancel</button>
          <button onClick={onConfirm} className="flex-1 py-2.5 rounded-xl text-sm font-medium" style={{ backgroundColor: "#C44545", color: "#FFFFFF" }}>Delete</button>
        </div>
      </div>
    </div>
  );
}

/* ─── KB Article Editor ──────────────────────────────────────────────────────── */
function ArticlePanel({ initial, onSave, onClose }: { initial?: KBArticle; onSave: (a: Partial<KBArticle>) => void; onClose: () => void }) {
  const [title, setTitle]       = useState(initial?.title ?? "");
  const [category, setCategory] = useState<string>(initial?.category ?? "Orders");
  const [content, setContent]   = useState(initial?.content ?? "");
  const [status, setStatus]     = useState<string>(initial?.status ?? "Draft");
  const [heroImage, setHeroImage] = useState(initial?.heroImage ?? "");
  const [steps, setSteps]       = useState<KBStep[]>(initial?.steps ?? []);

  const addStep    = () => { hapticTap(); setSteps((p) => [...p, { detail: "", image: "" }]); };
  const removeStep = (i: number) => { hapticTap(); setSteps((p) => p.filter((_, idx) => idx !== i)); };
  const updateStep = (i: number, patch: Partial<KBStep>) => setSteps((p) => p.map((s, idx) => idx === i ? { ...s, ...patch } : s));

  const handleSave = (publish: boolean) => {
    hapticTap();
    if (!title.trim()) return;
    const cleanSteps = steps.map((s) => ({ detail: s.detail.trim(), image: s.image?.trim() || undefined })).filter((s) => s.detail);
    onSave({
      title, category: category as KBCategory, content,
      heroImage: heroImage.trim() || undefined,
      steps: cleanSteps.length ? cleanSteps : undefined,
      status: (publish ? "Published" : "Draft") as KBStatus,
      lastUpdated: new Date().toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" }),
    });
  };

  return (
    <>
      <div className="fixed inset-0 bg-black/40 z-[55]" onClick={onClose} />
      <div className="fixed top-0 right-0 bottom-0 w-full max-w-xl z-[56] flex flex-col bg-white overflow-hidden" style={{ boxShadow: "-4px 0 40px rgba(30,42,71,0.12)" }}>
        <div className="flex items-center justify-between px-7 py-5 border-b" style={{ borderColor: LINE }}>
          <div>
            <p className="text-[10px] tracking-[0.16em] uppercase font-semibold" style={{ color: MUTED }}>Knowledge Base</p>
            <h3 className="text-lg font-semibold mt-0.5" style={{ color: INK, fontFamily: "Playfair Display, serif" }}>{initial ? "Edit Article" : "New Article"}</h3>
          </div>
          <button onClick={onClose} className="w-8 h-8 rounded-full flex items-center justify-center hover:bg-gray-100 transition-colors"><X size={18} /></button>
        </div>

        <div className="flex-1 px-7 py-6 space-y-5 overflow-y-auto">
          <div>
            <label className="block text-xs font-semibold tracking-wide uppercase mb-1.5" style={{ color: SUB }}>Question / Title</label>
            <input className="w-full border rounded-lg px-4 py-2.5 text-sm outline-none focus:border-[#0088CC] transition-colors" style={{ borderColor: LINE, color: INK }}
              placeholder="e.g. How do I track my order?" value={title} onChange={(e) => setTitle(e.target.value)} />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold tracking-wide uppercase mb-1.5" style={{ color: SUB }}>Category</label>
              <div className="relative">
                <select value={category} onChange={(e) => setCategory(e.target.value)} className="w-full border rounded-lg px-4 py-2.5 text-sm outline-none appearance-none" style={{ borderColor: LINE, color: INK }}>
                  {KB_CATS.map((c) => <option key={c}>{c}</option>)}
                </select>
                <ChevronDown size={14} className="absolute right-3 top-1/2 -translate-y-1/2 pointer-events-none" style={{ color: SUB }} />
              </div>
            </div>
            <div>
              <label className="block text-xs font-semibold tracking-wide uppercase mb-1.5" style={{ color: SUB }}>Status</label>
              <div className="relative">
                <select value={status} onChange={(e) => setStatus(e.target.value)} className="w-full border rounded-lg px-4 py-2.5 text-sm outline-none appearance-none" style={{ borderColor: LINE, color: INK }}>
                  <option>Draft</option>
                  <option>Published</option>
                </select>
                <ChevronDown size={14} className="absolute right-3 top-1/2 -translate-y-1/2 pointer-events-none" style={{ color: SUB }} />
              </div>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold tracking-wide uppercase mb-1.5" style={{ color: SUB }}>Answer / Content</label>
            <textarea rows={5} className="w-full border rounded-lg px-4 py-3 text-sm outline-none focus:border-[#0088CC] transition-colors resize-none" style={{ borderColor: LINE, color: INK }}
              placeholder="Write the full answer that customers will see..." value={content} onChange={(e) => setContent(e.target.value)} />
          </div>

          {/* Cover image */}
          <div>
            <label className="block text-xs font-semibold tracking-wide uppercase mb-1.5" style={{ color: SUB }}>Cover image (optional)</label>
            <input className="w-full border rounded-lg px-4 py-2.5 text-sm outline-none focus:border-[#0088CC] transition-colors" style={{ borderColor: LINE, color: INK }}
              placeholder="Paste an image URL…" value={heroImage} onChange={(e) => setHeroImage(e.target.value)} />
            {heroImage.trim() && (
              <div className="mt-2 rounded-lg overflow-hidden border" style={{ borderColor: LINE }}>
                <ImageWithFallback src={heroImage} alt="Cover preview" className="w-full h-32 object-cover" />
              </div>
            )}
          </div>

          {/* Step-by-step agent instructions */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <label className="block text-xs font-semibold tracking-wide uppercase" style={{ color: SUB }}>Agent step-by-step guide</label>
              <button onClick={addStep} className="flex items-center gap-1 text-xs font-medium px-2.5 py-1.5 rounded-lg transition-colors" style={{ backgroundColor: SKY_BG, color: SKY }}>
                <Plus size={12} /> Add step
              </button>
            </div>
            {steps.length === 0 && <p className="text-xs italic px-1 py-2" style={{ color: MUTED }}>No steps yet — add the actions an agent takes to resolve this, in order.</p>}
            <div className="space-y-3">
              {steps.map((step, i) => (
                <div key={i} className="p-3 rounded-xl border space-y-2" style={{ backgroundColor: SURF, borderColor: LINE }}>
                  <div className="flex items-start gap-2">
                    <div className="w-6 h-6 rounded-full flex items-center justify-center shrink-0 text-[11px] font-bold text-white mt-0.5" style={{ backgroundColor: INK }}>{i + 1}</div>
                    <textarea rows={2} className="flex-1 border rounded-lg px-3 py-2 text-sm outline-none focus:border-[#0088CC] transition-colors resize-none bg-white" style={{ borderColor: LINE, color: INK }}
                      placeholder="Describe what the agent should do in this step…" value={step.detail} onChange={(e) => updateStep(i, { detail: e.target.value })} />
                    <button onClick={() => removeStep(i)} className="w-7 h-7 flex items-center justify-center rounded-lg hover:bg-red-50 transition-colors shrink-0 mt-0.5" title="Remove step">
                      <Trash2 size={13} style={{ color: "#C44545" }} />
                    </button>
                  </div>
                  <div className="pl-8">
                    <input className="w-full border rounded-lg px-3 py-2 text-xs outline-none focus:border-[#0088CC] transition-colors bg-white" style={{ borderColor: LINE, color: INK }}
                      placeholder="Optional image URL for this step…" value={step.image ?? ""} onChange={(e) => updateStep(i, { image: e.target.value })} />
                    {step.image?.trim() && (
                      <div className="mt-2 rounded-lg overflow-hidden border" style={{ borderColor: LINE }}>
                        <ImageWithFallback src={step.image} alt={`Step ${i + 1} preview`} className="w-full h-28 object-cover" />
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

        <div className="px-7 py-5 border-t flex items-center justify-between gap-3" style={{ borderColor: LINE }}>
          <button onClick={onClose} className="px-5 py-2.5 rounded-xl text-sm font-medium border" style={{ borderColor: LINE, color: SUB }}>Cancel</button>
          <div className="flex gap-2">
            <button onClick={() => handleSave(false)} className="px-5 py-2.5 rounded-xl text-sm font-medium border" style={{ borderColor: INK, color: INK }}>Save Draft</button>
            <button onClick={() => handleSave(true)} disabled={!title.trim() || !content.trim()} className="px-5 py-2.5 rounded-xl text-sm font-medium flex items-center gap-2 hover:opacity-90 disabled:opacity-40 transition-opacity" style={{ backgroundColor: INK, color: "#FFFFFF" }}>
              <Globe size={14} /> Publish
            </button>
          </div>
        </div>
      </div>
    </>
  );
}

/* ─── KB Article Viewer (read-only, with reactions + comments) ───────────────── */
function ArticleView({ articleId, onClose, onEdit }: { articleId: number; onClose: () => void; onEdit?: () => void }) {
  const { articles, voteArticle, addComment } = useKB();
  const { user } = useBackendAuth();
  const article = articles.find((a) => a.id === articleId);
  const [comment, setComment] = useState("");
  const endRef = useRef<HTMLDivElement>(null);

  useEffect(() => { endRef.current?.scrollIntoView({ behavior: "smooth" }); }, [article?.comments?.length]);

  if (!article) return null;
  const m = CAT_META[article.category];
  const Icon = m.icon;

  const voterKey = user?.email ?? "anon";
  const votes = article.votes ?? {};
  const likeCount = Object.values(votes).filter((v) => v === "like").length;
  const dislikeCount = Object.values(votes).filter((v) => v === "dislike").length;
  const myVote = votes[voterKey];
  const comments = article.comments ?? [];

  const postComment = () => {
    if (!comment.trim()) return;
    hapticTap();
    addComment(article.id, user?.name ?? "Agent", comment.trim());
    setComment("");
  };

  return (
    <>
      <div className="fixed top-0 right-0 bottom-0 w-full max-w-md z-[56] flex flex-col bg-white border-l" style={{ borderColor: LINE, boxShadow: "-4px 0 40px rgba(30,42,71,0.12)" }}>
        <div className="flex items-center justify-between px-7 py-5 border-b shrink-0" style={{ borderColor: LINE }}>
          <div className="min-w-0">
            <p className="text-[10px] tracking-[0.16em] uppercase font-semibold" style={{ color: MUTED }}>Knowledge Base Article</p>
            <div className="flex items-center gap-2 mt-1">
              <span className="text-[10px] font-semibold px-2 py-0.5 rounded" style={{ backgroundColor: m.bg, color: m.text }}>{article.category}</span>
              <span className="text-[10px] font-semibold px-2 py-0.5 rounded-full" style={{ backgroundColor: article.status === "Published" ? "#D1FAE5" : "#F3F4F6", color: article.status === "Published" ? "#065F46" : "#374151" }}>{article.status}</span>
            </div>
          </div>
          <button onClick={onClose} className="w-8 h-8 rounded-full flex items-center justify-center hover:bg-gray-100 transition-colors shrink-0"><X size={18} /></button>
        </div>

        <div className="flex-1 overflow-y-auto px-7 py-6">
          <div className="flex items-start gap-3 mb-5">
            <div className="w-11 h-11 rounded-xl flex items-center justify-center shrink-0" style={{ backgroundColor: m.bg }}><Icon size={18} style={{ color: m.dot }} /></div>
            <h3 className="text-xl font-semibold leading-snug" style={{ color: INK, fontFamily: "Playfair Display, serif" }}>{article.title}</h3>
          </div>

          <div className="flex items-center gap-6 mb-6 pb-6 border-b" style={{ borderColor: LINE }}>
            <div className="flex items-center gap-2"><Eye size={15} style={{ color: MUTED }} /><span className="text-sm font-semibold" style={{ color: INK }}>{article.views.toLocaleString()}</span><span className="text-xs" style={{ color: MUTED }}>views</span></div>
            <div className="flex items-center gap-2"><CheckCircle2 size={15} style={{ color: "#22C55E" }} /><span className="text-sm font-semibold" style={{ color: "#22C55E" }}>{article.helpful}</span><span className="text-xs" style={{ color: MUTED }}>found helpful</span></div>
            <div className="flex items-center gap-2"><Clock size={15} style={{ color: MUTED }} /><span className="text-xs" style={{ color: SUB }}>Updated {article.lastUpdated}</span></div>
          </div>

          {article.heroImage && (
            <div className="rounded-xl overflow-hidden mb-6 border" style={{ borderColor: LINE }}>
              <ImageWithFallback src={article.heroImage} alt={article.title} className="w-full h-48 object-cover" />
            </div>
          )}

          <p className="text-[15px] leading-[26px] whitespace-pre-line" style={{ color: SUB }}>{article.content}</p>

          {article.steps && article.steps.length > 0 && (
            <div className="mt-7 pt-6 border-t" style={{ borderColor: LINE }}>
              <div className="flex items-center gap-2 mb-4">
                <div className="w-7 h-7 rounded-lg flex items-center justify-center" style={{ backgroundColor: SKY_BG }}><Zap size={14} style={{ color: SKY }} /></div>
                <h4 className="text-sm font-semibold tracking-wide uppercase" style={{ color: INK }}>How agents resolve this — step by step</h4>
              </div>
              <ol className="space-y-4">
                {article.steps.map((step, i) => (
                  <li key={i} className="flex gap-3">
                    <div className="w-7 h-7 rounded-full flex items-center justify-center shrink-0 text-xs font-bold text-white" style={{ backgroundColor: INK }}>{i + 1}</div>
                    <div className="flex-1 min-w-0 space-y-2.5">
                      <p className="text-[14px] leading-[22px]" style={{ color: SUB }}>{step.detail}</p>
                      {step.image && (
                        <div className="rounded-lg overflow-hidden border" style={{ borderColor: LINE }}>
                          <ImageWithFallback src={step.image} alt={`Step ${i + 1}`} className="w-full h-40 object-cover" />
                        </div>
                      )}
                    </div>
                  </li>
                ))}
              </ol>
            </div>
          )}

          {/* Reactions */}
          <div className="mt-7 pt-6 border-t flex items-center gap-3" style={{ borderColor: LINE }}>
            <p className="text-xs font-semibold mr-1" style={{ color: SUB }}>Was this helpful?</p>
            <button onClick={() => { hapticTap(); voteArticle(article.id, voterKey, "like"); }}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm font-medium border transition-colors"
              style={{ borderColor: myVote === "like" ? "#22C55E" : LINE, backgroundColor: myVote === "like" ? "#D1FAE5" : "transparent", color: myVote === "like" ? "#065F46" : SUB }}>
              <ThumbsUp size={14} /> {likeCount}
            </button>
            <button onClick={() => { hapticTap(); voteArticle(article.id, voterKey, "dislike"); }}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm font-medium border transition-colors"
              style={{ borderColor: myVote === "dislike" ? "#EF4444" : LINE, backgroundColor: myVote === "dislike" ? "#FEE2E2" : "transparent", color: myVote === "dislike" ? "#991B1B" : SUB }}>
              <ThumbsDown size={14} /> {dislikeCount}
            </button>
          </div>

          {/* Comments */}
          <div className="mt-6 pt-6 border-t" style={{ borderColor: LINE }}>
            <div className="flex items-center gap-2 mb-4">
              <MessageSquare size={15} style={{ color: SKY }} />
              <h4 className="text-sm font-semibold" style={{ color: INK }}>Agent notes &amp; comments ({comments.length})</h4>
            </div>
            <div className="space-y-3 mb-4">
              {comments.length === 0 && <p className="text-xs italic" style={{ color: MUTED }}>No comments yet — share a tip for the team.</p>}
              {comments.map((c) => (
                <div key={c.id} className="flex gap-3">
                  <div className="w-8 h-8 rounded-full flex items-center justify-center shrink-0 text-[11px] font-semibold text-white" style={{ backgroundColor: INK }}>
                    {c.author.split(" ").map((n) => n[0]).join("").slice(0, 2)}
                  </div>
                  <div className="flex-1 min-w-0 rounded-xl px-3.5 py-2.5" style={{ backgroundColor: SURF, border: `1px solid ${LINE}` }}>
                    <div className="flex items-center justify-between gap-2 mb-0.5">
                      <span className="text-xs font-semibold" style={{ color: INK }}>{c.author}</span>
                      <span className="text-[10px]" style={{ color: MUTED }}>{timeAgo(c.timestamp)}</span>
                    </div>
                    <p className="text-sm leading-relaxed" style={{ color: SUB }}>{c.text}</p>
                  </div>
                </div>
              ))}
              <div ref={endRef} />
            </div>
            <div className="flex gap-2">
              <input className="flex-1 border rounded-xl px-4 py-2.5 text-sm outline-none focus:border-[#0088CC] transition-colors" style={{ borderColor: LINE, color: INK }}
                placeholder="Add a comment for the team…" value={comment} onChange={(e) => setComment(e.target.value)}
                onKeyDown={(e) => { if (e.key === "Enter" && !e.shiftKey) { e.preventDefault(); postComment(); } }} />
              <button onClick={postComment} disabled={!comment.trim()} className="px-4 py-2.5 rounded-xl font-medium flex items-center gap-2 text-sm disabled:opacity-40 hover:opacity-90 transition-opacity" style={{ backgroundColor: INK, color: "#FFFFFF" }}>
                <Send size={15} /> Post
              </button>
            </div>
          </div>
        </div>

        <div className="px-7 py-4 border-t flex items-center justify-between shrink-0" style={{ borderColor: LINE }}>
          <button onClick={() => { hapticTap(); window.open("/help", "_blank"); }} className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium border transition-colors hover:bg-[#EEF3FB]" style={{ borderColor: LINE, color: SUB }}>
            <ArrowUpRight size={14} /> Open in Help Center
          </button>
          {onEdit && (
            <button onClick={() => { hapticTap(); onEdit(); }} className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-medium hover:opacity-90 transition-opacity" style={{ backgroundColor: INK, color: "#FFFFFF" }}>
              <Edit2 size={14} /> Edit Article
            </button>
          )}
        </div>
      </div>
    </>
  );
}

/* ─── KB Quick-open panel (usable from Tickets & Live Chat) ──────────────────── */
function KBQuickPanel({ onClose }: { onClose: () => void }) {
  const { articles } = useKB();
  const [search, setSearch] = useState("");
  const [viewId, setViewId] = useState<number | null>(null);

  const results = articles.filter((a) => {
    const q = search.toLowerCase();
    return a.status === "Published" && (a.title.toLowerCase().includes(q) || a.content.toLowerCase().includes(q) || a.category.toLowerCase().includes(q));
  });

  return (
    <>
      <div className="fixed top-0 right-0 bottom-0 w-full max-w-md z-[45] flex flex-col bg-white border-l" style={{ borderColor: LINE, boxShadow: "-4px 0 40px rgba(30,42,71,0.12)" }}>
        <div className="flex items-center justify-between px-6 py-5 border-b shrink-0" style={{ borderColor: LINE }}>
          <div>
            <p className="text-[10px] tracking-[0.16em] uppercase font-semibold" style={{ color: MUTED }}>Quick help</p>
            <h3 className="text-lg font-semibold mt-0.5" style={{ color: INK, fontFamily: "Playfair Display, serif" }}>Knowledge Base</h3>
          </div>
          <button onClick={onClose} className="w-8 h-8 rounded-full flex items-center justify-center hover:bg-gray-100 transition-colors"><X size={18} /></button>
        </div>
        <div className="px-6 py-4 border-b shrink-0" style={{ borderColor: LINE }}>
          <div className="relative">
            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2" style={{ color: MUTED }} />
            <input autoFocus className="w-full border rounded-lg pl-9 pr-4 py-2 text-sm outline-none focus:border-[#0088CC] transition-colors" style={{ borderColor: LINE, color: INK, backgroundColor: SURF }}
              placeholder="Search articles while you work…" value={search} onChange={(e) => setSearch(e.target.value)} />
          </div>
        </div>
        <div className="flex-1 overflow-y-auto px-6 py-4 space-y-2">
          {results.map((a) => {
            const m = CAT_META[a.category]; const Icon = m.icon;
            return (
              <button key={a.id} onClick={() => { hapticTap(); setViewId(a.id); }} className="w-full flex items-center gap-3 p-3 rounded-xl border text-left hover:shadow-sm transition-all" style={{ backgroundColor: "#FFFFFF", borderColor: LINE }}>
                <div className="w-8 h-8 rounded-lg flex items-center justify-center shrink-0" style={{ backgroundColor: m.bg }}><Icon size={14} style={{ color: m.dot }} /></div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium truncate" style={{ color: INK }}>{a.title}</p>
                  <div className="flex items-center gap-2 mt-0.5">
                    <span className="text-[10px]" style={{ color: MUTED }}>{a.category}</span>
                    {a.steps && a.steps.length > 0 && <span className="inline-flex items-center gap-1 text-[10px] font-semibold" style={{ color: SKY }}><Zap size={9} /> {a.steps.length}-step guide</span>}
                  </div>
                </div>
                <Eye size={14} className="shrink-0" style={{ color: MUTED }} />
              </button>
            );
          })}
          {results.length === 0 && (
            <div className="py-16 text-center"><BookOpen size={28} className="mx-auto mb-3" style={{ color: LINE }} /><p className="text-sm" style={{ color: SUB }}>No published articles match.</p></div>
          )}
        </div>
      </div>
      {viewId !== null && <ArticleView articleId={viewId} onClose={() => setViewId(null)} />}
    </>
  );
}

function KBButton({ onClick }: { onClick: () => void }) {
  return (
    <button onClick={onClick} className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium border transition-colors hover:bg-[#EEF3FB]" style={{ borderColor: LINE, color: INK }}>
      <BookOpen size={15} style={{ color: SKY }} /> Knowledge Base
    </button>
  );
}

/* ─── Knowledge Base Tab ─────────────────────────────────────────────────────── */
const ARTICLES_PER_PAGE = 10;

function KnowledgeBase() {
  const { articles, addArticle, updateArticle, deleteArticle, toggleStatus } = useKB();
  const [search, setSearch]         = useState("");
  const [searchOpen, setSearchOpen] = useState(false);
  const [catFilter, setCatFilter]   = useState<string>("All");
  const [statusFilter, setStatusFilter] = useState<string>("All");
  const [page, setPage]             = useState(1);
  const [panel, setPanel]           = useState<KBArticle | "new" | null>(null);
  const [viewId, setViewId]         = useState<number | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<KBArticle | null>(null);
  const [toast, setToast]           = useState<string | null>(null);
  const searchRef = useRef<HTMLInputElement>(null);

  const filtered = articles.filter((a) => {
    const q = search.toLowerCase();
    return (a.title.toLowerCase().includes(q) || a.content.toLowerCase().includes(q)) &&
      (catFilter === "All" || a.category === catFilter) &&
      (statusFilter === "All" || a.status === statusFilter);
  });

  // Reset to the first page whenever the result set changes.
  useEffect(() => { setPage(1); }, [search, catFilter, statusFilter]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / ARTICLES_PER_PAGE));
  const currentPage = Math.min(page, totalPages);
  const pageItems = filtered.slice((currentPage - 1) * ARTICLES_PER_PAGE, currentPage * ARTICLES_PER_PAGE);

  const openSearch = () => { setSearchOpen(true); setTimeout(() => searchRef.current?.focus(), 50); };
  const closeSearch = () => { setSearchOpen(false); setSearch(""); };

  const saveArticle = (data: Partial<KBArticle>) => {
    if (panel === "new") { addArticle(data); setToast("Article created"); }
    else if (panel) { updateArticle((panel as KBArticle).id, data); setToast("Article updated"); }
    setPanel(null);
  };

  const published = articles.filter((a) => a.status === "Published").length;
  const totalViews = articles.reduce((s, a) => s + a.views, 0);
  const catStats = KB_CATS.map((cat) => ({ cat, total: articles.filter((a) => a.category === cat).length, pub: articles.filter((a) => a.category === cat && a.status === "Published").length }));

  return (
    <div>
      <div className="flex items-start justify-between mb-6">
        <div>
          <p className="text-[10px] tracking-[0.18em] uppercase font-semibold mb-1" style={{ color: MUTED }}>Customer Support</p>
          <h2 className="text-2xl font-semibold" style={{ color: INK, fontFamily: "Playfair Display, serif" }}>Knowledge Base</h2>
          <p className="text-sm mt-1" style={{ color: SUB }}>Manage help articles and agent playbooks</p>
        </div>
        <div className="flex gap-2">
          <button onClick={() => { hapticTap(); window.open("/help", "_blank"); }} className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium border transition-colors hover:bg-[#EEF3FB]" style={{ borderColor: LINE, color: SUB }}>
            <ArrowUpRight size={14} /> View Help Center
          </button>
          <button onClick={() => { hapticTap(); setPanel("new"); }} className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-medium hover:opacity-90 transition-opacity" style={{ backgroundColor: INK, color: "#FFFFFF" }}>
            <Plus size={15} /> New Article
          </button>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {[
          { label: "Total Articles", value: String(articles.length), icon: BookOpen, color: INK },
          { label: "Published", value: String(published), icon: Globe, color: "#22C55E" },
          { label: "Drafts", value: String(articles.length - published), icon: FileText, color: "#F59E0B" },
          { label: "Total Views", value: totalViews.toLocaleString(), icon: Eye, color: SKY },
        ].map(({ label, value, icon: Icon, color }) => (
          <div key={label} className="p-4 rounded-xl flex items-center gap-4" style={{ backgroundColor: SURF, border: `1px solid ${LINE}` }}>
            <div className="w-10 h-10 rounded-lg flex items-center justify-center" style={{ backgroundColor: `${color}18` }}><Icon size={18} style={{ color }} /></div>
            <div>
              <p className="text-xl font-bold" style={{ color: INK }}>{value}</p>
              <p className="text-xs" style={{ color: SUB }}>{label}</p>
            </div>
          </div>
        ))}
      </div>

      {/* Category carousel — scrolls horizontally, contained so it never widens the page */}
      <div className="w-full max-w-full overflow-x-auto overflow-y-hidden mb-6">
        <div className="flex gap-3 pb-1 w-max">
          {catStats.map(({ cat, total, pub }) => {
            const m = CAT_META[cat]; const Icon = m.icon; const active = catFilter === cat;
            return (
              <button key={cat} onClick={() => { hapticTap(); setCatFilter(active ? "All" : cat); }} className="flex items-center gap-3 p-3.5 rounded-xl border text-left transition-all hover:shadow-sm shrink-0 w-60" style={{ backgroundColor: active ? m.bg : "#FFFFFF", borderColor: active ? m.dot : LINE }}>
                <div className="w-9 h-9 rounded-lg flex items-center justify-center shrink-0" style={{ backgroundColor: m.bg }}><Icon size={16} style={{ color: m.dot }} /></div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold truncate" style={{ color: INK }}>{cat}</p>
                  <p className="text-xs" style={{ color: MUTED }}>{pub}/{total} published</p>
                </div>
              </button>
            );
          })}
        </div>
      </div>

      {/* Filters */}
      <div className="flex items-center gap-3 mb-5">
        <div className="flex items-center gap-2">
          {["All", "Published", "Draft"].map((s) => (
            <button key={s} onClick={() => { hapticTap(); setStatusFilter(s); }} className="px-3.5 py-1.5 text-xs font-medium rounded-full border transition-all" style={{ borderColor: statusFilter === s ? INK : LINE, backgroundColor: statusFilter === s ? "#EEF3FB" : "transparent", color: statusFilter === s ? INK : SUB }}>{s}</button>
          ))}
        </div>

        <div className="flex-1" />

        {/* Collapsible search — icon that expands into an input */}
        {searchOpen ? (
          <div className="flex items-center gap-1.5 border rounded-full pl-3.5 pr-1.5 py-1.5" style={{ borderColor: SKY, backgroundColor: "#FFFFFF" }}>
            <Search size={14} style={{ color: SKY }} />
            <input ref={searchRef} className="w-40 sm:w-56 text-sm outline-none bg-transparent" style={{ color: INK }}
              placeholder="Search articles…" value={search} onChange={(e) => setSearch(e.target.value)}
              onKeyDown={(e) => { if (e.key === "Escape") closeSearch(); }} />
            <button onClick={() => { hapticTap(); closeSearch(); }} className="w-6 h-6 rounded-full flex items-center justify-center hover:bg-gray-100 transition-colors"><X size={13} style={{ color: SUB }} /></button>
          </div>
        ) : (
          <button onClick={() => { hapticTap(); openSearch(); }} className="w-9 h-9 rounded-full border flex items-center justify-center transition-colors hover:bg-[#EEF3FB]" style={{ borderColor: LINE }} title="Search articles">
            <Search size={16} style={{ color: SUB }} />
          </button>
        )}
      </div>

      {/* Article list */}
      <div className="space-y-2">
        {pageItems.map((article) => {
          const m = CAT_META[article.category]; const Icon = m.icon;
          const votes = article.votes ?? {};
          const likeCount = Object.values(votes).filter((v) => v === "like").length;
          const commentCount = (article.comments ?? []).length;
          return (
            <div key={article.id} className="group flex items-center gap-4 p-3.5 rounded-xl border hover:shadow-sm transition-all" style={{ backgroundColor: "#FFFFFF", borderColor: LINE }}>
              <div className="w-9 h-9 rounded-lg flex items-center justify-center shrink-0" style={{ backgroundColor: m.bg }}><Icon size={15} style={{ color: m.dot }} /></div>
              <button onClick={() => { hapticTap(); setViewId(article.id); }} className="flex-1 min-w-0 text-left">
                <div className="flex items-center gap-2 mb-0.5 flex-wrap">
                  <span className="text-[10px] font-semibold px-2 py-0.5 rounded" style={{ backgroundColor: m.bg, color: m.text }}>{article.category}</span>
                  <span className="text-[10px] font-semibold px-2 py-0.5 rounded-full" style={{ backgroundColor: article.status === "Published" ? "#D1FAE5" : "#F3F4F6", color: article.status === "Published" ? "#065F46" : "#374151" }}>{article.status}</span>
                  {article.steps && article.steps.length > 0 && (
                    <span className="inline-flex items-center gap-1 text-[10px] font-semibold px-2 py-0.5 rounded-full" style={{ backgroundColor: SKY_BG, color: SKY }}><Zap size={10} /> {article.steps.length}-step guide</span>
                  )}
                </div>
                <p className="text-sm font-medium truncate hover:underline" style={{ color: INK }}>{article.title}</p>
                <p className="text-xs mt-0.5 flex items-center gap-3" style={{ color: MUTED }}>
                  <span>Updated {article.lastUpdated}</span>
                  {likeCount > 0 && <span className="inline-flex items-center gap-1"><ThumbsUp size={10} /> {likeCount}</span>}
                  {commentCount > 0 && <span className="inline-flex items-center gap-1"><MessageSquare size={10} /> {commentCount}</span>}
                </p>
              </button>
              <div className="hidden lg:flex items-center gap-6 shrink-0">
                <div className="text-right"><p className="text-sm font-semibold" style={{ color: INK }}>{article.views.toLocaleString()}</p><p className="text-xs" style={{ color: MUTED }}>views</p></div>
                <div className="text-right"><p className="text-sm font-semibold" style={{ color: "#22C55E" }}>{article.helpful}</p><p className="text-xs" style={{ color: MUTED }}>helpful</p></div>
              </div>
              <div className="flex items-center gap-1 shrink-0 opacity-0 group-hover:opacity-100 transition-opacity">
                <button onClick={() => { hapticTap(); setViewId(article.id); }} className="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-[#EEF3FB] transition-colors" title="View"><Eye size={14} style={{ color: SUB }} /></button>
                <button onClick={() => { hapticTap(); setPanel(article); }} className="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-[#EEF3FB] transition-colors" title="Edit"><Edit2 size={14} style={{ color: SUB }} /></button>
                <button onClick={() => { hapticTap(); toggleStatus(article.id); setToast("Article status updated"); }} className="px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors" style={{ borderColor: article.status === "Published" ? LINE : INK, backgroundColor: article.status === "Published" ? "transparent" : "#EEF3FB", color: article.status === "Published" ? SUB : INK }}>
                  {article.status === "Published" ? "Unpublish" : "Publish"}
                </button>
                <button onClick={() => { hapticTap(); setDeleteTarget(article); }} className="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-red-50 transition-colors" title="Delete"><Trash2 size={14} style={{ color: "#C44545" }} /></button>
              </div>
            </div>
          );
        })}
        {filtered.length === 0 && (
          <div className="py-16 text-center">
            <BookOpen size={32} className="mx-auto mb-3" style={{ color: LINE }} />
            <p className="text-sm" style={{ color: SUB }}>No articles match your filters.</p>
          </div>
        )}
      </div>

      {/* Pagination — up to 10 articles per page */}
      {filtered.length > 0 && (
        <div className="flex items-center justify-between gap-4 mt-6 pt-4 border-t" style={{ borderColor: LINE }}>
          <p className="text-xs" style={{ color: MUTED }}>
            Showing {(currentPage - 1) * ARTICLES_PER_PAGE + 1}–{Math.min(currentPage * ARTICLES_PER_PAGE, filtered.length)} of {filtered.length}
          </p>
          <div className="flex items-center gap-1.5">
            <button onClick={() => { hapticTap(); setPage((p) => Math.max(1, p - 1)); }} disabled={currentPage === 1}
              className="w-8 h-8 rounded-lg border flex items-center justify-center transition-colors disabled:opacity-40 disabled:cursor-not-allowed hover:bg-[#EEF3FB]" style={{ borderColor: LINE }} title="Previous page">
              <ChevronLeft size={15} style={{ color: SUB }} />
            </button>
            {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
              <button key={p} onClick={() => { hapticTap(); setPage(p); }} className="min-w-8 h-8 px-2.5 rounded-lg text-sm font-medium border transition-all"
                style={{ borderColor: p === currentPage ? INK : LINE, backgroundColor: p === currentPage ? INK : "transparent", color: p === currentPage ? "#FFFFFF" : SUB }}>
                {p}
              </button>
            ))}
            <button onClick={() => { hapticTap(); setPage((p) => Math.min(totalPages, p + 1)); }} disabled={currentPage === totalPages}
              className="w-8 h-8 rounded-lg border flex items-center justify-center transition-colors disabled:opacity-40 disabled:cursor-not-allowed hover:bg-[#EEF3FB]" style={{ borderColor: LINE }} title="Next page">
              <ChevronRight size={15} style={{ color: SUB }} />
            </button>
          </div>
        </div>
      )}

      {viewId !== null && <ArticleView articleId={viewId} onClose={() => setViewId(null)} onEdit={() => { const a = articles.find((x) => x.id === viewId); setViewId(null); if (a) setPanel(a); }} />}
      {panel && <ArticlePanel initial={panel === "new" ? undefined : panel as KBArticle} onSave={saveArticle} onClose={() => setPanel(null)} />}
      {deleteTarget && <ConfirmDelete label={deleteTarget.title} onConfirm={() => { deleteArticle(deleteTarget.id); setDeleteTarget(null); setToast("Article deleted"); }} onCancel={() => setDeleteTarget(null)} />}
      {toast && <Toast message={toast} onDone={() => setToast(null)} />}
    </div>
  );
}

/* ─── Tickets ────────────────────────────────────────────────────────────────── */
const TICKET_STATUSES: Ticket["status"][] = ["Open", "In Progress", "Resolved"];
const TICKET_PRIORITIES: Ticket["priority"][] = ["Low", "Normal", "High"];
const TEAMS = ["Unassigned", "Support Team", "Billing Team", "Shipping Team", "Escalations"];
const AGENTS = ["Unassigned", "Sarah Chen", "Priya Sharma", "Alex Morgan", "Marcus Ali"];

const TICKET_SS: Record<string, { bg: string; text: string; dot: string }> = {
  "Open":        { bg: "#DBEAFE", text: "#1E3A8A", dot: "#3B82F6" },
  "In Progress": { bg: "#FEF3C7", text: "#92400E", dot: "#F59E0B" },
  "Resolved":    { bg: "#D1FAE5", text: "#065F46", dot: "#22C55E" },
};
const TICKET_PS: Record<string, { bg: string; text: string }> = {
  High:   { bg: "#FEE2E2", text: "#991B1B" },
  Normal: { bg: "#FEF3C7", text: "#92400E" },
  Low:    { bg: "#F3F4F6", text: "#374151" },
};

function TicketDrawer({ ticket, onClose, onToast, onOpenKB, shifted }: { ticket: Ticket; onClose: () => void; onToast: (m: string) => void; onOpenKB: () => void; shifted?: boolean }) {
  const { updateTicketStatus, updateTicketPriority, assignTicket, assignTeam, addReply } = useTickets();
  const { user } = useBackendAuth();
  const [reply, setReply] = useState("");
  const [showMacros, setShowMacros] = useState(false);
  const endRef = useRef<HTMLDivElement>(null);

  useEffect(() => { endRef.current?.scrollIntoView({ behavior: "smooth" }); }, [ticket.replies?.length]);

  const send = () => {
    if (!reply.trim()) return;
    hapticTap();
    addReply(ticket.id, { from: "agent", message: reply.trim() });
    setReply(""); setShowMacros(false);
    onToast("Reply sent to customer");
  };
  const applyMacro = (text: string) => { hapticTap(); setReply((prev) => (prev.trim() ? `${prev.trim()} ${text}` : text)); setShowMacros(false); };
  const assignToMe = () => { if (!user) return; hapticTap(); assignTicket(ticket.id, user.name); if (ticket.status === "Open") updateTicketStatus(ticket.id, "In Progress"); onToast("Assigned to you"); };
  const mine = !!user && ticket.assignedTo === user.name;
  const agentReplies = (ticket.replies ?? []).filter((r) => r.from === "agent").length;

  return (
    <>
      <div className="fixed top-0 bottom-0 w-full max-w-xl z-[41] flex flex-col bg-white border-l transition-[right] duration-300" style={{ right: shifted ? "28rem" : 0, borderColor: LINE, boxShadow: "-4px 0 40px rgba(30,42,71,0.12)" }}>
        <div className="flex items-center justify-between px-7 py-5 border-b shrink-0" style={{ borderColor: LINE }}>
          <div className="min-w-0">
            <p className="text-[10px] tracking-[0.16em] uppercase font-semibold" style={{ color: MUTED }}>{ticket.id} · {ticket.category}</p>
            <h3 className="text-lg font-semibold mt-0.5 truncate" style={{ color: INK, fontFamily: "Playfair Display, serif" }}>{ticket.subject}</h3>
          </div>
          <div className="flex items-center gap-1.5 shrink-0">
            <button onClick={() => { hapticTap(); onOpenKB(); }} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors hover:bg-[#EEF3FB]" style={{ borderColor: LINE, color: INK }} title="Open Knowledge Base">
              <BookOpen size={13} style={{ color: SKY }} /> KB
            </button>
            <button onClick={onClose} className="w-8 h-8 rounded-full flex items-center justify-center hover:bg-gray-100 transition-colors"><X size={18} /></button>
          </div>
        </div>

        <div className="flex-1 overflow-y-auto px-7 py-6 space-y-6">
          {/* Requester + controls */}
          <div className="p-4 rounded-xl space-y-3" style={{ backgroundColor: SURF, border: `1px solid ${LINE}` }}>
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-full flex items-center justify-center text-white text-sm font-semibold shrink-0" style={{ backgroundColor: INK }}>
                {ticket.name.split(" ").map((n) => n[0]).join("")}
              </div>
              <div className="min-w-0 flex-1">
                <p className="text-sm font-semibold truncate" style={{ color: INK }}>{ticket.name}</p>
                <p className="text-xs flex items-center gap-1 truncate" style={{ color: SUB }}><Mail size={11} /> {ticket.email}</p>
              </div>
              {!mine && (
                <button onClick={assignToMe} className="shrink-0 flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium hover:opacity-90 transition-opacity" style={{ backgroundColor: INK, color: "#FFFFFF" }}>
                  <UserCheck size={13} /> Assign to me
                </button>
              )}
            </div>

            <div className="flex items-center gap-2 flex-wrap">
              <span className="inline-flex items-center gap-1 text-[11px] font-medium px-2 py-1 rounded-full" style={{ backgroundColor: "#EEF1F8", color: SUB }}><Timer size={11} /> Opened {timeAgo(ticket.createdAt)}</span>
              {ticket.status !== "Resolved" && (
                <span className="inline-flex items-center gap-1 text-[11px] font-semibold px-2 py-1 rounded-full" style={{ backgroundColor: agentReplies > 0 ? "#D1FAE5" : "#FEE2E2", color: agentReplies > 0 ? "#065F46" : "#991B1B" }}>
                  {agentReplies > 0 ? "First response sent" : "Awaiting first response"}
                </span>
              )}
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-[10px] font-semibold uppercase tracking-wide mb-1" style={{ color: SUB }}>Status</label>
                <div className="relative">
                  <select value={ticket.status} onChange={(e) => { hapticTap(); updateTicketStatus(ticket.id, e.target.value as Ticket["status"]); onToast("Status updated"); }} className="w-full border rounded-lg px-2.5 py-2 text-xs outline-none appearance-none" style={{ borderColor: LINE, color: INK }}>
                    {TICKET_STATUSES.map((s) => <option key={s}>{s}</option>)}
                  </select>
                  <ChevronDown size={12} className="absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none" style={{ color: SUB }} />
                </div>
              </div>
              <div>
                <label className="block text-[10px] font-semibold uppercase tracking-wide mb-1" style={{ color: SUB }}>Priority</label>
                <div className="relative">
                  <select value={ticket.priority} onChange={(e) => { hapticTap(); updateTicketPriority(ticket.id, e.target.value as Ticket["priority"]); onToast("Priority updated"); }} className="w-full border rounded-lg px-2.5 py-2 text-xs outline-none appearance-none" style={{ borderColor: LINE, color: INK }}>
                    {TICKET_PRIORITIES.map((p) => <option key={p}>{p}</option>)}
                  </select>
                  <ChevronDown size={12} className="absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none" style={{ color: SUB }} />
                </div>
              </div>
              <div>
                <label className="block text-[10px] font-semibold uppercase tracking-wide mb-1" style={{ color: SUB }}>Team</label>
                <div className="relative">
                  <select value={ticket.team ?? "Unassigned"} onChange={(e) => { hapticTap(); assignTeam(ticket.id, e.target.value); onToast("Team assigned"); }} className="w-full border rounded-lg px-2.5 py-2 text-xs outline-none appearance-none" style={{ borderColor: LINE, color: INK }}>
                    {TEAMS.map((t) => <option key={t}>{t}</option>)}
                  </select>
                  <ChevronDown size={12} className="absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none" style={{ color: SUB }} />
                </div>
              </div>
              <div>
                <label className="block text-[10px] font-semibold uppercase tracking-wide mb-1" style={{ color: SUB }}>Agent</label>
                <div className="relative">
                  <select value={ticket.assignedTo ?? "Unassigned"} onChange={(e) => { hapticTap(); assignTicket(ticket.id, e.target.value); onToast("Agent assigned"); }} className="w-full border rounded-lg px-2.5 py-2 text-xs outline-none appearance-none" style={{ borderColor: LINE, color: INK }}>
                    {AGENTS.map((a) => <option key={a}>{a}</option>)}
                  </select>
                  <ChevronDown size={12} className="absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none" style={{ color: SUB }} />
                </div>
              </div>
            </div>
          </div>

          {/* Original message */}
          <div>
            <p className="text-[10px] font-semibold uppercase tracking-wide mb-2" style={{ color: SUB }}>Original message</p>
            <div className="p-4 rounded-xl text-sm leading-relaxed" style={{ backgroundColor: "#F0F4FB", color: SUB }}>{ticket.message}</div>
          </div>

          {/* Conversation */}
          <div>
            <p className="text-[10px] font-semibold uppercase tracking-wide mb-2" style={{ color: SUB }}>Conversation</p>
            <div className="space-y-3">
              {(ticket.replies ?? []).length === 0 && <p className="text-xs italic" style={{ color: MUTED }}>No replies yet.</p>}
              {(ticket.replies ?? []).map((r, i) => (
                <div key={i} className={`flex ${r.from === "agent" ? "justify-end" : "justify-start"}`}>
                  <div className="max-w-[80%] px-4 py-2.5 rounded-2xl" style={{ backgroundColor: r.from === "agent" ? INK : "#F0F4FB", color: r.from === "agent" ? "#FFFFFF" : INK }}>
                    <p className="text-sm">{r.message}</p>
                    <span className="text-[10px] mt-1 block" style={{ opacity: 0.6 }}>{fmtDate(r.timestamp)}</span>
                  </div>
                </div>
              ))}
              <div ref={endRef} />
            </div>
          </div>
        </div>

        {/* Reply box */}
        <div className="px-7 py-4 border-t shrink-0 space-y-2.5" style={{ borderColor: LINE }}>
          <div className="flex items-center gap-2">
            <button onClick={() => { hapticTap(); setShowMacros((s) => !s); }} className="shrink-0 flex items-center gap-1 text-xs font-medium px-2.5 py-1.5 rounded-lg transition-colors" style={{ backgroundColor: showMacros ? INK : SKY_BG, color: showMacros ? "#FFFFFF" : SKY }}>
              <Zap size={12} /> Macros
            </button>
            {showMacros && (
              <div className="flex gap-1.5 overflow-x-auto pb-0.5">
                {CANNED_REPLIES.map((mm) => (
                  <button key={mm.label} onClick={() => applyMacro(mm.text)} className="shrink-0 text-xs font-medium px-2.5 py-1.5 rounded-lg border hover:bg-gray-50 transition-colors whitespace-nowrap" style={{ borderColor: LINE, color: INK }}>{mm.label}</button>
                ))}
              </div>
            )}
          </div>
          <div className="flex gap-2">
            <input className="flex-1 border rounded-xl px-4 py-2.5 text-sm outline-none focus:border-[#0088CC] transition-colors" style={{ borderColor: LINE, color: INK }}
              placeholder="Write a reply to the customer…" value={reply} onChange={(e) => setReply(e.target.value)}
              onKeyDown={(e) => { if (e.key === "Enter" && !e.shiftKey) { e.preventDefault(); send(); } }} />
            <button onClick={send} disabled={!reply.trim()} className="px-5 py-2.5 rounded-xl font-medium flex items-center gap-2 text-sm disabled:opacity-40 hover:opacity-90 transition-opacity" style={{ backgroundColor: INK, color: "#FFFFFF" }}>
              <Send size={15} /> Reply
            </button>
          </div>
        </div>
      </div>
    </>
  );
}

function TicketsTab() {
  const { tickets } = useTickets();
  const { user } = useBackendAuth();
  const [search, setSearch] = useState("");
  const [filterStatus, setFilterStatus] = useState("All");
  const [filterPriority, setFilterPriority] = useState("All");
  const [mineOnly, setMineOnly] = useState(false);
  const [openId, setOpenId] = useState<string | null>(null);
  const [showKB, setShowKB] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  const filtered = tickets.filter((t) => {
    const q = search.toLowerCase();
    return (t.subject.toLowerCase().includes(q) || t.name.toLowerCase().includes(q) || t.id.toLowerCase().includes(q)) &&
      (filterStatus === "All" || t.status === filterStatus) &&
      (filterPriority === "All" || t.priority === filterPriority) &&
      (!mineOnly || (!!user && t.assignedTo === user.name));
  });

  const open = tickets.filter((t) => t.status === "Open").length;
  const inProgress = tickets.filter((t) => t.status === "In Progress").length;
  const resolved = tickets.filter((t) => t.status === "Resolved").length;
  const mineCount = user ? tickets.filter((t) => t.assignedTo === user.name).length : 0;
  const active = tickets.find((t) => t.id === openId) ?? null;

  return (
    <div>
      <div className="flex items-start justify-between mb-6">
        <div>
          <p className="text-[10px] tracking-[0.18em] uppercase font-semibold mb-1" style={{ color: MUTED }}>Customer Support</p>
          <h2 className="text-2xl font-semibold" style={{ color: INK, fontFamily: "Playfair Display, serif" }}>Support Tickets</h2>
          <p className="text-sm mt-1" style={{ color: SUB }}>Route to a team, assign an agent, and resolve fast</p>
        </div>
        <KBButton onClick={() => { hapticTap(); setShowKB(true); }} />
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {[
          { label: "Total", value: tickets.length, color: INK },
          { label: "Open", value: open, color: "#3B82F6" },
          { label: "In Progress", value: inProgress, color: "#F59E0B" },
          { label: "Resolved", value: resolved, color: "#22C55E" },
        ].map(({ label, value, color }) => (
          <div key={label} className="p-4 rounded-xl" style={{ backgroundColor: SURF, border: `1px solid ${LINE}` }}>
            <p className="text-2xl font-bold" style={{ color }}>{value}</p>
            <p className="text-xs mt-0.5" style={{ color: SUB }}>{label}</p>
          </div>
        ))}
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-5">
        <div className="flex-1 min-w-48 relative">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2" style={{ color: MUTED }} />
          <input className="w-full border rounded-lg pl-9 pr-4 py-2 text-sm outline-none focus:border-[#0088CC] transition-colors" style={{ borderColor: LINE, color: INK, backgroundColor: "#FFFFFF" }}
            placeholder="Search tickets…" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
        <button onClick={() => { hapticTap(); setMineOnly((v) => !v); }} className="px-3 py-1.5 text-xs font-medium rounded-full border transition-all flex items-center gap-1.5" style={{ borderColor: mineOnly ? INK : LINE, backgroundColor: mineOnly ? INK : "transparent", color: mineOnly ? "#FFFFFF" : SUB }}>
          <UserCheck size={12} /> Assigned to me ({mineCount})
        </button>
        <div className="flex gap-2">
          {["All", ...TICKET_STATUSES].map((s) => (
            <button key={s} onClick={() => { hapticTap(); setFilterStatus(s); }} className="px-3 py-1.5 text-xs font-medium rounded-full border transition-all whitespace-nowrap" style={{ borderColor: filterStatus === s ? INK : LINE, backgroundColor: filterStatus === s ? INK : "transparent", color: filterStatus === s ? "#FFFFFF" : SUB }}>{s}</button>
          ))}
        </div>
        <div className="flex gap-2">
          {["All", ...TICKET_PRIORITIES].map((p) => (
            <button key={p} onClick={() => { hapticTap(); setFilterPriority(p); }} className="px-3 py-1.5 text-xs font-medium rounded-full border transition-all" style={{ borderColor: filterPriority === p ? INK : LINE, backgroundColor: filterPriority === p ? "#EEF3FB" : "transparent", color: filterPriority === p ? INK : SUB }}>{p}</button>
          ))}
        </div>
      </div>

      {/* Ticket list */}
      <div className="space-y-2">
        {filtered.map((ticket) => {
          const ss = TICKET_SS[ticket.status] ?? TICKET_SS.Open;
          const ps = TICKET_PS[ticket.priority] ?? TICKET_PS.Low;
          const replyCount = (ticket.replies ?? []).length;
          return (
            <button key={ticket.id} onClick={() => { hapticTap(); setOpenId(ticket.id); }} className="w-full group flex items-center gap-4 p-3.5 rounded-xl border hover:shadow-sm transition-all text-left" style={{ backgroundColor: "#FFFFFF", borderColor: LINE }}>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-1 flex-wrap">
                  <span className="font-mono text-xs" style={{ color: MUTED }}>{ticket.id}</span>
                  <span className="text-[10px] font-bold px-2 py-0.5 rounded-full" style={{ backgroundColor: ps.bg, color: ps.text }}>{ticket.priority.toUpperCase()}</span>
                  <span className="inline-flex items-center gap-1 text-[10px] font-semibold px-2 py-0.5 rounded-full" style={{ backgroundColor: ss.bg, color: ss.text }}>
                    <span className="w-1.5 h-1.5 rounded-full" style={{ backgroundColor: ss.dot }} />{ticket.status}
                  </span>
                  {ticket.team && ticket.team !== "Unassigned" && (
                    <span className="inline-flex items-center gap-1 text-[10px] font-medium px-2 py-0.5 rounded-full" style={{ backgroundColor: SKY_BG, color: SKY }}><Users size={10} /> {ticket.team}</span>
                  )}
                  {ticket.assignedTo && ticket.assignedTo !== "Unassigned" && (
                    <span className="inline-flex items-center gap-1 text-[10px] font-medium px-2 py-0.5 rounded-full" style={{ backgroundColor: "#EEF1F8", color: SUB }}><UserCheck size={10} /> {ticket.assignedTo}</span>
                  )}
                </div>
                <p className="text-sm font-medium truncate" style={{ color: INK }}>{ticket.subject}</p>
                <p className="text-xs mt-0.5 flex items-center gap-2" style={{ color: MUTED }}>
                  <span>{ticket.name}</span> · <span>{ticket.category}</span> · <span>{fmtDate(ticket.createdAt)}</span>
                  {replyCount > 0 && <span className="inline-flex items-center gap-1"><MessageCircle size={11} /> {replyCount}</span>}
                </p>
              </div>
              <ArrowUpRight size={16} className="opacity-0 group-hover:opacity-100 transition-opacity shrink-0" style={{ color: SUB }} />
            </button>
          );
        })}
        {filtered.length === 0 && (
          <div className="py-16 text-center">
            <MessageCircle size={32} className="mx-auto mb-3" style={{ color: LINE }} />
            <p className="text-sm" style={{ color: SUB }}>No tickets match your filters.</p>
          </div>
        )}
      </div>

      {active && <TicketDrawer ticket={active} onClose={() => setOpenId(null)} onToast={(m) => setToast(m)} onOpenKB={() => setShowKB(true)} shifted={showKB} />}
      {showKB && <KBQuickPanel onClose={() => setShowKB(false)} />}
      {toast && <Toast message={toast} onDone={() => setToast(null)} />}
    </div>
  );
}

/* ─── Live Chat ──────────────────────────────────────────────────────────────── */
type Msg = { from: "customer" | "agent"; text: string; time: string };
const CONVERSATIONS = [
  { id: 1, name: "Sarah K.", waiting: "2 min", order: "#12345", status: "waiting" as const },
  { id: 2, name: "Marcus C.", waiting: "8 min", order: "#12342", status: "active" as const },
  { id: 3, name: "Elena R.", waiting: "15 min", order: "#12338", status: "waiting" as const },
];

function LiveChat() {
  const [convs, setConvs] = useState(CONVERSATIONS);
  const [activeId, setActiveId] = useState(CONVERSATIONS[0].id);
  const [messages, setMessages] = useState<Msg[]>([
    { from: "customer", text: "Hi, I haven't received my order yet. It's been 5 days.", time: "10:32 AM" },
    { from: "agent", text: "Hi Sarah! Let me check the status of your order #12345 for you.", time: "10:33 AM" },
    { from: "customer", text: "Thank you! It was supposed to arrive yesterday.", time: "10:34 AM" },
  ]);
  const [input, setInput] = useState("");
  const [showMacros, setShowMacros] = useState(false);
  const [showKB, setShowKB] = useState(false);
  const endRef = useRef<HTMLDivElement>(null);
  const activeConv = convs.find((c) => c.id === activeId);

  const applyMacro = (text: string) => { hapticTap(); setInput((prev) => (prev.trim() ? `${prev.trim()} ${text}` : text)); setShowMacros(false); };

  const sendMessage = () => {
    if (!input.trim()) return;
    hapticTap();
    const now = new Date().toLocaleTimeString("en-US", { hour: "2-digit", minute: "2-digit" });
    setMessages((prev) => [...prev, { from: "agent", text: input.trim(), time: now }]);
    setInput(""); setShowMacros(false);
    setTimeout(() => endRef.current?.scrollIntoView({ behavior: "smooth" }), 50);
  };
  const closeChat = (id: number) => {
    hapticTap();
    const remaining = convs.filter((c) => c.id !== id);
    setConvs(remaining);
    if (activeId === id && remaining.length > 0) setActiveId(remaining[0].id);
  };

  return (
    <div className="transition-[margin] duration-300" style={{ marginRight: showKB ? "28rem" : 0 }}>
      <div className="mb-6 flex items-start justify-between">
        <div>
          <p className="text-[10px] tracking-[0.18em] uppercase font-semibold mb-1" style={{ color: MUTED }}>Customer Support</p>
          <h2 className="text-2xl font-semibold" style={{ color: INK, fontFamily: "Playfair Display, serif" }}>Live Chat</h2>
          <p className="text-sm mt-1" style={{ color: SUB }}>Respond to customers in real time</p>
        </div>
        <KBButton onClick={() => { hapticTap(); setShowKB(true); }} />
      </div>

      <div className="grid grid-cols-3 gap-6 h-[calc(100vh-260px)]">
        <div className="col-span-1 rounded-xl border overflow-hidden" style={{ borderColor: LINE, backgroundColor: "#FFFFFF" }}>
          <div className="p-4 border-b" style={{ borderColor: LINE }}>
            <h3 className="text-sm font-semibold" style={{ color: INK }}>Active Conversations ({convs.length})</h3>
          </div>
          <div className="divide-y overflow-y-auto" style={{ borderColor: LINE }}>
            {convs.map((conv) => (
              <button key={conv.id} onClick={() => { hapticTap(); setActiveId(conv.id); }} className="w-full p-4 text-left transition-colors" style={{ backgroundColor: activeId === conv.id ? "#EEF3FB" : "transparent" }}>
                <div className="flex items-center justify-between mb-1">
                  <span className="text-sm font-semibold" style={{ color: INK }}>{conv.name}</span>
                  <span className="text-xs" style={{ color: MUTED }}>{conv.waiting}</span>
                </div>
                <p className="text-xs" style={{ color: SUB }}>Order {conv.order}</p>
                <div className="flex items-center gap-2 mt-2">
                  <span className="w-2 h-2 rounded-full" style={{ backgroundColor: conv.status === "active" ? "#22C55E" : "#F59E0B" }} />
                  <span className="text-xs capitalize" style={{ color: SUB }}>{conv.status}</span>
                </div>
              </button>
            ))}
            {convs.length === 0 && (
              <div className="py-10 text-center"><MessageCircle size={24} className="mx-auto mb-2" style={{ color: LINE }} /><p className="text-xs" style={{ color: SUB }}>No active chats</p></div>
            )}
          </div>
        </div>

        {convs.length > 0 && activeConv ? (
          <div className="col-span-2 rounded-xl border flex flex-col overflow-hidden" style={{ borderColor: LINE, backgroundColor: "#FFFFFF" }}>
            <div className="px-5 py-4 border-b flex items-center justify-between" style={{ borderColor: LINE }}>
              <div>
                <p className="text-sm font-semibold" style={{ color: INK }}>{activeConv.name}</p>
                <p className="text-xs" style={{ color: SUB }}>Order {activeConv.order} · Waiting {activeConv.waiting}</p>
              </div>
              <button onClick={() => closeChat(activeConv.id)} className="px-3 py-1.5 rounded-lg text-xs font-medium hover:opacity-80 transition-opacity" style={{ backgroundColor: "#FEE2E2", color: "#991B1B" }}>Close Chat</button>
            </div>
            <div className="flex-1 p-5 overflow-y-auto space-y-3">
              {messages.map((msg, idx) => (
                <div key={idx} className={`flex ${msg.from === "agent" ? "justify-end" : "justify-start"}`}>
                  <div className="max-w-xs px-4 py-2.5 rounded-2xl" style={{ backgroundColor: msg.from === "agent" ? INK : "#F0F4FB", color: msg.from === "agent" ? "#FFFFFF" : INK }}>
                    <p className="text-sm">{msg.text}</p>
                    <span className="text-[10px] mt-1 block" style={{ opacity: 0.6 }}>{msg.time}</span>
                  </div>
                </div>
              ))}
              <div ref={endRef} />
            </div>
            <div className="px-5 py-4 border-t space-y-2.5" style={{ borderColor: LINE }}>
              <div className="flex items-center gap-2">
                <button onClick={() => { hapticTap(); setShowMacros((s) => !s); }} className="shrink-0 flex items-center gap-1 text-xs font-medium px-2.5 py-1.5 rounded-lg transition-colors" style={{ backgroundColor: showMacros ? INK : SKY_BG, color: showMacros ? "#FFFFFF" : SKY }}>
                  <Zap size={12} /> Macros
                </button>
                {showMacros && (
                  <div className="flex gap-1.5 overflow-x-auto pb-0.5">
                    {CANNED_REPLIES.map((mm) => (
                      <button key={mm.label} onClick={() => applyMacro(mm.text)} className="shrink-0 text-xs font-medium px-2.5 py-1.5 rounded-lg border hover:bg-gray-50 transition-colors whitespace-nowrap" style={{ borderColor: LINE, color: INK }}>{mm.label}</button>
                    ))}
                  </div>
                )}
              </div>
              <div className="flex gap-2">
                <input type="text" placeholder="Type a message…" className="flex-1 border rounded-xl px-4 py-2.5 text-sm outline-none focus:border-[#0088CC] transition-colors" style={{ borderColor: LINE, color: INK }}
                  value={input} onChange={(e) => setInput(e.target.value)} onKeyDown={(e) => { if (e.key === "Enter" && !e.shiftKey) { e.preventDefault(); sendMessage(); } }} />
                <button onClick={sendMessage} disabled={!input.trim()} className="px-5 py-2.5 rounded-xl font-medium flex items-center gap-2 text-sm disabled:opacity-40 hover:opacity-90 transition-opacity" style={{ backgroundColor: INK, color: "#FFFFFF" }}>
                  <Send size={15} /> Send
                </button>
              </div>
            </div>
          </div>
        ) : (
          <div className="col-span-2 rounded-xl border flex items-center justify-center" style={{ borderColor: LINE, backgroundColor: "#FFFFFF" }}>
            <div className="text-center"><MessageCircle size={40} className="mx-auto mb-3" style={{ color: LINE }} /><p className="text-sm" style={{ color: SUB }}>No active conversations</p></div>
          </div>
        )}
      </div>

      {showKB && <KBQuickPanel onClose={() => setShowKB(false)} />}
    </div>
  );
}

/* ─── Root ───────────────────────────────────────────────────────────────────── */
interface Props { activeTab: string; }

export function CustomerSupport({ activeTab }: Props) {
  if (activeTab === "tickets") return <TicketsTab />;
  if (activeTab === "knowledge") return <KnowledgeBase />;
  return <LiveChat />;
}