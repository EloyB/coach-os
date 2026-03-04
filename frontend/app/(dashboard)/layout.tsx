import { DashboardSidebar } from "@/components/layouts/dashboard-sidebar";
import { MobileBottomNav } from "@/components/layouts/dashboard-bottom-nav";

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="flex h-screen bg-[#F5F4F1] overflow-hidden">
      <DashboardSidebar />

      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Topbar */}
        <header className="bg-white border-b border-gray-100 px-8 py-4 flex items-center justify-between shrink-0">
          <p className="text-xs text-gray-400 font-medium uppercase tracking-wider">
            Tennis &amp; Padel Platform
          </p>
          <div className="flex items-center gap-3">
            <div className="text-right">
              <p className="text-sm font-semibold text-gray-800 leading-none">Coach</p>
              <p className="text-xs text-gray-400 mt-0.5">Beheerder</p>
            </div>
            <div className="w-9 h-9 rounded-full bg-tennis-green flex items-center justify-center shrink-0">
              <span className="text-tennis-lime text-sm font-bold">C</span>
            </div>
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1 overflow-y-auto px-8 py-8 pb-16 lg:pb-8">{children}</main>
      </div>

      <MobileBottomNav />
    </div>
  );
}
